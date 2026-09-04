using TransitPay.API.Models;

namespace TransitPay.API.Interfaces;

/// <summary>
/// Creates and manages JWT access tokens and refresh tokens.
/// Refresh tokens are persisted and support revocation (logout), rotation (each refresh
/// issues a new token), and family reuse-detection for theft mitigation.
/// </summary>
public interface ITokenService
{
    /// <summary>
    /// Creates a signed JWT access token for the user.
    /// The token embeds the user ID, username, mobile number, and role as claims, is signed
    /// with HS256 using the centralized security key, and expires per Jwt:ExpiryHours
    /// (default 8 hours).
    /// </summary>
    /// <param name="user">The user to issue the token for.</param>
    /// <returns>The serialized JWT string.</returns>
    Task<string> CreateTokenAsync(User user);

    /// <summary>
    /// Creates and persists a new refresh token for the user.
    /// The token value is 64 cryptographically random bytes (Base64-encoded) and
    /// expires after 7 days.
    /// </summary>
    /// <param name="userId">The user ID the refresh token belongs to.</param>
    /// <returns>The persisted <see cref="RefreshToken"/> entity.</returns>
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
