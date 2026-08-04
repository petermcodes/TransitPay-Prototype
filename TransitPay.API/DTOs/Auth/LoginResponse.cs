namespace TransitPay.API.DTOs.Auth;

public class LoginResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public LoginData? Data { get; set; }
}

public class LoginData
{
    public string Token { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public UserInfo User { get; set; } = new();
}

public class UserInfo
{
    public int UserId { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string MobileNumber { get; set; } = string.Empty;
    public int RoleId { get; set; }
    public string? RoleName { get; set; }
}