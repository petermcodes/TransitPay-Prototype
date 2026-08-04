namespace TransitPay.API.DTOs.Auth;

public class RefreshTokenResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public RefreshTokenData? Data { get; set; }
}

public class RefreshTokenData
{
    public string Token { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
}