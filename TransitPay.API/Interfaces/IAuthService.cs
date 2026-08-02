namespace TransitPay.API.Interfaces;

public interface IAuthService
{
    Task<object> RegisterAsync(string firstName, string lastName, string mobileNumber, string password, string roleName);
    Task<object> LoginAsync(string mobileNumber, string password);
    Task<object> RefreshTokenAsync(int userId, string refreshToken);
}
