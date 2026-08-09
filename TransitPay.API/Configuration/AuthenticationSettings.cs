namespace TransitPay.API.Configuration;

/// <summary>
/// Configuration settings for authentication security policies.
/// Bound from the "Authentication" section of appsettings.json.
/// </summary>
public class AuthenticationSettings
{
    /// <summary>
    /// The number of consecutive failed login attempts before the account is locked out.
    /// </summary>
    public int MaxFailedAttempts { get; set; } = 5;

    /// <summary>
    /// The duration (in minutes) an account is locked out after exceeding
    /// MaxFailedAttempts failed login attempts.
    /// </summary>
    public int LockoutMinutes { get; set; } = 15;
}