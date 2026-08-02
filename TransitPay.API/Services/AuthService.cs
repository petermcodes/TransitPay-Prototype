using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TransitPay.API.Data;
using TransitPay.API.Interfaces;
using TransitPay.API.Models;

namespace TransitPay.API.Services;

public class AuthService : IAuthService
{
    private readonly TransitPayDbContext _dbContext;
    private readonly PasswordHasher<User> _passwordHasher;
    private readonly ITokenService _tokenService;

    public AuthService(TransitPayDbContext dbContext, PasswordHasher<User> passwordHasher, ITokenService tokenService)
    {
        _dbContext = dbContext;
        _passwordHasher = passwordHasher;
        _tokenService = tokenService;
    }

    public async Task<object> RegisterAsync(string firstName, string lastName, string mobileNumber, string password, string roleName)
    {
        var role = await _dbContext.Roles.FirstOrDefaultAsync(r => r.RoleName == roleName);
        if (role == null)
        {
            return new { success = false, message = "Role not found." };
        }

        var existingUser = await _dbContext.Users.FirstOrDefaultAsync(u => u.MobileNumber == mobileNumber);
        if (existingUser != null)
        {
            return new { success = false, message = "User already exists." };
        }

        var user = new User
        {
            Username = mobileNumber,
            FirstName = firstName,
            LastName = lastName,
            MobileNumber = mobileNumber,
            PasswordHash = _passwordHasher.HashPassword(null!, password),
            IsActive = true,
            RoleId = role.RoleId,
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync();

        return new { success = true, message = "User registered successfully.", data = new { userId = user.UserId, role = role.RoleName } };
    }

    public async Task<object> LoginAsync(string mobileNumber, string password)
    {
        var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.MobileNumber == mobileNumber);
        if (user == null)
        {
            return new { success = false, message = "Invalid credentials." };
        }

        var verificationResult = _passwordHasher.VerifyHashedPassword(null!, user.PasswordHash, password);
        if (verificationResult != PasswordVerificationResult.Success)
        {
            return new { success = false, message = "Invalid credentials." };
        }

        var role = await _dbContext.Roles.FindAsync(user.RoleId);
        var token = await _tokenService.CreateTokenAsync(user);
        var refreshToken = await _tokenService.CreateRefreshTokenAsync(user.UserId);

        return new
        {
            success = true,
            message = "Login successful.",
            data = new
            {
                token,
                refreshToken = refreshToken.Token,
                user = new
                {
                    user.UserId,
                    user.FirstName,
                    user.LastName,
                    user.MobileNumber,
                    roleId = user.RoleId,
                    roleName = role?.RoleName
                }
            }
        };
    }

    public async Task<object> RefreshTokenAsync(int userId, string refreshToken)
    {
        var isValid = await _tokenService.ValidateRefreshTokenAsync(userId, refreshToken);
        if (!isValid)
        {
            return new { success = false, message = "Invalid or expired refresh token." };
        }

        var user = await _dbContext.Users.FindAsync(userId);
        if (user == null)
        {
            return new { success = false, message = "User not found." };
        }

        await _tokenService.RevokeRefreshTokenAsync(userId, refreshToken);
        var newToken = await _tokenService.CreateTokenAsync(user);
        var newRefreshToken = await _tokenService.CreateRefreshTokenAsync(user.UserId);

        return new
        {
            success = true,
            message = "Token refreshed successfully.",
            data = new
            {
                token = newToken,
                refreshToken = newRefreshToken.Token
            }
        };
    }
}
