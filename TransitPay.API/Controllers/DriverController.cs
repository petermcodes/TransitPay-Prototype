using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TransitPay.API.Data;
using TransitPay.API.Enums;
using TransitPay.API.Models;

namespace TransitPay.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class DriverController : ControllerBase
{
    private readonly TransitPayDbContext _dbContext;
    private readonly PasswordHasher<User> _passwordHasher;
    private readonly ILogger<DriverController> _logger;

    public DriverController(TransitPayDbContext dbContext, PasswordHasher<User> passwordHasher, ILogger<DriverController> logger)
    {
        _dbContext = dbContext;
        _passwordHasher = passwordHasher;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetDrivers()
    {
        var driverRole = await _dbContext.Roles.FirstOrDefaultAsync(r => r.RoleName == RoleName.Driver);
        if (driverRole == null)
        {
            return Ok(new { success = true, message = "Drivers retrieved successfully.", data = new List<object>() });
        }

        var drivers = await _dbContext.Users
            .Where(u => u.RoleId == driverRole.RoleId)
            .Select(u => new
            {
                u.UserId,
                u.FirstName,
                u.LastName,
                u.MobileNumber,
                u.Username,
                u.IsActive,
                u.CreatedAt
            })
            .ToListAsync();

        return Ok(new { success = true, message = "Drivers retrieved successfully.", data = drivers });
    }

    [HttpPost]
    public async Task<IActionResult> CreateDriver([FromBody] CreateDriverRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new { success = false, message = "Validation failed.", errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage) });
        }

        try
        {
            var driverRole = await _dbContext.Roles.FirstOrDefaultAsync(r => r.RoleName == RoleName.Driver);
            if (driverRole == null)
            {
                return BadRequest(new { success = false, message = "Driver role not found." });
            }

            var existingUser = await _dbContext.Users.FirstOrDefaultAsync(u => u.MobileNumber == request.MobileNumber);
            if (existingUser != null)
            {
                return BadRequest(new { success = false, message = "A user with this mobile number already exists." });
            }

            var driver = new User
            {
                Username = request.MobileNumber,
                FirstName = request.FirstName,
                LastName = request.LastName,
                MobileNumber = request.MobileNumber,
                PasswordHash = _passwordHasher.HashPassword(null!, request.Password),
                IsActive = false, // Drivers start as pending until approved
                RoleId = driverRole.RoleId,
                CreatedAt = DateTime.UtcNow
            };

            _dbContext.Users.Add(driver);
            await _dbContext.SaveChangesAsync();

            _logger.LogInformation("Driver created successfully. UserId: {UserId}", driver.UserId);

            return Ok(new
            {
                success = true,
                message = "Driver created successfully. Pending approval.",
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
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating driver");
            return StatusCode(500, new { success = false, message = "An error occurred while creating the driver." });
        }
    }

    [HttpPut("{id}/approve")]
    public async Task<IActionResult> ApproveDriver(int id)
    {
        try
        {
            var driver = await _dbContext.Users.FindAsync(id);
            if (driver == null)
            {
                return NotFound(new { success = false, message = "Driver not found." });
            }

            var driverRole = await _dbContext.Roles.FirstOrDefaultAsync(r => r.RoleName == RoleName.Driver);
            if (driverRole == null || driver.RoleId != driverRole.RoleId)
            {
                return BadRequest(new { success = false, message = "User is not a driver." });
            }

            driver.IsActive = true;
            await _dbContext.SaveChangesAsync();

            _logger.LogInformation("Driver approved. UserId: {UserId}", id);

            return Ok(new { success = true, message = "Driver approved successfully." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error approving driver {UserId}", id);
            return StatusCode(500, new { success = false, message = "An error occurred while approving the driver." });
        }
    }

    [HttpPut("{id}/reject")]
    public async Task<IActionResult> RejectDriver(int id)
    {
        try
        {
            var driver = await _dbContext.Users.FindAsync(id);
            if (driver == null)
            {
                return NotFound(new { success = false, message = "Driver not found." });
            }

            _dbContext.Users.Remove(driver);
            await _dbContext.SaveChangesAsync();

            _logger.LogInformation("Driver rejected and removed. UserId: {UserId}", id);

            return Ok(new { success = true, message = "Driver rejected and removed." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error rejecting driver {UserId}", id);
            return StatusCode(500, new { success = false, message = "An error occurred while rejecting the driver." });
        }
    }
}

public class CreateDriverRequest
{
    [Required(ErrorMessage = "First name is required.")]
    [StringLength(50, MinimumLength = 2)]
    public string FirstName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Last name is required.")]
    [StringLength(50, MinimumLength = 2)]
    public string LastName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Mobile number is required.")]
    [StringLength(15, MinimumLength = 10)]
    public string MobileNumber { get; set; } = string.Empty;

    [Required(ErrorMessage = "Password is required.")]
    [StringLength(100, MinimumLength = 8)]
    public string Password { get; set; } = string.Empty;
}