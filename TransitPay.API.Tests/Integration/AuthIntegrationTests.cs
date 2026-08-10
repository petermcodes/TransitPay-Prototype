using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace TransitPay.API.Tests.Integration;

public class AuthIntegrationTests : TestBase
{
    public AuthIntegrationTests(TestWebApplicationFactory factory) : base(factory) { }

    [Fact]
    public async Task Register_Login_Refresh_Logout_FullLifecycle()
    {
        // Register
        var mobile = $"0917{Random.Shared.Next(1000000, 9999999)}";
        var username = $"lc_{Guid.NewGuid():N}"[..12];
        var password = "Sx9!Qw2#Ty8$Lm4";

        var regResponse = await Client.PostAsJsonAsync("/api/auth/register", new
        {
            username,
            firstName = "Lifecycle",
            lastName = "Tester",
            mobileNumber = mobile,
            password
        });
        Assert.Equal(HttpStatusCode.OK, regResponse.StatusCode);

        // Login
        var loginResponse = await Client.PostAsJsonAsync("/api/auth/login", new
        {
            username,
            password
        });
        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);
        var login = await loginResponse.Content.ReadFromJsonAsync<JsonElement>();
        var token = login.GetProperty("data").GetProperty("token").GetString()!;
        var refreshToken = login.GetProperty("data").GetProperty("refreshToken").GetString()!;
        var userId = login.GetProperty("data").GetProperty("user").GetProperty("userId").GetInt32();

        Assert.False(string.IsNullOrEmpty(token));
        Assert.False(string.IsNullOrEmpty(refreshToken));
        Assert.True(userId > 0);

        // Refresh
        var refreshResponse = await Client.PostAsJsonAsync("/api/auth/refresh", new
        {
            userId,
            refreshToken
        });
        Assert.Equal(HttpStatusCode.OK, refreshResponse.StatusCode);
        var refresh = await refreshResponse.Content.ReadFromJsonAsync<JsonElement>();
        var newToken = refresh.GetProperty("data").GetProperty("token").GetString()!;
        Assert.False(string.IsNullOrEmpty(newToken));

        // Logout with new token
        var logoutClient = CreateAuthenticatedClient(newToken);
        var logoutResponse = await logoutClient.PostAsync("/api/auth/logout", null);
        Assert.Equal(HttpStatusCode.OK, logoutResponse.StatusCode);
    }

    [Fact]
    public async Task Register_DuplicateMobile_ReturnsConflict()
    {
        var mobile = $"0917{Random.Shared.Next(1000000, 9999999)}";
        var password = "Sx9!Qw2#Ty8$Lm4";
        var username1 = $"dup1_{Guid.NewGuid():N}"[..12];
        var username2 = $"dup2_{Guid.NewGuid():N}"[..12];

        var first = await Client.PostAsJsonAsync("/api/auth/register", new
        {
            username = username1,
            firstName = "Dup",
            lastName = "User",
            mobileNumber = mobile,
            password
        });
        Assert.Equal(HttpStatusCode.OK, first.StatusCode);

        var second = await Client.PostAsJsonAsync("/api/auth/register", new
        {
            username = username2,
            firstName = "Dup",
            lastName = "User",
            mobileNumber = mobile,
            password
        });
        Assert.True(second.StatusCode == HttpStatusCode.BadRequest ||
                    second.StatusCode == HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Login_InvalidCredentials_ReturnsBadRequest()
    {
        var response = await Client.PostAsJsonAsync("/api/auth/login", new
        {
            username = "nonexistent_user",
            password = "WrongPassword123!"
        });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Login_AdminRole_ReturnsAdminJwt()
    {
        var adminToken = await LoginAsAdminAsync();
        Assert.False(string.IsNullOrEmpty(adminToken));
    }

    [Fact]
    public async Task ValidateToken_ValidToken_ReturnsSuccess()
    {
        var (token, _) = await RegisterAndLoginPassengerAsync();
        var client = CreateAuthenticatedClient(token);
        var response = await client.GetAsync("/api/auth/validate");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task UnauthenticatedRequest_ReturnsUnauthorized()
    {
        var response = await Client.GetAsync("/api/cards/me");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}