using TransitPay.API.Models;

namespace TransitPay.API.Interfaces;

public interface ITokenService
{
    Task<string> CreateTokenAsync(User user);
    Task<RefreshToken> CreateRefreshTokenAsync(int userId);
    Task<bool> ValidateRefreshTokenAsync(int userId, string token);
    Task RevokeRefreshTokenAsync(int userId, string token);
}
