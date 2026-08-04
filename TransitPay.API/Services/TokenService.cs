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

    public async Task<string> CreateTokenAsync(User user)
    {
        try
        {
            var role = await _dbContext.Roles.FindAsync(user.RoleId);

            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, user.UserId.ToString()),
                new(ClaimTypes.Name, user.Username),
                new(ClaimTypes.MobilePhone, user.MobileNumber),
                new(ClaimTypes.Role, (role?.RoleName ?? RoleName.Passenger).ToString())
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

    public async Task RevokeRefreshTokenAsync(int userId, string token)
    {
        try
        {
            var refreshToken = await _dbContext.RefreshTokens
                .FirstOrDefaultAsync(rt => rt.UserId == userId && rt.Token == token);

            if (refreshToken != null)
            {
                refreshToken.Revoked = true;
                await _dbContext.SaveChangesAsync();
                _logger.LogInformation("Refresh token revoked for user: {UserId}", userId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error revoking refresh token for user: {UserId}", userId);
            throw;
        }
    }
}