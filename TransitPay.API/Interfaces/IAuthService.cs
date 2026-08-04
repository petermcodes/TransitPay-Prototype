namespace TransitPay.API.Interfaces;

using TransitPay.API.DTOs.Auth;

public interface IAuthService
{
    Task<RegisterResponse> RegisterAsync(string firstName, string lastName, string mobileNumber, string password, string roleName);
    Task<LoginResponse> LoginAsync(string mobileNumber, string password);
    Task<RefreshTokenResponse> RefreshTokenAsync(int userId, string refreshToken);
}
