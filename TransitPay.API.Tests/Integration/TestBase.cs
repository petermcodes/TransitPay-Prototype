using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace TransitPay.API.Tests.Integration;

/// <summary>
/// Base class for integration tests that exercise the full HTTP pipeline
/// (controllers → services → database) via WebApplicationFactory.
/// </summary>
public abstract class TestBase : IClassFixture<TestWebApplicationFactory>
{
    protected readonly HttpClient Client;
    protected readonly TestWebApplicationFactory Factory;

    protected TestBase(TestWebApplicationFactory factory)
    {
        Factory = factory;
        Client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        // Reset the database for each test class to ensure isolation
        factory.ResetDatabase();
    }

    /// <summary>
    /// Registers a new passenger and logs in, returning the JWT + userId.
    /// </summary>
    protected async Task<(string Token, int UserId)> RegisterAndLoginPassengerAsync(string? mobile = null)
    {
        var uniqueMobile = mobile ?? $"0917{Random.Shared.Next(1000000, 9999999)}";
        var username = $"user_{Guid.NewGuid():N}"[..12];
        var password = "Sx9!Qw2#Ty8$Lm4";

        var regResponse = await Client.PostAsJsonAsync("/api/auth/register", new
        {
            username,
            firstName = "Integration",
            lastName = "Tester",
            mobileNumber = uniqueMobile,
            password
        });
        regResponse.EnsureSuccessStatusCode();

        var loginResponse = await Client.PostAsJsonAsync("/api/auth/login", new
        {
            username,
            password
        });
        loginResponse.EnsureSuccessStatusCode();

        var login = await loginResponse.Content.ReadFromJsonAsync<JsonElement>();
        var token = login.GetProperty("data").GetProperty("token").GetString()!;
        var userId = login.GetProperty("data").GetProperty("user").GetProperty("userId").GetInt32();

        return (token, userId);
    }

    /// <summary>
    /// Logs in as the seeded admin user.
    /// </summary>
    protected async Task<string> LoginAsAdminAsync()
    {
        var loginResponse = await Client.PostAsJsonAsync("/api/auth/login", new
        {
            username = "Admin",
            password = TestWebApplicationFactory.AdminBootstrapPassword
        });
        loginResponse.EnsureSuccessStatusCode();

        var login = await loginResponse.Content.ReadFromJsonAsync<JsonElement>();
        return login.GetProperty("data").GetProperty("token").GetString()!;
    }

    /// <summary>
    /// Logs in as the seeded driver user.
    /// </summary>
    protected async Task<string> LoginAsDriverAsync()
    {
        var loginResponse = await Client.PostAsJsonAsync("/api/auth/login", new
        {
            username = "DriverTest",
            password = TestWebApplicationFactory.DriverPassword
        });
        loginResponse.EnsureSuccessStatusCode();

        var login = await loginResponse.Content.ReadFromJsonAsync<JsonElement>();
        return login.GetProperty("data").GetProperty("token").GetString()!;
    }

    /// <summary>
    /// Creates an authenticated HttpClient with the given JWT.
    /// </summary>
    protected HttpClient CreateAuthenticatedClient(string token)
    {
        var client = Factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    /// <summary>
    /// Creates a trip plan for the authenticated user and returns the plan ID.
    /// The card is derived from the JWT claims server-side.
    /// </summary>
    protected async Task<int> CreateTripPlanAsync(string token, int originTerminalId, int destinationTerminalId)
    {
        var client = CreateAuthenticatedClient(token);
        var response = await client.PostAsJsonAsync("/api/trip-plan", new
        {
            originTerminalId,
            destinationTerminalId
        });
        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        return body.GetProperty("data").GetProperty("planId").GetInt32();
    }

    /// <summary>
    /// Starts a trip for the authenticated driver and returns the trip ID.
    /// The driver ID is derived from the JWT claims server-side.
    /// </summary>
    protected async Task<int> StartTripAsync(string token, int originTerminalId = 1, int destinationTerminalId = 2)
    {
        var client = CreateAuthenticatedClient(token);
        var response = await client.PostAsJsonAsync("/api/Trip/start", new
        {
            originTerminalId,
            finalDestinationTerminalId = destinationTerminalId
        });
        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        return body.GetProperty("data").GetProperty("tripId").GetInt32();
    }

    /// <summary>
    /// Gets the QR data + signature for a card.
    /// </summary>
    protected async Task<(string Data, string Signature)> GetQrAsync(string token, int cardId)
    {
        var client = CreateAuthenticatedClient(token);
        var response = await client.GetAsync($"/api/payment/qr/{cardId}");
        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        var data = body.GetProperty("data").GetProperty("data").GetString()!;
        var signature = body.GetProperty("data").GetProperty("signature").GetString()!;
        return (data, signature);
    }
}