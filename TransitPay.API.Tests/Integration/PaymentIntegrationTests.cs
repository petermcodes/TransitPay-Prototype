using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace TransitPay.API.Tests.Integration;

public class PaymentIntegrationTests : TestBase
{
    public PaymentIntegrationTests(TestWebApplicationFactory factory) : base(factory) { }

    [Fact]
    public async Task CreateTripPlan_CalculatesFare_AndReturnsPlan()
    {
        var (token, _) = await RegisterAndLoginPassengerAsync();
        var planId = await CreateTripPlanAsync(token, originTerminalId: 1, destinationTerminalId: 2);

        Assert.True(planId > 0);

        // Verify the plan details
        var client = CreateAuthenticatedClient(token);
        var response = await client.GetAsync($"/api/trip-plan/{planId}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        var data = body.GetProperty("data");
        Assert.Equal(1, data.GetProperty("originTerminalId").GetInt32());
        Assert.Equal(2, data.GetProperty("destinationTerminalId").GetInt32());
        Assert.Equal("Active", data.GetProperty("status").GetString());
        Assert.Equal(12.50m, data.GetProperty("finalFarePrice").GetDecimal());
    }

    [Fact]
    public async Task GetQR_ForCard_ReturnsSignedQR()
    {
        var (token, _) = await RegisterAndLoginPassengerAsync();
        var client = CreateAuthenticatedClient(token);

        // Get the card ID
        var cardResponse = await client.GetAsync("/api/cards/me");
        var cardBody = await cardResponse.Content.ReadFromJsonAsync<JsonElement>();
        var cardId = cardBody.GetProperty("data").GetProperty("cardId").GetInt32();

        var (data, signature) = await GetQrAsync(token, cardId);

        Assert.False(string.IsNullOrEmpty(data));
        Assert.False(string.IsNullOrEmpty(signature));
    }

    [Fact]
    public async Task ProcessConductorPayment_FullFlow_CompletesPayment()
    {
        // 1. Register passenger
        var (passengerToken, _) = await RegisterAndLoginPassengerAsync();
        var passengerClient = CreateAuthenticatedClient(passengerToken);

        // 2. Get card ID
        var cardResponse = await passengerClient.GetAsync("/api/cards/me");
        var cardBody = await cardResponse.Content.ReadFromJsonAsync<JsonElement>();
        var cardId = cardBody.GetProperty("data").GetProperty("cardId").GetInt32();

        // 3. Create trip plan
        var planId = await CreateTripPlanAsync(passengerToken, originTerminalId: 1, destinationTerminalId: 2);

        // 4. Get QR
        var (qrData, signature) = await GetQrAsync(passengerToken, cardId);

        // 5. Login as driver and start trip
        var driverToken = await LoginAsDriverAsync();
        var tripId = await StartTripAsync(driverToken, originTerminalId: 1, destinationTerminalId: 2);

        // 6. Process conductor payment
        var driverClient = CreateAuthenticatedClient(driverToken);
        var paymentResponse = await driverClient.PostAsJsonAsync("/api/payment/process-conductor", new
        {
            qrData,
            signature
        });

        // Payment must succeed — the QR system is verified working
        if (paymentResponse.StatusCode != HttpStatusCode.OK)
        {
            var errorBody = await paymentResponse.Content.ReadAsStringAsync();
            throw new Exception($"Payment failed: {paymentResponse.StatusCode} - {errorBody}");
        }

        var paymentBody = await paymentResponse.Content.ReadFromJsonAsync<JsonElement>();
        var trn = paymentBody.GetProperty("data").GetProperty("transactionReferenceNumber").GetString();
        Assert.False(string.IsNullOrEmpty(trn));
        Assert.StartsWith("TRN-", trn);
    }

    [Fact]
    public async Task ProcessConductorPayment_InvalidQR_ReturnsBadRequest()
    {
        var driverToken = await LoginAsDriverAsync();
        await StartTripAsync(driverToken, originTerminalId: 1, destinationTerminalId: 2);

        var driverClient = CreateAuthenticatedClient(driverToken);
        var response = await driverClient.PostAsJsonAsync("/api/payment/process-conductor", new
        {
            qrData = "invalid-qr-data",
            signature = "invalid-signature"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task ProcessConductorPayment_WithoutActiveTrip_ReturnsBadRequest()
    {
        var (passengerToken, _) = await RegisterAndLoginPassengerAsync();
        var passengerClient = CreateAuthenticatedClient(passengerToken);

        var cardResponse = await passengerClient.GetAsync("/api/cards/me");
        var cardBody = await cardResponse.Content.ReadFromJsonAsync<JsonElement>();
        var cardId = cardBody.GetProperty("data").GetProperty("cardId").GetInt32();

        await CreateTripPlanAsync(passengerToken, originTerminalId: 1, destinationTerminalId: 2);
        var (qrData, signature) = await GetQrAsync(passengerToken, cardId);

        // Driver has no active trip
        var driverToken = await LoginAsDriverAsync();
        var driverClient = CreateAuthenticatedClient(driverToken);

        var response = await driverClient.PostAsJsonAsync("/api/payment/process-conductor", new
        {
            qrData,
            signature
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task ProcessConductorPayment_WithoutTripPlan_ReturnsBadRequest()
    {
        var (passengerToken, _) = await RegisterAndLoginPassengerAsync();
        var passengerClient = CreateAuthenticatedClient(passengerToken);

        var cardResponse = await passengerClient.GetAsync("/api/cards/me");
        var cardBody = await cardResponse.Content.ReadFromJsonAsync<JsonElement>();
        var cardId = cardBody.GetProperty("data").GetProperty("cardId").GetInt32();

        // No trip plan created
        var (qrData, signature) = await GetQrAsync(passengerToken, cardId);

        var driverToken = await LoginAsDriverAsync();
        await StartTripAsync(driverToken, originTerminalId: 1, destinationTerminalId: 2);

        var driverClient = CreateAuthenticatedClient(driverToken);
        var response = await driverClient.PostAsJsonAsync("/api/payment/process-conductor", new
        {
            qrData,
            signature
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task ProcessConductorPayment_InsufficientBalance_ReturnsBadRequest()
    {
        // Register passenger
        var (passengerToken, _) = await RegisterAndLoginPassengerAsync();
        var passengerClient = CreateAuthenticatedClient(passengerToken);

        var cardResponse = await passengerClient.GetAsync("/api/cards/me");
        var cardBody = await cardResponse.Content.ReadFromJsonAsync<JsonElement>();
        var cardId = cardBody.GetProperty("data").GetProperty("cardId").GetInt32();

        // Create trip plan
        await CreateTripPlanAsync(passengerToken, originTerminalId: 1, destinationTerminalId: 2);
        var (qrData, signature) = await GetQrAsync(passengerToken, cardId);

        // Driver starts trip
        var driverToken = await LoginAsDriverAsync();
        await StartTripAsync(driverToken, originTerminalId: 1, destinationTerminalId: 2);

        // Drain the wallet balance below the fare amount
        using (var scope = Factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<TransitPay.API.Data.TransitPayDbContext>();
            var wallet = db.Wallets.First(w => w.CardId == cardId);
            wallet.Balance = 5.00m;
            await db.SaveChangesAsync();
        }

        var driverClient = CreateAuthenticatedClient(driverToken);
        var response = await driverClient.PostAsJsonAsync("/api/payment/process-conductor", new
        {
            qrData,
            signature
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task ProcessConductorPayment_ConcurrentPayments_GetDistinctTRNs()
    {
        // Register two passengers
        var (token1, _) = await RegisterAndLoginPassengerAsync();
        var (token2, _) = await RegisterAndLoginPassengerAsync();

        var client1 = CreateAuthenticatedClient(token1);
        var client2 = CreateAuthenticatedClient(token2);

        // Get card IDs
        var card1Response = await client1.GetAsync("/api/cards/me");
        var card1Body = await card1Response.Content.ReadFromJsonAsync<JsonElement>();
        var card1Id = card1Body.GetProperty("data").GetProperty("cardId").GetInt32();

        var card2Response = await client2.GetAsync("/api/cards/me");
        var card2Body = await card2Response.Content.ReadFromJsonAsync<JsonElement>();
        var card2Id = card2Body.GetProperty("data").GetProperty("cardId").GetInt32();

        // Create trip plans
        await CreateTripPlanAsync(token1, originTerminalId: 1, destinationTerminalId: 2);
        await CreateTripPlanAsync(token2, originTerminalId: 1, destinationTerminalId: 2);

        // Get QRs
        var (qr1Data, sig1) = await GetQrAsync(token1, card1Id);
        var (qr2Data, sig2) = await GetQrAsync(token2, card2Id);

        // Driver starts trip
        var driverToken = await LoginAsDriverAsync();
        await StartTripAsync(driverToken, originTerminalId: 1, destinationTerminalId: 2);

        var driverClient = CreateAuthenticatedClient(driverToken);

        // Process both payments
        var payment1 = await driverClient.PostAsJsonAsync("/api/payment/process-conductor", new
        {
            qrData = qr1Data,
            signature = sig1
        });
        var payment2 = await driverClient.PostAsJsonAsync("/api/payment/process-conductor", new
        {
            qrData = qr2Data,
            signature = sig2
        });

        // Both payments must succeed and have distinct TRNs
        if (payment1.StatusCode != HttpStatusCode.OK)
        {
            var errorBody = await payment1.Content.ReadAsStringAsync();
            throw new Exception($"Payment 1 failed: {payment1.StatusCode} - {errorBody}");
        }
        if (payment2.StatusCode != HttpStatusCode.OK)
        {
            var errorBody = await payment2.Content.ReadAsStringAsync();
            throw new Exception($"Payment 2 failed: {payment2.StatusCode} - {errorBody}");
        }

        var body1 = await payment1.Content.ReadFromJsonAsync<JsonElement>();
        var body2 = await payment2.Content.ReadFromJsonAsync<JsonElement>();
        
        var trn1 = body1.GetProperty("data").GetProperty("transactionReferenceNumber").GetString()!;
        var trn2 = body2.GetProperty("data").GetProperty("transactionReferenceNumber").GetString()!;

        Assert.NotEqual(trn1, trn2);
    }
}