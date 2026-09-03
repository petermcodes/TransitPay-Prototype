using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace TransitPay.API.Tests.Integration;

/// <summary>
/// Integration tests for passenger wallet operations (balance lookup and top-ups)
/// through the full HTTP pipeline.
/// </summary>
public class PassengerWalletIntegrationTests : TestBase
{
    public PassengerWalletIntegrationTests(TestWebApplicationFactory factory) : base(factory) { }

    [Fact]
    public async Task Register_AutomaticallyCreatesCard()
    {
        var (token, userId) = await RegisterAndLoginPassengerAsync();

        var client = CreateAuthenticatedClient(token);
        var response = await client.GetAsync("/api/cards/me");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        var cardId = body.GetProperty("data").GetProperty("cardId").GetInt32();
        Assert.True(cardId > 0);
    }

    [Fact]
    public async Task GetWallet_ReturnsBalance_ForOwnedCard()
    {
        var (token, _) = await RegisterAndLoginPassengerAsync();
        var client = CreateAuthenticatedClient(token);

        // Get the card ID
        var cardResponse = await client.GetAsync("/api/cards/me");
        var cardBody = await cardResponse.Content.ReadFromJsonAsync<JsonElement>();
        var cardId = cardBody.GetProperty("data").GetProperty("cardId").GetInt32();

        // Get the wallet
        var walletResponse = await client.GetAsync($"/api/wallet/{cardId}");
        Assert.Equal(HttpStatusCode.OK, walletResponse.StatusCode);

        var walletBody = await walletResponse.Content.ReadFromJsonAsync<JsonElement>();
        var balance = walletBody.GetProperty("data").GetProperty("balance").GetDecimal();
        Assert.Equal(50.00m, balance);
    }

    [Fact]
    public async Task GetWallet_ForOtherUsersCard_ReturnsNotFound()
    {
        var (token1, _) = await RegisterAndLoginPassengerAsync();
        var (token2, _) = await RegisterAndLoginPassengerAsync();

        var client1 = CreateAuthenticatedClient(token1);
        var client2 = CreateAuthenticatedClient(token2);

        // Get card IDs for both users
        var card1Response = await client1.GetAsync("/api/cards/me");
        var card1Body = await card1Response.Content.ReadFromJsonAsync<JsonElement>();
        var card1Id = card1Body.GetProperty("data").GetProperty("cardId").GetInt32();

        // User 2 tries to access user 1's wallet
        var response = await client2.GetAsync($"/api/wallet/{card1Id}");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task AdminTopUp_IncreasesBalance()
    {
        var (token, _) = await RegisterAndLoginPassengerAsync();
        var passengerClient = CreateAuthenticatedClient(token);

        // Get the card ID
        var cardResponse = await passengerClient.GetAsync("/api/cards/me");
        var cardBody = await cardResponse.Content.ReadFromJsonAsync<JsonElement>();
        var cardId = cardBody.GetProperty("data").GetProperty("cardId").GetInt32();

        // Admin logs in and tops up
        var adminToken = await LoginAsAdminAsync();
        var adminClient = CreateAuthenticatedClient(adminToken);

        var topUpResponse = await adminClient.PostAsJsonAsync("/api/wallet/topup", new
        {
            cardId,
            amount = 100.00m
        });
        Assert.Equal(HttpStatusCode.OK, topUpResponse.StatusCode);

        // Verify balance increased
        var walletResponse = await passengerClient.GetAsync($"/api/wallet/{cardId}");
        var walletBody = await walletResponse.Content.ReadFromJsonAsync<JsonElement>();
        var balance = walletBody.GetProperty("data").GetProperty("balance").GetDecimal();
        Assert.Equal(150.00m, balance);
    }

    [Fact]
    public async Task TopUp_ByPassenger_ReturnsForbidden()
    {
        var (token, _) = await RegisterAndLoginPassengerAsync();
        var client = CreateAuthenticatedClient(token);

        var response = await client.PostAsJsonAsync("/api/wallet/topup", new
        {
            cardId = 1,
            amount = 100.00m
        });
        Assert.True(response.StatusCode == HttpStatusCode.Forbidden ||
                    response.StatusCode == HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task TopUp_InvalidAmount_ReturnsBadRequest()
    {
        var adminToken = await LoginAsAdminAsync();
        var adminClient = CreateAuthenticatedClient(adminToken);

        var response = await adminClient.PostAsJsonAsync("/api/wallet/topup", new
        {
            cardId = 1,
            amount = 0
        });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}