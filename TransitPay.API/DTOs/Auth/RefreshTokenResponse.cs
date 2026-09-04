namespace TransitPay.API.DTOs.Auth;

/// <summary>
/// Response DTO returned by the token refresh endpoint.
/// Carries a fresh JWT access token and the rotated refresh token.
/// </summary>
public class RefreshTokenResponse
{
    /// <summary>Whether the refresh succeeded.</summary>
    public bool Success { get; set; }

    /// <summary>A human-readable result message.</summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>The fresh tokens. Null when the refresh failed.</summary>
    public RefreshTokenData? Data { get; set; }
}

/// <summary>
/// The token payload returned after a successful refresh.
/// </summary>
public class RefreshTokenData
{
    /// <summary>The newly issued JWT access token.</summary>
    public string Token { get; set; } = string.Empty;

    /// <summary>The rotated refresh token (the previous one is revoked).</summary>
    public string RefreshToken { get; set; } = string.Empty;
}