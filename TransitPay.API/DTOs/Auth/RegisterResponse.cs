namespace TransitPay.API.DTOs.Auth;

/// <summary>
/// Response DTO returned by the registration endpoint.
/// Confirms the created passenger account and the role assigned server-side.
/// </summary>
public class RegisterResponse
{
    /// <summary>Whether the registration succeeded.</summary>
    public bool Success { get; set; }

    /// <summary>A human-readable result message.</summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>The created account summary. Null when registration failed.</summary>
    public RegisterData? Data { get; set; }
}

/// <summary>
/// The account summary returned after a successful registration.
/// </summary>
public class RegisterData
{
    /// <summary>The new user's unique ID.</summary>
    public int UserId { get; set; }

    /// <summary>The role assigned to the account (always "Passenger" for self-service registration).</summary>
    public string Role { get; set; } = string.Empty;
}