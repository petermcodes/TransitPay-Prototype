using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using TransitPay.API.Data;
using TransitPay.API.DTOs.Admin;
using TransitPay.API.Enums;
using TransitPay.API.Interfaces;
using TransitPay.API.Models;
using TransitPay.API.Models.History;
using TransitPay.API.Utilities;

namespace TransitPay.API.Services;

/// <summary>
/// Service for the Administration Domain.
/// Handles all user, driver, and administrator account management.
/// Controllers remain thin — all business logic lives here.
/// </summary>
public class AdminService : IAdminService
{
    private readonly TransitPayDbContext _dbContext;
    private readonly PasswordHasher<User> _passwordHasher;
    private readonly ILogger<AdminService> _logger;

    /// <summary>
    /// Creates a new AdminService.
    /// </summary>
    public AdminService(
        TransitPayDbContext dbContext,
        PasswordHasher<User> passwordHasher,
        ILogger<AdminService> logger)
    {
        _dbContext = dbContext;
        _passwordHasher = passwordHasher;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<User> CreateDriverAsync(CreateDriverRequest request)
    {
        _logger.LogInformation("Creating driver account for mobile: {MobileNumber}", request.MobileNumber);

        var driverRole = await _dbContext.Roles.FirstOrDefaultAsync(r => r.RoleName == RoleName.Driver)
            ?? throw new InvalidOperationException("Driver role not found.");

        await ValidateNewUserAsync(request.MobileNumber, request.FirstName, request.LastName, request.Password);

        // Use a temporary unique username first; the final Driver ID is derived from the
        // assigned UserId (identity column, never reused) to guarantee uniqueness.
        var driver = new User
        {
            Username = $"DRV-PENDING-{Guid.NewGuid():N}",
            FirstName = request.FirstName,
            LastName = request.LastName,
            MobileNumber = request.MobileNumber,
            // Temporary placeholder hash — replaced with the real password below.
            PasswordHash = _passwordHasher.HashPassword(null!, "TEMP-PLACEHOLDER-NOT-USABLE"),
            IsActive = true, // Drivers are created active immediately — no approval workflow
            RoleId = driverRole.RoleId,
            PasswordChangedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.Users.Add(driver);
        await _dbContext.SaveChangesAsync();

        // Driver ID = DRV-{UserId:D6} — guaranteed unique, never reused.
        // When no custom password is provided, the Driver ID becomes the default password.
        driver.Username = $"DRV-{driver.UserId:D6}";
        driver.PasswordHash = _passwordHasher.HashPassword(null!, string.IsNullOrWhiteSpace(request.Password)
            ? driver.Username
            : request.Password);
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("Driver created successfully. UserId: {UserId}, DriverId: {DriverId}", driver.UserId, driver.Username);
        return driver;
    }

    /// <inheritdoc />
    public async Task<User> CreateAdministratorAsync(CreateAdministratorRequest request)
    {
        _logger.LogInformation("Creating administrator account for mobile: {MobileNumber}", request.MobileNumber);

        var adminRole = await _dbContext.Roles.FirstOrDefaultAsync(r => r.RoleName == RoleName.Admin)
            ?? throw new InvalidOperationException("Admin role not found.");

        await ValidateNewUserAsync(request.MobileNumber, request.FirstName, request.LastName, request.Password);

        var admin = new User
        {
            Username = request.Username,
            FirstName = request.FirstName,
            LastName = request.LastName,
            MobileNumber = request.MobileNumber,
            PasswordHash = _passwordHasher.HashPassword(null!, request.Password),
            IsActive = true,
            RoleId = adminRole.RoleId,
            PasswordChangedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.Users.Add(admin);
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("Administrator created successfully. UserId: {UserId}", admin.UserId);
        return admin;
    }

    /// <inheritdoc />
    public async Task<User> ActivateUserAsync(int userId)
    {
        _logger.LogInformation("Activating user {UserId}", userId);

        var user = await FindUserOrThrowAsync(userId);
        user.IsActive = true;
        user.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("User {UserId} activated successfully", userId);
        return user;
    }

    /// <inheritdoc />
    public async Task<User> DeactivateUserAsync(int userId)
    {
        _logger.LogInformation("Deactivating user {UserId}", userId);

        var user = await FindUserOrThrowAsync(userId);
        user.IsActive = false;
        user.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("User {UserId} deactivated successfully", userId);
        return user;
    }

    /// <inheritdoc />
    public async Task<User> ResetUserPasswordAsync(int userId, string newPassword)
    {
        _logger.LogInformation("Resetting password for user {UserId}", userId);

        var user = await FindUserOrThrowAsync(userId);

        // Apply password policy with the user's personal information
        var (isValid, errorMessage) = PasswordPolicy.Validate(
            newPassword,
            user.FirstName,
            user.LastName,
            user.MobileNumber);

        if (!isValid)
        {
            throw new InvalidOperationException(errorMessage);
        }

        user.PasswordHash = _passwordHasher.HashPassword(null!, newPassword);
        user.PasswordChangedAt = DateTime.UtcNow;
        user.FailedLoginAttempts = 0;
        user.LockoutEnd = null;
        user.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("Password reset successfully for user {UserId}", userId);
        return user;
    }

    /// <inheritdoc />
    public async Task<User> UnlockUserAccountAsync(int userId)
    {
        _logger.LogInformation("Unlocking account for user {UserId}", userId);

        var user = await FindUserOrThrowAsync(userId);
        user.LockoutEnd = null;
        user.FailedLoginAttempts = 0;
        user.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("User {UserId} unlocked successfully", userId);
        return user;
    }

    /// <inheritdoc />
    public async Task<User> UpdateUserAsync(int userId, UpdateUserRequest request)
    {
        _logger.LogInformation("Updating user {UserId}", userId);

        var user = await FindUserOrThrowAsync(userId);

        // Record the original record to history before applying changes
        var passengerRoleId = await GetPassengerRoleIdAsync();
        var driverRoleId = await GetDriverRoleIdAsync();
        if (user.RoleId == passengerRoleId)
        {
            await RecordPassengerHistoryAsync("EDIT", user, 0);
        }
        else if (user.RoleId == driverRoleId)
        {
            await RecordDriverHistoryAsync("EDIT", user, 0);
        }

        // Check if the new mobile number is already taken by another user
        if (user.MobileNumber != request.MobileNumber)
        {
            var existingUser = await _dbContext.Users
                .FirstOrDefaultAsync(u => u.MobileNumber == request.MobileNumber && u.UserId != userId);

            if (existingUser != null)
            {
                throw new InvalidOperationException("A user with this mobile number already exists.");
            }
        }

        user.FirstName = request.FirstName;
        user.LastName = request.LastName;
        user.MobileNumber = request.MobileNumber;
        user.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("User {UserId} updated successfully", userId);
        return user;
    }

    /// <inheritdoc />
    public async Task<(List<User> Users, int Total)> GetUsersAsync(int page, int pageSize)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var passengerRoleId = await GetPassengerRoleIdAsync();

        // Only return passengers (role_id = 1) — never administrators or system accounts
        var query = _dbContext.Users
            .Include(u => u.Role)
            .Where(u => u.RoleId == passengerRoleId && u.DeletedAt == null);

        var total = await query.CountAsync();
        var users = await query
            .OrderByDescending(u => u.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (users, total);
    }

    /// <inheritdoc />
    public async Task<(List<User> Drivers, int Total)> GetDriversAsync(int page, int pageSize)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var driverRoleId = await GetDriverRoleIdAsync();

        // Only return drivers (role_id = 2) — never administrators or passengers
        var query = _dbContext.Users
            .Where(u => u.RoleId == driverRoleId && u.DeletedAt == null);

        var total = await query.CountAsync();
        var drivers = await query
            .OrderByDescending(u => u.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (drivers, total);
    }

    /// <summary>
    /// Validates a new user account: checks for duplicate mobile number and applies password policy.
    /// </summary>
    private async Task ValidateNewUserAsync(string mobileNumber, string firstName, string lastName, string? password)
    {
        var existingUser = await _dbContext.Users.FirstOrDefaultAsync(u => u.MobileNumber == mobileNumber);
        if (existingUser != null)
        {
            throw new InvalidOperationException("A user with this mobile number already exists.");
        }

        // Password policy is only enforced when an explicit password is provided.
        // Driver accounts use the Driver ID (e.g., DRV-000010) as the default password.
        if (!string.IsNullOrWhiteSpace(password))
        {
            var (isValid, errorMessage) = PasswordPolicy.Validate(password, firstName, lastName, mobileNumber);
            if (!isValid)
            {
                throw new InvalidOperationException(errorMessage);
            }
        }
    }

    /// <summary>
    /// Finds a user by ID or throws if not found.
    /// </summary>
    private async Task<User> FindUserOrThrowAsync(int userId)
    {
        var user = await _dbContext.Users.FindAsync(userId)
            ?? throw new InvalidOperationException("User not found.");
        return user;
    }

    /// <summary>
    /// Resolves the Passenger role ID from the roles table, or 0 when the role has
    /// not been seeded yet.
    /// </summary>
    private async Task<int> GetPassengerRoleIdAsync()
    {
        var role = await _dbContext.Roles.FirstOrDefaultAsync(r => r.RoleName == RoleName.Passenger);
        return role?.RoleId ?? 0;
    }

    /// <summary>
    /// Resolves the Driver role ID from the roles table, or 0 when the role has
    /// not been seeded yet.
    /// </summary>
    private async Task<int> GetDriverRoleIdAsync()
    {
        var role = await _dbContext.Roles.FirstOrDefaultAsync(r => r.RoleName == RoleName.Driver);
        return role?.RoleId ?? 0;
    }

    // ── Strongly typed history recording ─────────────────────────────────────

    /// <summary>
    /// Records a passenger edit or delete operation to the appropriate history table.
    /// </summary>
    private async Task RecordPassengerHistoryAsync(string operation, User original, int performedByUserId, string? reason = null)
    {
        var json = JsonSerializer.Serialize(original);
        if (operation == "EDIT")
        {
            _dbContext.PassengerEditHistories.Add(new PassengerEditHistory
            {
                OriginalRecordId = original.UserId,
                Operation = "EDIT",
                PerformedByUserId = performedByUserId,
                PerformedAt = DateTime.UtcNow,
                OriginalData = json,
                Reason = reason
            });
        }
        else
        {
            _dbContext.PassengerDeleteHistories.Add(new PassengerDeleteHistory
            {
                OriginalRecordId = original.UserId,
                Operation = "DELETE",
                PerformedByUserId = performedByUserId,
                PerformedAt = DateTime.UtcNow,
                OriginalData = json,
                Reason = reason
            });
        }
        await _dbContext.SaveChangesAsync();
    }

    /// <summary>
    /// Records a driver edit or delete operation to the appropriate history table.
    /// </summary>
    private async Task RecordDriverHistoryAsync(string operation, User original, int performedByUserId, string? reason = null)
    {
        var json = JsonSerializer.Serialize(original);
        if (operation == "EDIT")
        {
            _dbContext.DriverEditHistories.Add(new DriverEditHistory
            {
                OriginalRecordId = original.UserId,
                Operation = "EDIT",
                PerformedByUserId = performedByUserId,
                PerformedAt = DateTime.UtcNow,
                OriginalData = json,
                Reason = reason
            });
        }
        else
        {
            _dbContext.DriverDeleteHistories.Add(new DriverDeleteHistory
            {
                OriginalRecordId = original.UserId,
                Operation = "DELETE",
                PerformedByUserId = performedByUserId,
                PerformedAt = DateTime.UtcNow,
                OriginalData = json,
                Reason = reason
            });
        }
        await _dbContext.SaveChangesAsync();
    }

    /// <summary>
    /// Records a terminal edit or delete operation to the appropriate history table.
    /// </summary>
    private async Task RecordTerminalHistoryAsync(string operation, Terminal original, int performedByUserId, string? reason = null)
    {
        var json = JsonSerializer.Serialize(original);
        if (operation == "EDIT")
        {
            _dbContext.TerminalEditHistories.Add(new TerminalEditHistory
            {
                OriginalRecordId = original.TerminalId,
                Operation = "EDIT",
                PerformedByUserId = performedByUserId,
                PerformedAt = DateTime.UtcNow,
                OriginalData = json,
                Reason = reason
            });
        }
        else
        {
            _dbContext.TerminalDeleteHistories.Add(new TerminalDeleteHistory
            {
                OriginalRecordId = original.TerminalId,
                Operation = "DELETE",
                PerformedByUserId = performedByUserId,
                PerformedAt = DateTime.UtcNow,
                OriginalData = json,
                Reason = reason
            });
        }
        await _dbContext.SaveChangesAsync();
    }

    /// <summary>
    /// Records a fare matrix edit or delete operation to the appropriate history table.
    /// </summary>
    private async Task RecordFareMatrixHistoryAsync(string operation, FareRule original, int performedByUserId, string? reason = null)
    {
        var json = JsonSerializer.Serialize(original);
        if (operation == "EDIT")
        {
            _dbContext.FareMatrixEditHistories.Add(new FareMatrixEditHistory
            {
                OriginalRecordId = original.FareId,
                Operation = "EDIT",
                PerformedByUserId = performedByUserId,
                PerformedAt = DateTime.UtcNow,
                OriginalData = json,
                Reason = reason
            });
        }
        else
        {
            _dbContext.FareMatrixDeleteHistories.Add(new FareMatrixDeleteHistory
            {
                OriginalRecordId = original.FareId,
                Operation = "DELETE",
                PerformedByUserId = performedByUserId,
                PerformedAt = DateTime.UtcNow,
                OriginalData = json,
                Reason = reason
            });
        }
        await _dbContext.SaveChangesAsync();
    }
}