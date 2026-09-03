using TransitPay.API.Enums;

namespace TransitPay.API.DTOs.Auth;

/// <summary>
/// Response DTO returned by the login endpoint.
/// Carries the JWT access token, refresh token, and a summary of the authenticated user.
/// </summary>
public class LoginResponse
{
    /// <summary>Whether the login succeeded.</summary>
    public bool Success { get; set; }

    /// <summary>A human-readable result message (generic on failure to prevent account enumeration).</summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>The issued tokens and user profile. Null when the login failed.</summary>
    public LoginData? Data { get; set; }
}

/// <summary>
/// The token payload of a successful login.
/// </summary>
public class LoginData
{
    /// <summary>The short-lived JWT access token.</summary>
    public string Token { get; set; } = string.Empty;

    /// <summary>The long-lived refresh token used to obtain new access tokens.</summary>
    public string RefreshToken { get; set; } = string.Empty;

    /// <summary>The authenticated user's profile summary.</summary>
    public UserInfo User { get; set; } = new();
}

/// <summary>
/// A PII-bearing summary of the authenticated user. Returned only to the authenticated client.
/// </summary>
public class UserInfo
{
    /// <summary>The user's unique ID.</summary>
    public int UserId { get; set; }

    /// <summary>The login name (username or Driver ID).</summary>
    public string Username { get; set; } = string.Empty;

    /// <summary>The user's first name.</summary>
    public string FirstName { get; set; } = string.Empty;

    /// <summary>The user's last name.</summary>
    public string LastName { get; set; } = string.Empty;

    /// <summary>The user's mobile number.</summary>
    public string MobileNumber { get; set; } = string.Empty;

    /// <summary>The role ID assigned to the user.</summary>
    public int RoleId { get; set; }

    /// <summary>The role name (Passenger, Driver, Admin).</summary>
    public RoleName? RoleName { get; set; }
}
