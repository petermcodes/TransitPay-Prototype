using TransitPay.API.Models;

namespace TransitPay.API.Interfaces;

public interface ITokenService
{
    Task<string> CreateTokenAsync(User user);
    Task<RefreshToken> CreateRefreshTokenAsync(int userId);

    /// <summary>
    /// Rotates a refresh token: validates the current token, revokes it,
    /// and issues a new token linked via ReplacedByTokenId.
    /// Returns null if the current token is invalid, expired, or revoked.
    /// </summary>
    Task<RefreshToken?> RotateRefreshTokenAsync(int userId, string currentToken);

    /// <summary>
    /// Revokes all active refresh tokens for a user.
    /// Used for secure logout.
    /// </summary>
    Task RevokeAllRefreshTokensAsync(int userId);

    /// <summary>
    /// Validates a refresh token for reuse detection.
    /// If the token has already been rotated (revoked with ReplacedByTokenId set),
    /// ALL tokens in the family are revoked to mitigate token theft.
    /// </summary>
    Task<bool> ValidateRefreshTokenAsync(int userId, string token);
}
