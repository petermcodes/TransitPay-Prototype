using System.ComponentModel.DataAnnotations;

namespace TransitPay.API.DTOs.Admin;

/// <summary>
/// Request DTO for creating a Driver account (Admin only).
/// Drivers are created active immediately — no approval workflow.
/// </summary>
public class CreateDriverRequest
{
    /// <summary>The driver's first name.</summary>
    [Required(ErrorMessage = "First name is required.")]
    [StringLength(50, MinimumLength = 2, ErrorMessage = "First name must be between 2 and 50 characters.")]
    public string FirstName { get; set; } = string.Empty;

    /// <summary>The driver's last name.</summary>
    [Required(ErrorMessage = "Last name is required.")]
    [StringLength(50, MinimumLength = 2, ErrorMessage = "Last name must be between 2 and 50 characters.")]
    public string LastName { get; set; } = string.Empty;

    /// <summary>The driver's Philippine mobile number (e.g., 09171234567).</summary>
    [Required(ErrorMessage = "Mobile number is required.")]
    [RegularExpression(@"^09\d{9}$", ErrorMessage = "Mobile number must be a valid Philippine number (e.g., 09171234567).")]
    public string MobileNumber { get; set; } = string.Empty;

    /// <summary>Initial password. When omitted, the Driver ID (DRV-xxxxxx) becomes the default password.</summary>
    [StringLength(100, MinimumLength = 8, ErrorMessage = "Password must be at least 8 characters.")]
    public string? Password { get; set; }
}

/// <summary>
/// Request DTO for creating an Administrator account (Admin only).
/// Only existing Administrators or the initial bootstrap may create additional Admins.
/// </summary>
public class CreateAdministratorRequest
{
    /// <summary>The admin's login username.</summary>
    [Required(ErrorMessage = "Username is required.")]
    [StringLength(50, MinimumLength = 3, ErrorMessage = "Username must be between 3 and 50 characters.")]
    public string Username { get; set; } = string.Empty;

    /// <summary>The admin's first name.</summary>
    [Required(ErrorMessage = "First name is required.")]
    [StringLength(50, MinimumLength = 2, ErrorMessage = "First name must be between 2 and 50 characters.")]
    public string FirstName { get; set; } = string.Empty;

    /// <summary>The admin's last name.</summary>
    [Required(ErrorMessage = "Last name is required.")]
    [StringLength(50, MinimumLength = 2, ErrorMessage = "Last name must be between 2 and 50 characters.")]
    public string LastName { get; set; } = string.Empty;

    /// <summary>The admin's Philippine mobile number (e.g., 09171234567).</summary>
    [Required(ErrorMessage = "Mobile number is required.")]
    [RegularExpression(@"^09\d{9}$", ErrorMessage = "Mobile number must be a valid Philippine number (e.g., 09171234567).")]
    public string MobileNumber { get; set; } = string.Empty;

    /// <summary>The admin's initial password.</summary>
    [Required(ErrorMessage = "Password is required.")]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "Password must be at least 8 characters.")]
    public string Password { get; set; } = string.Empty;
}

/// <summary>
/// Request DTO for updating a user's personal information (Admin only).
/// </summary>
public class UpdateUserRequest
{
    /// <summary>The user's first name.</summary>
    [Required(ErrorMessage = "First name is required.")]
    [StringLength(50, MinimumLength = 2, ErrorMessage = "First name must be between 2 and 50 characters.")]
    public string FirstName { get; set; } = string.Empty;

    /// <summary>The user's last name.</summary>
    [Required(ErrorMessage = "Last name is required.")]
    [StringLength(50, MinimumLength = 2, ErrorMessage = "Last name must be between 2 and 50 characters.")]
    public string LastName { get; set; } = string.Empty;

    /// <summary>The user's Philippine mobile number (e.g., 09171234567).</summary>
    [Required(ErrorMessage = "Mobile number is required.")]
    [RegularExpression(@"^09\d{9}$", ErrorMessage = "Mobile number must be a valid Philippine number (e.g., 09171234567).")]
    public string MobileNumber { get; set; } = string.Empty;
}

/// <summary>
/// Request DTO for resetting a user's password (Admin only).
/// </summary>
public class ResetPasswordRequest
{
    /// <summary>The new password, validated against the TransitPay password policy.</summary>
    [Required(ErrorMessage = "New password is required.")]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "Password must be at least 8 characters.")]
    public string NewPassword { get; set; } = string.Empty;
}