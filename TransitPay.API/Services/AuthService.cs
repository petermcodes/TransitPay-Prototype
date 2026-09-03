using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using TransitPay.API.Configuration;
using TransitPay.API.Data;
using TransitPay.API.DTOs.Auth;
using TransitPay.API.Enums;
using TransitPay.API.Interfaces;
using TransitPay.API.Models;
using TransitPay.API.Utilities;

namespace TransitPay.API.Services;

/// <summary>
/// Handles the passenger authentication lifecycle: registration, login, token refresh, and logout.
/// - Registration always assigns the Passenger role server-side and provisions a transit card,
///   wallet, and QR code for the new account.
/// - Login supports username, mobile number, or Driver ID, and enforces account lockout with a
///   generic "Invalid credentials." message to prevent account enumeration.
/// - All security-sensitive events are written to the auth audit log with PII minimized via
///   <see cref="TransitPay.API.Utilities.PiiHasher"/>.
/// </summary>
public class AuthService : IAuthService
{
    private readonly TransitPayDbContext _dbContext;
    private readonly PasswordHasher<User> _passwordHasher;
    private readonly ITokenService _tokenService;
    private readonly IQRService _qrService;
    private readonly ILogger<AuthService> _logger;
    private readonly AuthenticationSettings _authSettings;
    private readonly IHttpContextAccessor _httpContextAccessor;

    /// <summary>
    /// Creates a new AuthService.
    /// </summary>
    public AuthService(
        TransitPayDbContext dbContext,
        PasswordHasher<User> passwordHasher,
        ITokenService tokenService,
        IQRService qrService,
        IOptions<AuthenticationSettings> authSettings,
        IHttpContextAccessor httpContextAccessor,
        ILogger<AuthService> logger)
    {
        _dbContext = dbContext;
        _passwordHasher = passwordHasher;
        _tokenService = tokenService;
        _qrService = qrService;
        _authSettings = authSettings.Value;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<RegisterResponse> RegisterAsync(string username, string firstName, string lastName, string mobileNumber, string password)
    {
        _logger.LogInformation("Registration attempt for username: {Username}", username);

        // Validate inputs
        if (string.IsNullOrWhiteSpace(username) || username.Length < 3)
        {
            return new RegisterResponse { Success = false, Message = "Username must be at least 3 characters." };
        }

        if (string.IsNullOrWhiteSpace(firstName) || firstName.Length < 2)
        {
            return new RegisterResponse { Success = false, Message = "First name must be at least 2 characters." };
        }

        if (string.IsNullOrWhiteSpace(lastName) || lastName.Length < 2)
        {
            return new RegisterResponse { Success = false, Message = "Last name must be at least 2 characters." };
        }

        if (string.IsNullOrWhiteSpace(mobileNumber) || mobileNumber.Length < 10)
        {
            return new RegisterResponse { Success = false, Message = "Mobile number must be at least 10 digits." };
        }

        // Apply password policy — all validation is server-side
        var (isValid, errorMessage) = PasswordPolicy.Validate(password, firstName, lastName, mobileNumber);
        if (!isValid)
        {
            _logger.LogWarning("Registration failed - password policy violation for username: {Username}", username);
            return new RegisterResponse { Success = false, Message = errorMessage! };
        }

        try
        {
            // Always assign the Passenger role server-side — never trust client-supplied role information
            var passengerRole = await _dbContext.Roles.FirstOrDefaultAsync(r => r.RoleName == RoleName.Passenger);
            if (passengerRole == null)
            {
                _logger.LogError("Passenger role not found in database during registration");
                return new RegisterResponse { Success = false, Message = "An error occurred during registration." };
            }

            // Validate username uniqueness
            var existingUsername = await _dbContext.Users.FirstOrDefaultAsync(u => u.Username == username);
            if (existingUsername != null)
            {
                _logger.LogWarning("Registration failed - username already taken: {Username}", username);
                return new RegisterResponse { Success = false, Message = "Username is already taken." };
            }

            // Validate mobile number uniqueness
            var existingMobile = await _dbContext.Users.FirstOrDefaultAsync(u => u.MobileNumber == mobileNumber);
            if (existingMobile != null)
            {
                _logger.LogWarning("Registration failed - mobile number already exists: {MobileNumber}", mobileNumber);
                return new RegisterResponse { Success = false, Message = "A user with this mobile number already exists." };
            }

            var user = new User
            {
                Username = username,
                FirstName = firstName,
                LastName = lastName,
                MobileNumber = mobileNumber,
                PasswordHash = _passwordHasher.HashPassword(null!, password),
                IsActive = true,
                RoleId = passengerRole.RoleId,
                PasswordChangedAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow
            };

            _dbContext.Users.Add(user);
            await _dbContext.SaveChangesAsync();

            // Automatically create a Transit card for the new passenger
            var card = new Card
            {
                UserId = user.UserId,
                CardNumber = GenerateCardNumber(),
                Status = CardStatus.ACTIVE,
                PassengerType = PassengerType.Passenger,
                IssueDate = DateTime.UtcNow,
                ExpiryDate = DateTime.UtcNow.AddYears(5), // Cards valid for 5 years
                CreatedAt = DateTime.UtcNow,
                RowVersion = new byte[] { 0, 0, 0, 0, 0, 0, 0, 1 }
            };

            _dbContext.Cards.Add(card);
            await _dbContext.SaveChangesAsync();

            _logger.LogInformation("Transit card created for user {UserId}. CardId: {CardId}", user.UserId, card.CardId);

            // Automatically create a wallet for the new card with initial balance
            var wallet = new Wallet
            {
                CardId = card.CardId,
                Balance = 50.00m, // Initial balance of ₱50.00
                Status = CardStatus.ACTIVE,
                CreatedAt = DateTime.UtcNow
            };

            _dbContext.Wallets.Add(wallet);
            await _dbContext.SaveChangesAsync();

            _logger.LogInformation("Wallet created for card {CardId} with initial balance ₱{Balance}", card.CardId, wallet.Balance);

            // Automatically generate a QR code for the new card using the card number as the token
            try
            {
                // Create QR code directly with the card number as the token for easy payment integration
                var qrCode = new QRCode
                {
                    CardId = card.CardId,
                    Token = card.CardNumber, // Use card number as QR token for direct payment linking
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };

                _dbContext.QRCodes.Add(qrCode);
                await _dbContext.SaveChangesAsync();

                _logger.LogInformation("QR code created for card {CardId} using card number as token", card.CardId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create QR code for card {CardId}", card.CardId);
                // Don't fail registration if QR creation fails - it can be created later
            }

            await WriteAuditLogAsync("register", user.UserId, mobileNumber);
            _logger.LogInformation("Passenger registered successfully. UserId: {UserId}, Username: {Username}, CardId: {CardId}", user.UserId, username, card.CardId);

            return new RegisterResponse
            {
                Success = true,
                Message = "User registered successfully.",
                Data = new RegisterData { UserId = user.UserId, Role = RoleName.Passenger.ToString() }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during registration for username: {Username}", username);
            return new RegisterResponse { Success = false, Message = "An error occurred during registration." };
        }
    }

    /// <inheritdoc />
    public async Task<LoginResponse> LoginAsync(string username, string password)
    {
        _logger.LogInformation("Login attempt for username: {Username}", username);

        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            _logger.LogWarning("Login failed - missing credentials");
            return new LoginResponse { Success = false, Message = "Invalid credentials." };
        }

        try
        {
            // Authenticate by username or mobile number (works for Admin, Driver, and Passenger)
            // Drivers log in with their Driver ID (e.g., DRV-000010); passengers/admins use username or mobile number.
            var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Username == username || u.MobileNumber == username);
            if (user == null)
            {
                // Always return the same generic message to prevent account enumeration
                await WriteAuditLogAsync("login_failed", null, null);
                _logger.LogWarning("Login failed - user not found: {Username}", username);
                return new LoginResponse { Success = false, Message = "Invalid credentials." };
            }

            // Block inactive users from authenticating
            if (!user.IsActive)
            {
                await WriteAuditLogAsync("login_failed", user.UserId, user.MobileNumber);
                _logger.LogWarning("Login failed - user {UserId} is inactive", user.UserId);
                return new LoginResponse { Success = false, Message = "Invalid credentials." };
            }

            // Block users whose lockout period has not expired
            if (user.LockoutEnd.HasValue && user.LockoutEnd.Value > DateTime.UtcNow)
            {
                await WriteAuditLogAsync("login_locked", user.UserId, user.MobileNumber);
                var remainingMinutes = (int)Math.Ceiling((user.LockoutEnd.Value - DateTime.UtcNow).TotalMinutes);
                _logger.LogWarning("Login failed - user {UserId} is locked out for {Minutes} more minutes", user.UserId, remainingMinutes);
                return new LoginResponse { Success = false, Message = "Invalid credentials." };
            }

            var verificationResult = _passwordHasher.VerifyHashedPassword(null!, user.PasswordHash, password);
            if (verificationResult != PasswordVerificationResult.Success)
            {
                // Record the failed attempt
                user.FailedLoginAttempts++;

                // Lock the account once the threshold is reached
                if (user.FailedLoginAttempts >= _authSettings.MaxFailedAttempts)
                {
                    user.LockoutEnd = DateTime.UtcNow.AddMinutes(_authSettings.LockoutMinutes);
                    user.FailedLoginAttempts = 0; // Reset counter; lockout is now enforced by LockoutEnd
                    _logger.LogWarning("User {UserId} locked out for {Minutes} minutes after {Attempts} failed attempts",
                        user.UserId, _authSettings.LockoutMinutes, _authSettings.MaxFailedAttempts);
                }
                else
                {
                    _logger.LogWarning("Login failed - invalid password for user {UserId} (attempt {Attempt}/{Max})",
                        user.UserId, user.FailedLoginAttempts, _authSettings.MaxFailedAttempts);
                }

                user.UpdatedAt = DateTime.UtcNow;
                await _dbContext.SaveChangesAsync();

                await WriteAuditLogAsync("login_failed", user.UserId, user.MobileNumber);

                // Always return the same generic message to prevent account enumeration
                return new LoginResponse { Success = false, Message = "Invalid credentials." };
            }

            // Clear lockout and reset failed attempts on successful login
            if (user.FailedLoginAttempts != 0 || user.LockoutEnd.HasValue)
            {
                user.FailedLoginAttempts = 0;
                user.LockoutEnd = null;
                user.UpdatedAt = DateTime.UtcNow;
            }

            var role = await _dbContext.Roles.FindAsync(user.RoleId);
            var token = await _tokenService.CreateTokenAsync(user);
            var refreshToken = await _tokenService.CreateRefreshTokenAsync(user.UserId);

            await _dbContext.SaveChangesAsync();

            await WriteAuditLogAsync("login_success", user.UserId, user.MobileNumber);
            _logger.LogInformation("Login successful for user: {UserId}", user.UserId);

            return new LoginResponse
            {
                Success = true,
                Message = "Login successful.",
                Data = new LoginData
                {
                    Token = token,
                    RefreshToken = refreshToken.Token,
                    User = new UserInfo
                    {
                        UserId = user.UserId,
                        Username = user.Username,
                        FirstName = user.FirstName,
                        LastName = user.LastName,
                        MobileNumber = user.MobileNumber,
                        RoleId = user.RoleId,
                        RoleName = role?.RoleName
                    }
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during login for username: {Username}", username);
            return new LoginResponse { Success = false, Message = "An error occurred during login." };
        }
    }

    /// <inheritdoc />
    public async Task<RefreshTokenResponse> RefreshTokenAsync(int userId, string refreshToken)
    {
        _logger.LogInformation("Token refresh attempt for user: {UserId}", userId);

        if (userId <= 0 || string.IsNullOrWhiteSpace(refreshToken))
        {
            _logger.LogWarning("Token refresh failed - invalid input for user: {UserId}", userId);
            return new RefreshTokenResponse { Success = false, Message = "Invalid or expired refresh token." };
        }

        try
        {
            var user = await _dbContext.Users.FindAsync(userId);
            if (user == null || !user.IsActive)
            {
                _logger.LogWarning("Token refresh failed - user not found or inactive: {UserId}", userId);
                return new RefreshTokenResponse { Success = false, Message = "Invalid or expired refresh token." };
            }

            // Rotate the refresh token (revokes current, issues new, detects reuse)
            var newRefreshToken = await _tokenService.RotateRefreshTokenAsync(userId, refreshToken);
            if (newRefreshToken == null)
            {
                await WriteAuditLogAsync("refresh_failed", userId, user.MobileNumber);
                _logger.LogWarning("Token refresh failed - rotation rejected for user: {UserId}", userId);
                return new RefreshTokenResponse { Success = false, Message = "Invalid or expired refresh token." };
            }

            var newToken = await _tokenService.CreateTokenAsync(user);

            await WriteAuditLogAsync("refresh_success", userId, user.MobileNumber);
            _logger.LogInformation("Token refreshed successfully for user: {UserId}", userId);

            return new RefreshTokenResponse
            {
                Success = true,
                Message = "Token refreshed successfully.",
                Data = new RefreshTokenData
                {
                    Token = newToken,
                    RefreshToken = newRefreshToken.Token
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during token refresh for user: {UserId}", userId);
            return new RefreshTokenResponse { Success = false, Message = "An error occurred during token refresh." };
        }
    }

    /// <summary>
    /// Writes a PII-minimized authentication audit log entry.
    /// Only stores a SHA-256 hash of the mobile number — never plaintext PII.
    /// </summary>
    private async Task WriteAuditLogAsync(string eventType, int? userId, string? mobileNumber)
    {
        try
        {
            var ip = _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString();

            _dbContext.AuthAuditLogs.Add(new AuthAuditLog
            {
                UserId = userId,
                EventType = eventType,
                ActorHash = string.IsNullOrWhiteSpace(mobileNumber) ? null : PiiHasher.Sha256Hex(mobileNumber),
                IpAddress = ip,
                CreatedAt = DateTime.UtcNow
            });

            await _dbContext.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            // Audit logging must never break the auth flow
            _logger.LogError(ex, "Failed to write audit log for event {EventType}", eventType);
        }
    }

    /// <inheritdoc />
    public async Task<User?> GetUserByIdAsync(int userId)
    {
        try
        {
            var user = await _dbContext.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.UserId == userId);
            return user;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving user by ID: {UserId}", userId);
            return null;
        }
    }

    /// <inheritdoc />
    public async Task<bool> LogoutAsync(int userId)
    {
        _logger.LogInformation("Logout request for user: {UserId}", userId);

        if (userId <= 0)
        {
            return false;
        }

        try
        {
            await _tokenService.RevokeAllRefreshTokensAsync(userId);
            await WriteAuditLogAsync("logout", userId, null);
            _logger.LogInformation("Logout successful for user: {UserId}", userId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during logout for user: {UserId}", userId);
            return false;
        }
    }

    /// <summary>
    /// Generates a unique 16-digit card number for a new transit card.
    /// </summary>
    private static string GenerateCardNumber()
    {
        // Generate a 16-digit numeric card number
        var cardNumber = new char[16];
        var random = new Random();
        
        for (int i = 0; i < 16; i++)
        {
            cardNumber[i] = (char)('0' + random.Next(0, 10));
        }
        
        return new string(cardNumber);
    }
}
