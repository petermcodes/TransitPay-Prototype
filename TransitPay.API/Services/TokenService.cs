using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using TransitPay.API.Data;
using TransitPay.API.Enums;
using TransitPay.API.Interfaces;
using TransitPay.API.Models;

namespace TransitPay.API.Services;

/// <summary>
/// Service for creating and managing JWT access tokens and refresh tokens.
/// Uses the centralized ISecurityKeyProvider for signing key consistency.
/// </summary>
public class TokenService : ITokenService
{
    private readonly IConfiguration _configuration;
    private readonly TransitPayDbContext _dbContext;
    private readonly ISecurityKeyProvider _securityKeyProvider;
    private readonly ILogger<TokenService> _logger;

    /// <summary>
    /// Creates a new TokenService. The centralized <see cref="ISecurityKeyProvider"/>
    /// keeps JWT signing keys consistent across the application.
    /// </summary>
    public TokenService(
        IConfiguration configuration,
        TransitPayDbContext dbContext,
        ISecurityKeyProvider securityKeyProvider,
        ILogger<TokenService> logger)
    {
        _configuration = configuration;
        _dbContext = dbContext;
        _securityKeyProvider = securityKeyProvider;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<string> CreateTokenAsync(User user)
    {
        try
        {
            var role = await _dbContext.Roles.FindAsync(user.RoleId);
            var roleName = role?.RoleName ?? RoleName.Passenger;

            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, user.UserId.ToString()),
                new(ClaimTypes.Name, user.Username),
                new(ClaimTypes.MobilePhone, user.MobileNumber),
                new(ClaimTypes.Role, Enum.GetName(typeof(RoleName), roleName) ?? roleName.ToString())
            };

            // Use the centralized security key provider
            var key = _securityKeyProvider.GetSymmetricSecurityKey();
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var tokenExpiryHours = _configuration.GetValue<int?>("Jwt:ExpiryHours") ?? 8;
            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claims,
                expires: DateTime.Now.AddHours(tokenExpiryHours),
                signingCredentials: creds);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating JWT token for user: {UserId}", user.UserId);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<RefreshToken> CreateRefreshTokenAsync(int userId)
    {
        try
        {
            var token = new RefreshToken
            {
                UserId = userId,
                Token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64)),
                ExpiresAt = DateTime.UtcNow.AddDays(7),
                CreatedAt = DateTime.UtcNow,
                Revoked = false
            };

            _dbContext.RefreshTokens.Add(token);
            await _dbContext.SaveChangesAsync();

            return token;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating refresh token for user: {UserId}", userId);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<RefreshToken?> RotateRefreshTokenAsync(int userId, string currentToken)
    {
        try
        {
            var refreshToken = await _dbContext.RefreshTokens
                .FirstOrDefaultAsync(rt => rt.UserId == userId && rt.Token == currentToken);

            if (refreshToken == null)
            {
                _logger.LogWarning("Rotate failed - refresh token not found for user: {UserId}", userId);
                return null;
            }

            // Reuse detection: if this token was already rotated (revoked with a replacement),
            // an attacker may be replaying a stolen token. Revoke ALL tokens in the family.
            if (refreshToken.Revoked && refreshToken.ReplacedByTokenId.HasValue)
            {
                _logger.LogWarning("Refresh token reuse detected for user {UserId} — revoking entire token family", userId);

                // Find the family root: the original token that started this chain
                RefreshToken? root = refreshToken;
                while (root.ReplacedByTokenId.HasValue)
                {
                    var parent = await _dbContext.RefreshTokens.FindAsync(root.ReplacedByTokenId.Value);
                    if (parent == null || parent.UserId != userId)
                    {
                        break;
                    }
                    root = parent;
                }

                // Revoke all tokens in the family chain
                var allUserTokens = await _dbContext.RefreshTokens
                    .Where(rt => rt.UserId == userId && !rt.Revoked)
                    .ToListAsync();

                foreach (var t in allUserTokens)
                {
                    t.Revoked = true;
                }

                await _dbContext.SaveChangesAsync();
                return null;
            }

            if (refreshToken.Revoked || refreshToken.ExpiresAt < DateTime.UtcNow)
            {
                _logger.LogWarning("Rotate failed - token revoked or expired for user: {UserId}", userId);
                return null;
            }

            // Revoke the current token and create a replacement
            refreshToken.Revoked = true;

            var newToken = new RefreshToken
            {
                UserId = userId,
                Token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64)),
                ExpiresAt = DateTime.UtcNow.AddDays(7),
                CreatedAt = DateTime.UtcNow,
                Revoked = false
            };

            _dbContext.RefreshTokens.Add(newToken);
            await _dbContext.SaveChangesAsync();

            // Link the replacement back to the original token for reuse detection
            refreshToken.ReplacedByTokenId = newToken.TokenId;
            await _dbContext.SaveChangesAsync();

            _logger.LogInformation("Refresh token rotated for user: {UserId}", userId);
            return newToken;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error rotating refresh token for user: {UserId}", userId);
            return null;
        }
    }

    /// <inheritdoc />
    public async Task RevokeAllRefreshTokensAsync(int userId)
    {
        try
        {
            var activeTokens = await _dbContext.RefreshTokens
                .Where(rt => rt.UserId == userId && !rt.Revoked)
                .ToListAsync();

            foreach (var token in activeTokens)
            {
                token.Revoked = true;
            }

            await _dbContext.SaveChangesAsync();
            _logger.LogInformation("All refresh tokens revoked for user: {UserId} ({Count} tokens)", userId, activeTokens.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error revoking all refresh tokens for user: {UserId}", userId);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<bool> ValidateRefreshTokenAsync(int userId, string token)
    {
        try
        {
            var refreshToken = await _dbContext.RefreshTokens
                .FirstOrDefaultAsync(rt => rt.UserId == userId && rt.Token == token);

            if (refreshToken == null || refreshToken.Revoked || refreshToken.ExpiresAt < DateTime.UtcNow)
            {
                _logger.LogWarning("Invalid or expired refresh token for user: {UserId}", userId);
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating refresh token for user: {UserId}", userId);
            return false;
        }
    }
}
