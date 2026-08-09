using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TransitPay.API.Data;
using TransitPay.API.DTOs.Admin;
using TransitPay.API.Enums;
using TransitPay.API.Interfaces;
using TransitPay.API.Models;
using TransitPay.API.Models.History;
using TransitPay.API.Utilities;

namespace TransitPay.API.Controllers;

[ApiController]
[Route("api/admin")]
[Authorize(Roles = "Admin")]
public class AdminController : ControllerBase
{
    private readonly TransitPayDbContext _dbContext;
    private readonly IAdminService _adminService;

    public AdminController(TransitPayDbContext dbContext, IAdminService adminService)
    {
        _dbContext = dbContext;
        _adminService = adminService;
    }

    [HttpGet("users")]
    public async Task<IActionResult> GetUsers([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var (users, total) = await _adminService.GetUsersAsync(page, pageSize);

        return Ok(new
        {
            success = true,
            message = "Users retrieved successfully.",
            data = users.Select(u => new { u.UserId, u.Username, u.FirstName, u.LastName, u.MobileNumber, u.IsActive, roleName = u.Role!.RoleName.ToString() }),
            pagination = new { page, pageSize, total, totalPages = (int)Math.Ceiling(total / (double)pageSize) }
        });
    }

    [HttpGet("drivers")]
    public async Task<IActionResult> GetDrivers([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var (drivers, total) = await _adminService.GetDriversAsync(page, pageSize);

        return Ok(new
        {
            success = true,
            message = "Drivers retrieved successfully.",
            data = drivers.Select(u => new { u.UserId, u.Username, u.FirstName, u.LastName, u.MobileNumber, u.IsActive }),
            pagination = new { page, pageSize, total, totalPages = (int)Math.Ceiling(total / (double)pageSize) }
        });
    }

    /// <summary>
    /// Creates a new Administrator account (Admin only).
    /// Only existing Administrators may create additional Administrator accounts.
    /// </summary>
    [HttpPost("accounts")]
    public async Task<IActionResult> CreateAdministrator([FromBody] CreateAdministratorRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new { success = false, message = "Validation failed.", errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage) });
        }

        try
        {
            var admin = await _adminService.CreateAdministratorAsync(request);

            return Ok(new
            {
                success = true,
                message = "Administrator created successfully.",
                data = new
                {
                    admin.UserId,
                    admin.FirstName,
                    admin.LastName,
                    admin.MobileNumber,
                    admin.IsActive,
                    admin.CreatedAt
                }
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
        catch (Exception)
        {
            return StatusCode(500, new { success = false, message = "An error occurred while creating the administrator." });
        }
    }

    /// <summary>
    /// Activates a user account (Admin only).
    /// </summary>
    [HttpPut("users/{id}/activate")]
    public async Task<IActionResult> ActivateUser(int id)
    {
        try
        {
            var user = await _adminService.ActivateUserAsync(id);

            return Ok(new
            {
                success = true,
                message = "User activated successfully.",
                data = new { user.UserId, user.IsActive }
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
        catch (Exception)
        {
            return StatusCode(500, new { success = false, message = "An error occurred while activating the user." });
        }
    }

    /// <summary>
    /// Deactivates a user account (Admin only).
    /// Deactivated users cannot authenticate. No data is deleted.
    /// </summary>
    [HttpPut("users/{id}/deactivate")]
    public async Task<IActionResult> DeactivateUser(int id)
    {
        try
        {
            var user = await _adminService.DeactivateUserAsync(id);

            return Ok(new
            {
                success = true,
                message = "User deactivated successfully.",
                data = new { user.UserId, user.IsActive }
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
        catch (Exception)
        {
            return StatusCode(500, new { success = false, message = "An error occurred while deactivating the user." });
        }
    }

    /// <summary>
    /// Resets a user's password (Admin only).
    /// Applies the password policy and clears any account lockout.
    /// </summary>
    [HttpPost("users/{id}/reset-password")]
    public async Task<IActionResult> ResetUserPassword(int id, [FromBody] ResetPasswordRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new { success = false, message = "Validation failed.", errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage) });
        }

        try
        {
            await _adminService.ResetUserPasswordAsync(id, request.NewPassword);

            return Ok(new { success = true, message = "Password reset successfully." });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
        catch (Exception)
        {
            return StatusCode(500, new { success = false, message = "An error occurred while resetting the password." });
        }
    }

    /// <summary>
    /// Unlocks a locked user account (Admin only).
    /// </summary>
    [HttpPost("users/{id}/unlock")]
    public async Task<IActionResult> UnlockUser(int id)
    {
        try
        {
            await _adminService.UnlockUserAccountAsync(id);

            return Ok(new { success = true, message = "User account unlocked successfully." });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
        catch (Exception)
        {
            return StatusCode(500, new { success = false, message = "An error occurred while unlocking the user." });
        }
    }

    /// <summary>
    /// Updates a user's personal information (Admin only).
    /// </summary>
    [HttpPut("users/{id}")]
    public async Task<IActionResult> UpdateUser(int id, [FromBody] UpdateUserRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new { success = false, message = "Validation failed.", errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage) });
        }

        try
        {
            var user = await _adminService.UpdateUserAsync(id, request);

            return Ok(new
            {
                success = true,
                message = "User updated successfully.",
                data = new
                {
                    user.UserId,
                    user.FirstName,
                    user.LastName,
                    user.MobileNumber
                }
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
        catch (Exception)
        {
            return StatusCode(500, new { success = false, message = "An error occurred while updating the user." });
        }
    }

    [HttpGet("terminals")]
    public async Task<IActionResult> GetTerminals()
    {
        var terminals = await _dbContext.Terminals
            .Where(t => t.DeletedAt == null)
            .Select(t => new { t.TerminalId, t.TerminalName, t.IsActive })
            .ToListAsync();
        return Ok(new { success = true, message = "Terminals retrieved successfully.", data = terminals });
    }

    [HttpPost("terminals")]
    public async Task<IActionResult> CreateTerminal([FromBody] CreateTerminalRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new { success = false, message = "Validation failed.", errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage) });
        }

        try
        {
            var terminal = new Terminal
            {
                TerminalName = request.TerminalName,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };
            _dbContext.Terminals.Add(terminal);
            await _dbContext.SaveChangesAsync();
            return Ok(new { success = true, message = "Terminal created successfully.", data = terminal });
        }
        catch (DbUpdateException)
        {
            return BadRequest(new { success = false, message = "A terminal with this name already exists." });
        }
        catch (Exception)
        {
            return StatusCode(500, new { success = false, message = "An error occurred while creating the terminal." });
        }
    }

    /// <summary>
    /// Updates an existing terminal (Admin only).
    /// The original record is preserved in history before the update.
    /// </summary>
    [HttpPut("terminals/{terminalId}")]
    public async Task<IActionResult> UpdateTerminal(int terminalId, [FromBody] CreateTerminalRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new { success = false, message = "Validation failed.", errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage) });
        }

        var terminal = await _dbContext.Terminals.FirstOrDefaultAsync(t => t.TerminalId == terminalId && t.DeletedAt == null);
        if (terminal == null)
        {
            return NotFound(new { success = false, message = "Terminal not found." });
        }

        // Record original data to history before updating
        _dbContext.TerminalEditHistories.Add(new TerminalEditHistory
        {
            OriginalRecordId = terminal.TerminalId,
            Operation = "EDIT",
            PerformedByUserId = User.GetAuthenticatedUserId()!.Value,
            PerformedAt = DateTime.UtcNow,
            OriginalData = JsonSerializer.Serialize(terminal),
            Reason = null
        });

        terminal.TerminalName = request.TerminalName;
        terminal.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync();
        return Ok(new { success = true, message = "Terminal updated successfully.", data = terminal });
    }

    /// <summary>
    /// Deletes (soft-deletes) a terminal (Admin only).
    /// The original record is preserved in history before deletion.
    /// </summary>
    [HttpDelete("terminals/{terminalId}")]
    public async Task<IActionResult> DeleteTerminal(int terminalId, [FromQuery] bool confirm = false)
    {
        var terminal = await _dbContext.Terminals.FirstOrDefaultAsync(t => t.TerminalId == terminalId && t.DeletedAt == null);
        if (terminal == null)
        {
            return NotFound(new { success = false, message = "Terminal not found." });
        }

        // Check if terminal is used in any fare rules
        var fareRulesCount = await _dbContext.FareRules
            .CountAsync(fr => (fr.OriginTerminalId == terminalId || fr.DestinationTerminalId == terminalId) 
                && fr.DeletedAt == null 
                && fr.IsActive);

        if (fareRulesCount > 0 && !confirm)
        {
            // Return warning that confirmation is required
            return Ok(new
            {
                success = false,
                warning = true,
                message = $"This terminal is used in {fareRulesCount} fare rule(s). Deleting it will also permanently delete those fare rules. This action cannot be undone.",
                affectedFareRules = fareRulesCount,
                requiresConfirmation = true
            });
        }

        if (fareRulesCount > 0 && confirm)
        {
            // Cascade delete: Remove all related fare rules first
            var relatedFareRules = await _dbContext.FareRules
                .Where(fr => (fr.OriginTerminalId == terminalId || fr.DestinationTerminalId == terminalId) 
                    && fr.DeletedAt == null 
                    && fr.IsActive)
                .ToListAsync();

            // Record fare rules to delete history
            foreach (var fareRule in relatedFareRules)
            {
                _dbContext.FareMatrixDeleteHistories.Add(new FareMatrixDeleteHistory
                {
                    OriginalRecordId = fareRule.FareId,
                    Operation = "CASCADE_DELETE",
                    PerformedByUserId = User.GetAuthenticatedUserId()!.Value,
                    PerformedAt = DateTime.UtcNow,
                    OriginalData = JsonSerializer.Serialize(fareRule),
                    Reason = "Terminal deleted"
                });
            }

            // Hard delete fare rules
            _dbContext.FareRules.RemoveRange(relatedFareRules);
            await _dbContext.SaveChangesAsync();
        }

        // Record terminal deletion to history
        _dbContext.TerminalDeleteHistories.Add(new TerminalDeleteHistory
        {
            OriginalRecordId = terminal.TerminalId,
            Operation = "DELETE",
            PerformedByUserId = User.GetAuthenticatedUserId()!.Value,
            PerformedAt = DateTime.UtcNow,
            OriginalData = JsonSerializer.Serialize(terminal),
            Reason = null
        });

        // Hard delete the terminal
        _dbContext.Terminals.Remove(terminal);
        await _dbContext.SaveChangesAsync();

        var message = fareRulesCount > 0 
            ? $"Terminal and {fareRulesCount} related fare rule(s) deleted successfully."
            : "Terminal deleted successfully.";

        return Ok(new { success = true, message = message });
    }

    [HttpGet("fare-rules")]
    public async Task<IActionResult> GetFareRules()
    {
        var fareRules = await _dbContext.FareRules
            .Include(fr => fr.OriginTerminal)
            .Include(fr => fr.DestinationTerminal)
            .Where(fr => fr.DeletedAt == null)
            .Select(fr => new
            {
                fr.FareId,
                originTerminalName = fr.OriginTerminal != null ? fr.OriginTerminal.TerminalName : "Unknown",
                destinationTerminalName = fr.DestinationTerminal != null ? fr.DestinationTerminal.TerminalName : "Unknown",
                fr.FareAmount,
                fr.EffectiveDate,
                fr.IsActive
            })
            .ToListAsync();
        return Ok(new { success = true, message = "Fare rules retrieved successfully.", data = fareRules });
    }

    [HttpPost("fare-rules")]
    public async Task<IActionResult> CreateFareRule([FromBody] CreateFareRuleRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new { success = false, message = "Validation failed.", errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage) });
            }

            // Prevent duplicate Origin/Destination combinations
            var duplicate = await _dbContext.FareRules
                .AnyAsync(fr => fr.OriginTerminalId == request.OriginTerminalId
                    && fr.DestinationTerminalId == request.DestinationTerminalId
                    && fr.DeletedAt == null
                    && fr.IsActive);

            if (duplicate)
            {
                return BadRequest(new { success = false, message = "A fare rule for this origin and destination already exists." });
            }

            // Validate terminals exist
            var originExists = await _dbContext.Terminals.AnyAsync(t => t.TerminalId == request.OriginTerminalId && t.DeletedAt == null);
            var destinationExists = await _dbContext.Terminals.AnyAsync(t => t.TerminalId == request.DestinationTerminalId && t.DeletedAt == null);
            
            if (!originExists || !destinationExists)
            {
                return BadRequest(new { success = false, message = "One or both selected terminals do not exist. Please refresh the terminal list and try again." });
            }

            // Ensure EffectiveDate is UTC
            var effectiveDate = request.EffectiveDate.Kind == DateTimeKind.Utc 
                ? request.EffectiveDate 
                : request.EffectiveDate.ToUniversalTime();

            var fareRule = new FareRule
            {
                OriginTerminalId = request.OriginTerminalId,
                DestinationTerminalId = request.DestinationTerminalId,
                VehicleType = Enums.VehicleType.BUS,
                PassengerType = Enums.PassengerType.Passenger,
                FareAmount = request.FareAmount,
                EffectiveDate = effectiveDate,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };
            _dbContext.FareRules.Add(fareRule);
            await _dbContext.SaveChangesAsync();
            return Ok(new { success = true, message = "Fare rule created successfully.", data = fareRule });
        }
        catch (DbUpdateException dbEx)
        {
            var dbError = new {
                success = false,
                message = $"Database error: {dbEx.Message}",
                innerException = dbEx.InnerException?.Message,
                innerInnerException = dbEx.InnerException?.InnerException?.Message
            };
            return StatusCode(500, dbError);
        }
        catch (Exception ex)
        {
            var errorDetails = new {
                success = false,
                message = $"General error: {ex.Message}",
                innerException = ex.InnerException?.Message,
                stackTrace = ex.StackTrace
            };
            return StatusCode(500, errorDetails);
        }
    }

    /// <summary>
    /// Updates an existing fare rule (Admin only).
    /// The original record is preserved in history before the update.
    /// </summary>
    [HttpPut("fare-rules/{fareId}")]
    public async Task<IActionResult> UpdateFareRule(int fareId, [FromBody] CreateFareRuleRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new { success = false, message = "Validation failed.", errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage) });
        }

        var fareRule = await _dbContext.FareRules.FirstOrDefaultAsync(fr => fr.FareId == fareId && fr.DeletedAt == null);
        if (fareRule == null)
        {
            return NotFound(new { success = false, message = "Fare rule not found." });
        }

        // Prevent duplicate Origin/Destination combinations (excluding this fare rule)
        var duplicate = await _dbContext.FareRules
            .AnyAsync(fr => fr.OriginTerminalId == request.OriginTerminalId
                && fr.DestinationTerminalId == request.DestinationTerminalId
                && fr.FareId != fareId
                && fr.DeletedAt == null
                && fr.IsActive);

        if (duplicate)
        {
            return BadRequest(new { success = false, message = "A fare rule for this origin and destination already exists." });
        }

        // Record original data to history before updating
        _dbContext.FareMatrixEditHistories.Add(new FareMatrixEditHistory
        {
            OriginalRecordId = fareRule.FareId,
            Operation = "EDIT",
            PerformedByUserId = User.GetAuthenticatedUserId()!.Value,
            PerformedAt = DateTime.UtcNow,
            OriginalData = JsonSerializer.Serialize(fareRule),
            Reason = null
        });

        // Ensure EffectiveDate is UTC
        var effectiveDate = request.EffectiveDate.Kind == DateTimeKind.Utc 
            ? request.EffectiveDate 
            : request.EffectiveDate.ToUniversalTime();

        fareRule.OriginTerminalId = request.OriginTerminalId;
        fareRule.DestinationTerminalId = request.DestinationTerminalId;
        fareRule.VehicleType = Enums.VehicleType.BUS;
        fareRule.PassengerType = Enums.PassengerType.Passenger;
        fareRule.FareAmount = request.FareAmount;
        fareRule.EffectiveDate = effectiveDate;
        fareRule.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync();
        return Ok(new { success = true, message = "Fare rule updated successfully.", data = fareRule });
    }

    /// <summary>
    /// Deletes (soft-deletes) a fare rule (Admin only).
    /// The original record is preserved in history before deletion.
    /// </summary>
    [HttpDelete("fare-rules/{fareId}")]
    public async Task<IActionResult> DeleteFareRule(int fareId)
    {
        var fareRule = await _dbContext.FareRules.FirstOrDefaultAsync(fr => fr.FareId == fareId && fr.DeletedAt == null);
        if (fareRule == null)
        {
            return NotFound(new { success = false, message = "Fare rule not found." });
        }

        // Record original data to history before deleting
        _dbContext.FareMatrixDeleteHistories.Add(new FareMatrixDeleteHistory
        {
            OriginalRecordId = fareRule.FareId,
            Operation = "DELETE",
            PerformedByUserId = User.GetAuthenticatedUserId()!.Value,
            PerformedAt = DateTime.UtcNow,
            OriginalData = JsonSerializer.Serialize(fareRule),
            Reason = null
        });

        fareRule.DeletedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync();
        return Ok(new { success = true, message = "Fare rule deleted successfully." });
    }

    [HttpGet("transactions")]
    public async Task<IActionResult> GetTransactions([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = _dbContext.Transactions
            .Include(t => t.Card).ThenInclude(c => c!.User)
            .Include(t => t.OriginTerminal)
            .Include(t => t.Terminal)
            .Where(t => t.DeletedAt == null);

        var total = await query.CountAsync();
        var transactions = await query
            .OrderByDescending(t => t.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(t => new
            {
                t.TransactionId,
                passengerName = t.Card!.User != null ? $"{t.Card.User.FirstName} {t.Card.User.LastName}".Trim() : "Unknown",
                originTerminalName = t.OriginTerminal != null ? t.OriginTerminal.TerminalName : "Unknown",
                destinationTerminalName = t.Terminal != null ? t.Terminal.TerminalName : "Unknown",
                cardNumber = CardFormatter.MaskCardNumber(t.Card!.CardNumber),
                t.Amount,
                t.TransactionType,
                t.TransactionName,
                t.TransactionReferenceNumber,
                t.ReferenceNumber,
                t.CreatedAt
            })
            .ToListAsync();

        return Ok(new
        {
            success = true,
            message = "Transactions retrieved successfully.",
            data = transactions,
            pagination = new { page, pageSize, total, totalPages = (int)Math.Ceiling(total / (double)pageSize) }
        });
    }

    [HttpGet("reports/summary")]
    public async Task<IActionResult> GetReportSummary()
    {
        var passengerRole = await _dbContext.Roles.FirstOrDefaultAsync(r => r.RoleName == RoleName.Passenger);
        var driverRole = await _dbContext.Roles.FirstOrDefaultAsync(r => r.RoleName == RoleName.Driver);

        // If roles don't exist, return 0 counts rather than throwing an exception
        var totalPassengers = passengerRole != null
            ? await _dbContext.Users.CountAsync(u => u.DeletedAt == null && u.RoleId == passengerRole.RoleId)
            : 0;

        var totalDrivers = driverRole != null
            ? await _dbContext.Users.CountAsync(u => u.DeletedAt == null && u.RoleId == driverRole.RoleId)
            : 0;

        var totalTerminals = await _dbContext.Terminals.CountAsync(t => t.DeletedAt == null);
        var totalTransactions = await _dbContext.Transactions.CountAsync(t => t.DeletedAt == null);
        var totalRevenue = await _dbContext.Transactions
            .Where(t => t.DeletedAt == null && t.TransactionType == TransactionType.PAYMENT)
            .SumAsync(t => (decimal?)t.Amount) ?? 0m;

        return Ok(new
        {
            success = true,
            message = "Report summary retrieved successfully.",
            data = new
            {
                totalPassengers,
                totalDrivers,
                totalTerminals,
                totalTransactions,
                totalRevenue
            }
        });
    }

    // ── Trip Management (Admin — Read-Only) ───────────────────────────────

    /// <summary>
    /// Retrieves all trips with pagination (Admin only).
    /// Administrators can only view trips — they cannot create, edit, end, or cancel them.
    /// </summary>
    [HttpGet("trips")]
    public async Task<IActionResult> GetTrips([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = _dbContext.Trips
            .Include(t => t.Driver)
            .Include(t => t.OriginTerminal)
            .Include(t => t.FinalDestinationTerminal)
            .OrderByDescending(t => t.CreatedAt);

        var total = await query.CountAsync();
        var trips = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(t => new
            {
                t.TripId,
                t.DriverId,
                driverName = t.Driver != null ? $"{t.Driver.FirstName} {t.Driver.LastName}".Trim() : "Unknown",
                t.OriginTerminalId,
                originTerminalName = t.OriginTerminal != null ? t.OriginTerminal.TerminalName : "Unknown",
                t.FinalDestinationTerminalId,
                finalDestinationTerminalName = t.FinalDestinationTerminal != null ? t.FinalDestinationTerminal.TerminalName : "Unknown",
                t.RouteName,
                t.TripStatus,
                t.StartedAt,
                t.EndedAt,
                t.PassengerCount,
                t.TotalRevenue,
                t.CreatedAt
            })
            .ToListAsync();

        return Ok(new
        {
            success = true,
            message = "Trips retrieved successfully.",
            data = trips,
            pagination = new { page, pageSize, total, totalPages = (int)Math.Ceiling(total / (double)pageSize) }
        });
    }

    /// <summary>
    /// Retrieves a specific trip by ID (Admin only).
    /// </summary>
    [HttpGet("trips/{tripId}")]
    public async Task<IActionResult> GetTripById(int tripId)
    {
        var trip = await _dbContext.Trips
            .Include(t => t.Driver)
            .Include(t => t.OriginTerminal)
            .Include(t => t.FinalDestinationTerminal)
            .Include(t => t.CurrentBoardingOriginTerminal)
            .FirstOrDefaultAsync(t => t.TripId == tripId);

        if (trip == null)
        {
            return NotFound(new { success = false, message = "Trip not found." });
        }

        return Ok(new
        {
            success = true,
            message = "Trip retrieved successfully.",
            data = new
            {
                trip.TripId,
                trip.DriverId,
                driverName = trip.Driver != null ? $"{trip.Driver.FirstName} {trip.Driver.LastName}".Trim() : "Unknown",
                trip.OriginTerminalId,
                originTerminalName = trip.OriginTerminal?.TerminalName,
                trip.FinalDestinationTerminalId,
                finalDestinationTerminalName = trip.FinalDestinationTerminal?.TerminalName,
                trip.CurrentBoardingOriginTerminalId,
                currentBoardingOriginTerminalName = trip.CurrentBoardingOriginTerminal?.TerminalName,
                trip.RouteName,
                trip.TripStatus,
                trip.StartedAt,
                trip.EndedAt,
                trip.PassengerCount,
                trip.TotalRevenue,
                trip.CreatedAt
            }
        });
    }
}

public class CreateFareRuleRequest
{
    [Required(ErrorMessage = "Origin terminal ID is required.")]
    public int OriginTerminalId { get; set; }

    [Required(ErrorMessage = "Destination terminal ID is required.")]
    public int DestinationTerminalId { get; set; }

    [Range(0.01, 10000, ErrorMessage = "Fare amount must be greater than 0.")]
    public decimal FareAmount { get; set; }

    public DateTime EffectiveDate { get; set; } = DateTime.UtcNow;
}
