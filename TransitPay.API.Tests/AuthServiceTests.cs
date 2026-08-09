using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using TransitPay.API.Configuration;
using TransitPay.API.Data;
using TransitPay.API.Enums;
using TransitPay.API.Models;
using TransitPay.API.Services;
using Xunit;

namespace TransitPay.API.Tests;

public class AuthServiceTests
{
    private static TransitPayDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<TransitPayDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new TransitPayDbContext(options);
    }

    private static AuthService CreateService(TransitPayDbContext context)
    {
        // Set JWT_KEY environment variable for SecurityKeyProvider
        Environment.SetEnvironmentVariable("JWT_KEY", "test-secret-key-at-least-32-characters-long-for-testing");

        var passwordHasher = new PasswordHasher<User>();
        var configuration = new ConfigurationBuilder().Build();
        var tokenService = new TokenService(
            configuration,
            context,
            new SecurityKeyProvider(configuration, NullLogger<SecurityKeyProvider>.Instance),
            NullLogger<TokenService>.Instance);
        var qrService = new QRService(context, new SecurityKeyProvider(configuration, NullLogger<SecurityKeyProvider>.Instance), NullLogger<QRService>.Instance);
        var httpContextAccessor = new HttpContextAccessor();

        return new AuthService(
            context,
            passwordHasher,
            tokenService,
            qrService,
            new OptionsWrapper<AuthenticationSettings>(new AuthenticationSettings()),
            httpContextAccessor,
            NullLogger<AuthService>.Instance);
    }

    private static async Task<Role> SeedRoleAsync(TransitPayDbContext context, RoleName roleName)
    {
        var role = new Role { RoleName = roleName };
        context.Roles.Add(role);
        await context.SaveChangesAsync();
        return role;
    }

    // Valid PH mobile format: ^09\d{9}$
    private const string MobileNumber = "09171234567";
    // Password passes PasswordPolicy: no personal info substrings, has upper/lower/digit/symbol
    private const string Password = "Sx9!Qw2#Ty8$Lm4";

    #region Registration Tests

    [Fact]
    public async Task RegisterAsync_ValidInput_ReturnsSuccessWithPassengerRole()
    {
        using var context = CreateContext();
        var service = CreateService(context);
        var passengerRole = await SeedRoleAsync(context, RoleName.Passenger);

        var result = await service.RegisterAsync(
            "testuser",
            "Passenger",
            "One",
            MobileNumber,
            Password);

        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.True(result.Data.UserId > 0);
        Assert.Equal("Passenger", result.Data.Role);
        Assert.Equal(passengerRole.RoleId, (await context.Users.FindAsync(result.Data.UserId))?.RoleId);
    }

    [Fact]
    public async Task RegisterAsync_DuplicateUsername_ReturnsFailure()
    {
        using var context = CreateContext();
        var service = CreateService(context);
        await SeedRoleAsync(context, RoleName.Passenger);

        await service.RegisterAsync(
            "duplicate",
            "First",
            "User",
            MobileNumber,
            Password);

        var result = await service.RegisterAsync(
            "duplicate",
            "Second",
            "User",
            "09987654321",
            "AnotherP@ss!99X");

        Assert.False(result.Success);
        Assert.Contains("Username is already taken", result.Message);
    }

    [Fact]
    public async Task RegisterAsync_DuplicateMobileNumber_ReturnsFailure()
    {
        using var context = CreateContext();
        var service = CreateService(context);
        await SeedRoleAsync(context, RoleName.Passenger);

        await service.RegisterAsync(
            "user1",
            "First",
            "User",
            MobileNumber,
            Password);

        var result = await service.RegisterAsync(
            "user2",
            "Second",
            "User",
            MobileNumber,
            Password);

        Assert.False(result.Success);
        Assert.Contains("mobile number already exists", result.Message);
    }

    [Fact]
    public async Task RegisterAsync_MissingPassengerRole_ReturnsFailure()
    {
        using var context = CreateContext();
        var service = CreateService(context);

        var result = await service.RegisterAsync(
            "testuser",
            "Passenger",
            "One",
            MobileNumber,
            Password);

        Assert.False(result.Success);
        Assert.Contains("error", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    #endregion

    #region Login Tests

    [Fact]
    public async Task LoginAsync_ValidCredentials_ReturnsSuccessWithTokens()
    {
        using var context = CreateContext();
        var service = CreateService(context);
        await SeedRoleAsync(context, RoleName.Passenger);

        await service.RegisterAsync(
            "loginuser",
            "Passenger",
            "One",
            MobileNumber,
            Password);

        var result = await service.LoginAsync("loginuser", Password);

        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.Equal("loginuser", result.Data.User.Username);
        Assert.NotNull(result.Data.Token);
        Assert.NotNull(result.Data.RefreshToken);
        Assert.Equal(RoleName.Passenger, result.Data.User.RoleName);
    }

    [Fact]
    public async Task LoginAsync_InvalidPassword_ReturnsFailure()
    {
        using var context = CreateContext();
        var service = CreateService(context);
        await SeedRoleAsync(context, RoleName.Passenger);

        await service.RegisterAsync(
            "testuser",
            "Passenger",
            "One",
            MobileNumber,
            Password);

        var result = await service.LoginAsync("testuser", "WrongPass!999X");

        Assert.False(result.Success);
        Assert.Contains("invalid", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task LoginAsync_NonExistentUser_ReturnsFailure()
    {
        using var context = CreateContext();
        var service = CreateService(context);

        var result = await service.LoginAsync("nonexistent", Password);

        Assert.False(result.Success);
        Assert.Contains("invalid", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task LoginAsync_MultipleFailedAttempts_ReturnsFailure()
    {
        using var context = CreateContext();
        var service = CreateService(context);
        await SeedRoleAsync(context, RoleName.Passenger);

        await service.RegisterAsync(
            "lockoutuser",
            "Passenger",
            "One",
            MobileNumber,
            Password);

        // Repeated failed login attempts should consistently fail
        for (int i = 0; i < 10; i++)
        {
            var failed = await service.LoginAsync("lockoutuser", "WrongPass!999X");
            Assert.False(failed.Success);
        }

        // Even with correct password, account protection should continue rejecting
        var result = await service.LoginAsync("lockoutuser", Password);

        Assert.False(result.Success);
    }

    #endregion

    #region Token Refresh Tests

    [Fact]
    public async Task RefreshTokenAsync_ValidToken_ReturnsNewRefreshToken()
    {
        using var context = CreateContext();
        var service = CreateService(context);
        await SeedRoleAsync(context, RoleName.Passenger);

        await service.RegisterAsync(
            "refreshuser",
            "Passenger",
            "One",
            MobileNumber,
            Password);

        var loginResult = await service.LoginAsync("refreshuser", Password);

        Assert.NotNull(loginResult.Data?.RefreshToken);
        Assert.NotNull(loginResult.Data?.Token);

        var refreshResult = await service.RefreshTokenAsync(
            loginResult.Data!.User.UserId,
            loginResult.Data!.RefreshToken!);

        Assert.True(refreshResult.Success);
        Assert.NotNull(refreshResult.Data);
        Assert.NotNull(refreshResult.Data.Token);
        Assert.NotNull(refreshResult.Data.RefreshToken);
    }

    [Fact]
    public async Task RefreshTokenAsync_InvalidToken_ReturnsFailure()
    {
        using var context = CreateContext();
        var service = CreateService(context);

        var result = await service.RefreshTokenAsync(999, "invalid-token");

        Assert.False(result.Success);
    }

    [Fact]
    public async Task RefreshTokenAsync_ExpiredToken_ReturnsFailure()
    {
        using var context = CreateContext();
        var service = CreateService(context);
        await SeedRoleAsync(context, RoleName.Passenger);

        await service.RegisterAsync(
            "expireduser",
            "Passenger",
            "One",
            MobileNumber,
            Password);

        var loginResult = await service.LoginAsync("expireduser", Password);

        var refreshToken = await context.RefreshTokens.FirstOrDefaultAsync(rt => rt.Token == loginResult.Data!.RefreshToken);
        if (refreshToken != null)
        {
            refreshToken.ExpiresAt = DateTime.UtcNow.AddDays(-1);
            await context.SaveChangesAsync();
        }

        var result = await service.RefreshTokenAsync(
            loginResult.Data!.User.UserId,
            loginResult.Data!.RefreshToken!);

        Assert.False(result.Success);
    }

    #endregion
}