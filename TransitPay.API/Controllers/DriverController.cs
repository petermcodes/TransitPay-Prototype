using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TransitPay.API.DTOs.Admin;
using TransitPay.API.Interfaces;

namespace TransitPay.API.Controllers;

/// <summary>
/// Manages Driver account lifecycle (Admin only).
/// All logic is delegated to IAdminService (Administration Domain).
/// Drivers are created active immediately — no approval workflow.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class DriverController : ControllerBase
{
    private readonly IAdminService _adminService;
    private readonly ILogger<DriverController> _logger;

    public DriverController(IAdminService adminService, ILogger<DriverController> logger)
    {
        _adminService = adminService;
        _logger = logger;
    }

    /// <summary>
    /// Retrieves all drivers (Admin only).
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetDrivers([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var (drivers, total) = await _adminService.GetDriversAsync(page, pageSize);

        return Ok(new
        {
            success = true,
            message = "Drivers retrieved successfully.",
            data = drivers.Select(u => new
            {
                u.UserId,
                u.FirstName,
                u.LastName,
                u.MobileNumber,
                u.Username,
                u.IsActive,
                u.CreatedAt
            }),
            pagination = new { page, pageSize, total, totalPages = (int)Math.Ceiling(total / (double)pageSize) }
        });
    }

    /// <summary>
    /// Creates a new Driver account (Admin only).
    /// The driver is created active immediately — no approval workflow.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreateDriver([FromBody] CreateDriverRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new { success = false, message = "Validation failed.", errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage) });
        }

        try
        {
            var driver = await _adminService.CreateDriverAsync(request);

            return Ok(new
            {
                success = true,
                message = "Driver created successfully.",
                data = new
                {
                    driver.UserId,
                    driver.FirstName,
                    driver.LastName,
                    driver.MobileNumber,
                    driver.IsActive,
                    driver.CreatedAt
                }
            });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning("Failed to create driver: {Message}", ex.Message);
            return BadRequest(new { success = false, message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating driver");
            return StatusCode(500, new { success = false, message = "An error occurred while creating the driver." });
        }
    }

    /// <summary>
    /// Activates a driver account (Admin only).
    /// </summary>
    [HttpPut("{id}/activate")]
    public async Task<IActionResult> ActivateDriver(int id)
    {
        try
        {
            var driver = await _adminService.ActivateUserAsync(id);

            return Ok(new
            {
                success = true,
                message = "Driver activated successfully.",
                data = new
                {
                    driver.UserId,
                    driver.IsActive
                }
            });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning("Failed to activate driver {UserId}: {Message}", id, ex.Message);
            return BadRequest(new { success = false, message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error activating driver {UserId}", id);
            return StatusCode(500, new { success = false, message = "An error occurred while activating the driver." });
        }
    }

    /// <summary>
    /// Deactivates a driver account (Admin only).
    /// Deactivated drivers cannot authenticate. No data is deleted.
    /// </summary>
    [HttpPut("{id}/deactivate")]
    public async Task<IActionResult> DeactivateDriver(int id)
    {
        try
        {
            var driver = await _adminService.DeactivateUserAsync(id);

            return Ok(new
            {
                success = true,
                message = "Driver deactivated successfully.",
                data = new
                {
                    driver.UserId,
                    driver.IsActive
                }
            });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning("Failed to deactivate driver {UserId}: {Message}", id, ex.Message);
            return BadRequest(new { success = false, message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deactivating driver {UserId}", id);
            return StatusCode(500, new { success = false, message = "An error occurred while deactivating the driver." });
        }
    }

    /// <summary>
    /// Resets a driver's password (Admin only).
    /// Applies the password policy and clears any account lockout.
    /// </summary>
    [HttpPost("{id}/reset-password")]
    public async Task<IActionResult> ResetDriverPassword(int id, [FromBody] ResetPasswordRequest request)
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
            _logger.LogWarning("Failed to reset password for driver {UserId}: {Message}", id, ex.Message);
            return BadRequest(new { success = false, message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resetting password for driver {UserId}", id);
            return StatusCode(500, new { success = false, message = "An error occurred while resetting the password." });
        }
    }

    /// <summary>
    /// Unlocks a locked driver account (Admin only).
    /// Clears lockout and resets failed login attempts.
    /// </summary>
    [HttpPost("{id}/unlock")]
    public async Task<IActionResult> UnlockDriver(int id)
    {
        try
        {
            await _adminService.UnlockUserAccountAsync(id);

            return Ok(new { success = true, message = "Driver account unlocked successfully." });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning("Failed to unlock driver {UserId}: {Message}", id, ex.Message);
            return BadRequest(new { success = false, message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error unlocking driver {UserId}", id);
            return StatusCode(500, new { success = false, message = "An error occurred while unlocking the driver." });
        }
    }
}