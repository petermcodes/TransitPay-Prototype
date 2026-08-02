using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using TransitPay.API.Interfaces;

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
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new { success = false, message = "Validation failed.", errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage) });
        }

        var result = await _authService.RegisterAsync(request.FirstName, request.LastName, request.MobileNumber, request.Password, request.RoleName);
        return Ok(result);
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new { success = false, message = "Validation failed.", errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage) });
        }

        var result = await _authService.LoginAsync(request.MobileNumber, request.Password);
        return Ok(result);
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh([FromBody] RefreshTokenRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new { success = false, message = "Validation failed.", errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage) });
        }

        var result = await _authService.RefreshTokenAsync(request.UserId, request.RefreshToken);
        return Ok(result);
    }
}

public class RegisterRequest
{
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

    [RegularExpression("^(Passenger|Driver|Admin)$", ErrorMessage = "Role must be Passenger, Driver, or Admin.")]
    public string RoleName { get; set; } = "Passenger";
}

public class LoginRequest
{
    [Required(ErrorMessage = "Mobile number is required.")]
    [RegularExpression(@"^09\d{9}$", ErrorMessage = "Mobile number must be a valid Philippine number (e.g., 09171234567).")]
    public string MobileNumber { get; set; } = string.Empty;

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
