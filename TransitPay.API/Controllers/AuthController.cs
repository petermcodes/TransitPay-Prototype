using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using TransitPay.API.DTOs.Auth;
using TransitPay.API.Interfaces;
using TransitPay.API.Utilities;

namespace TransitPay.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("register")]
    [EnableRateLimiting("auth")]
    public async Task<ActionResult<RegisterResponse>> Register([FromBody] RegisterRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new { success = false, message = "Validation failed.", errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage) });
        }

        var result = await _authService.RegisterAsync(request.Username, request.FirstName, request.LastName, request.MobileNumber, request.Password);
        if (!result.Success)
        {
            return BadRequest(result);
        }
        return Ok(result);
    }

    [HttpPost("login")]
    [EnableRateLimiting("auth")]
    public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new { success = false, message = "Validation failed.", errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage) });
        }

        var result = await _authService.LoginAsync(request.Username, request.Password);
        if (!result.Success)
        {
            return BadRequest(result);
        }
        return Ok(result);
    }

    [HttpPost("refresh")]
    [EnableRateLimiting("auth")]
    public async Task<ActionResult<RefreshTokenResponse>> Refresh([FromBody] RefreshTokenRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new { success = false, message = "Validation failed.", errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage) });
        }

        var result = await _authService.RefreshTokenAsync(request.UserId, request.RefreshToken);
        if (!result.Success)
        {
            return BadRequest(result);
        }
        return Ok(result);
    }

    /// <summary>
    /// Securely logs out the authenticated user by revoking all active refresh tokens.
    /// The JWT access token expires naturally.
    /// </summary>
    [HttpPost("logout")]
    [Authorize]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> Logout()
    {
        var userId = User.GetAuthenticatedUserId();
        if (userId == null)
        {
            return Unauthorized(new { success = false, message = "User not authenticated." });
        }

        var success = await _authService.LogoutAsync(userId.Value);
        if (!success)
        {
            return StatusCode(500, new { success = false, message = "An error occurred during logout." });
        }

        return Ok(new { success = true, message = "Logout successful." });
    }

    [HttpGet("validate")]
    [Authorize]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> ValidateToken()
    {
        var userId = User.GetAuthenticatedUserId();
        if (userId == null)
        {
            return Unauthorized(new { success = false, message = "Invalid or expired token." });
        }

        // Token is valid - return user info
        var user = await _authService.GetUserByIdAsync(userId.Value);
        if (user == null || user.DeletedAt != null || !user.IsActive)
        {
            return Unauthorized(new { success = false, message = "User not found or inactive." });
        }

        return Ok(new { 
            success = true, 
            message = "Token is valid.", 
            data = new {
                user.UserId,
                user.Username,
                user.FirstName,
                user.LastName,
                user.MobileNumber,
                user.RoleId
            }
        });
    }
}

public class RegisterRequest
{
    [Required(ErrorMessage = "Username is required.")]
    [StringLength(50, MinimumLength = 3, ErrorMessage = "Username must be between 3 and 50 characters.")]
    [RegularExpression(@"^[a-zA-Z0-9_-]+$", ErrorMessage = "Username can only contain letters, numbers, underscores, and hyphens.")]
    public string Username { get; set; } = string.Empty;

    [Required(ErrorMessage = "First name is required.")]
    [StringLength(50, MinimumLength = 2, ErrorMessage = "First name must be between 2 and 50 characters.")]
    public string FirstName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Last name is required.")]
    [StringLength(50, MinimumLength = 2, ErrorMessage = "Last name must be between 2 and 50 characters.")]
    public string LastName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Mobile number is required.")]
    [RegularExpression(@"^09\d{9}$", ErrorMessage = "Mobile number must be a valid Philippine number (e.g., 09171234567).")]
    public string MobileNumber { get; set; } = string.Empty;

    [Required(ErrorMessage = "Password is required.")]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "Password must be at least 8 characters.")]
    public string Password { get; set; } = string.Empty;
}

public class LoginRequest
{
    [Required(ErrorMessage = "Username is required.")]
    [StringLength(50, MinimumLength = 1, ErrorMessage = "Username is required.")]
    public string Username { get; set; } = string.Empty;

    [Required(ErrorMessage = "Password is required.")]
    public string Password { get; set; } = string.Empty;
}

public class RefreshTokenRequest
{
    [Required(ErrorMessage = "User ID is required.")]
    public int UserId { get; set; }

    [Required(ErrorMessage = "Refresh token is required.")]
    public string RefreshToken { get; set; } = string.Empty;
}