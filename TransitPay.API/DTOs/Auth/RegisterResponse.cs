namespace TransitPay.API.DTOs.Auth;

public class RegisterResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public RegisterData? Data { get; set; }
}

public class RegisterData
{
    public int UserId { get; set; }
    public string Role { get; set; } = string.Empty;
}