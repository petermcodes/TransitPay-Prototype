using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TransitPay.API.Data;
using TransitPay.API.DTOs.Auth;
using TransitPay.API.Enums;
using TransitPay.API.Interfaces;
using TransitPay.API.Models;

namespace TransitPay.API.Services;

public class AuthService : IAuthService
{
    private readonly TransitPayDbContext _dbContext;
    private readonly PasswordHasher<User> _passwordHasher;
    private readonly ITokenService _tokenService;
    private readonly ILogger<AuthService> _logger;

    public AuthService(TransitPayDbContext dbContext, PasswordHasher<User> passwordHasher, ITokenService tokenService, ILogger<AuthService> logger)
    {
        _dbContext = dbContext;
        _passwordHasher = passwordHasher;
        _tokenService = tokenService;
        _logger = logger;
    }

    public async Task<RegisterResponse> RegisterAsync(string firstName, string lastName, string mobileNumber, string password, string roleName)
    {
        _logger.LogInformation("Registration attempt for mobile number: {MobileNumber}", mobileNumber);

        // Validate inputs
        if (string.IsNullOrWhiteSpace(firstName) || firstName.Length < 2)
        {
            _logger.LogWarning("Registration failed - invalid first name: {FirstName}", firstName);
            return new RegisterResponse { Success = false, Message = "First name must be at least 2 characters." };
        }

        if (string.IsNullOrWhiteSpace(lastName) || lastName.Length < 2)
        {
            _logger.LogWarning("Registration failed - invalid last name: {LastName}", lastName);
            return new RegisterResponse { Success = false, Message = "Last name must be at least 2 characters." };
        }

        if (string.IsNullOrWhiteSpace(mobileNumber) || mobileNumber.Length < 10)
        {
            _logger.LogWarning("Registration failed - invalid mobile number: {MobileNumber}", mobileNumber);
            return new RegisterResponse { Success = false, Message = "Mobile number must be at least 10 digits." };
        }

        if (string.IsNullOrWhiteSpace(password) || password.Length < 8)
        {
            _logger.LogWarning("Registration failed - weak password for mobile: {MobileNumber}", mobileNumber);
            return new RegisterResponse { Success = false, Message = "Password must be at least 8 characters." };
        }

        if (string.IsNullOrWhiteSpace(roleName))
        {
            _logger.LogWarning("Registration failed - missing role name for mobile: {MobileNumber}", mobileNumber);
            return new RegisterResponse { Success = false, Message = "Role name is required." };
        }

        try
        {
            var role = await _dbContext.Roles.FirstOrDefaultAsync(r => r.RoleName == Enum.Parse<RoleName>(roleName));
            if (role == null)
            {
                _logger.LogWarning("Registration failed - role not found: {RoleName}", roleName);
                return new RegisterResponse { Success = false, Message = "Role not found." };
            }

            var existingUser = await _dbContext.Users.FirstOrDefaultAsync(u => u.MobileNumber == mobileNumber);
            if (existingUser != null)
            {
                _logger.LogWarning("Registration failed - user already exists: {MobileNumber}", mobileNumber);
                return new RegisterResponse { Success = false, Message = "User already exists." };
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

            _logger.LogInformation("User registered successfully. UserId: {UserId}, Mobile: {MobileNumber}", user.UserId, mobileNumber);

            return new RegisterResponse
            {
                Success = true,
                Message = "User registered successfully.",
                Data = new RegisterData { UserId = user.UserId, Role = role.RoleName.ToString() }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during registration for mobile number: {MobileNumber}", mobileNumber);
            return new RegisterResponse { Success = false, Message = "An error occurred during registration." };
        }
    }

    public async Task<LoginResponse> LoginAsync(string mobileNumber, string password)
    {
        _logger.LogInformation("Login attempt for mobile number: {MobileNumber}", mobileNumber);

        if (string.IsNullOrWhiteSpace(mobileNumber) || string.IsNullOrWhiteSpace(password))
        {
            _logger.LogWarning("Login failed - missing credentials");
            return new LoginResponse { Success = false, Message = "Invalid credentials." };
        }

        try
        {
            var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.MobileNumber == mobileNumber);
            if (user == null)
            {
                _logger.LogWarning("Login failed - user not found: {MobileNumber}", mobileNumber);
                return new LoginResponse { Success = false, Message = "Invalid credentials." };
            }

            var verificationResult = _passwordHasher.VerifyHashedPassword(null!, user.PasswordHash, password);
            if (verificationResult != PasswordVerificationResult.Success)
            {
                _logger.LogWarning("Login failed - invalid password for user: {UserId}", user.UserId);
                return new LoginResponse { Success = false, Message = "Invalid credentials." };
            }

            var role = await _dbContext.Roles.FindAsync(user.RoleId);
            var token = await _tokenService.CreateTokenAsync(user);
            var refreshToken = await _tokenService.CreateRefreshTokenAsync(user.UserId);

            _logger.LogInformation("Login successful for user: {UserId}", user.UserId);

            return new LoginResponse
            {
                Success = true,
                Message = "Login successful.",
                Data = new LoginData
                {
                    Token = token,
                    RefreshToken = refreshToken.Token,
                    User = new UserInfo
                    {
                        UserId = user.UserId,
                        FirstName = user.FirstName,
                        LastName = user.LastName,
                        MobileNumber = user.MobileNumber,
                        RoleId = user.RoleId,
                        RoleName = role?.RoleName.ToString()
                    }
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during login for mobile number: {MobileNumber}", mobileNumber);
            return new LoginResponse { Success = false, Message = "An error occurred during login." };
        }
    }

    public async Task<RefreshTokenResponse> RefreshTokenAsync(int userId, string refreshToken)
    {
        _logger.LogInformation("Token refresh attempt for user: {UserId}", userId);

        if (userId <= 0 || string.IsNullOrWhiteSpace(refreshToken))
        {
            _logger.LogWarning("Token refresh failed - invalid input for user: {UserId}", userId);
            return new RefreshTokenResponse { Success = false, Message = "Invalid or expired refresh token." };
        }

        try
        {
            var isValid = await _tokenService.ValidateRefreshTokenAsync(userId, refreshToken);
            if (!isValid)
            {
                _logger.LogWarning("Token refresh failed - invalid or expired token for user: {UserId}", userId);
                return new RefreshTokenResponse { Success = false, Message = "Invalid or expired refresh token." };
            }

            var user = await _dbContext.Users.FindAsync(userId);
            if (user == null)
            {
                _logger.LogWarning("Token refresh failed - user not found: {UserId}", userId);
                return new RefreshTokenResponse { Success = false, Message = "User not found." };
            }

            await _tokenService.RevokeRefreshTokenAsync(userId, refreshToken);
            var newToken = await _tokenService.CreateTokenAsync(user);
            var newRefreshToken = await _tokenService.CreateRefreshTokenAsync(user.UserId);

            _logger.LogInformation("Token refreshed successfully for user: {UserId}", userId);

            return new RefreshTokenResponse
            {
                Success = true,
                Message = "Token refreshed successfully.",
                Data = new RefreshTokenData
                {
                    Token = newToken,
                    RefreshToken = newRefreshToken.Token
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during token refresh for user: {UserId}", userId);
            return new RefreshTokenResponse { Success = false, Message = "An error occurred during token refresh." };
        }
    }
}