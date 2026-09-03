using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TransitPay.API.Data;
using TransitPay.API.Models;
using Xunit;

namespace TransitPay.API.Tests.Integration.PostgreSQL;

/// <summary>
/// Integration tests that require a real PostgreSQL database.
/// These tests verify database transactions, constraints, and real concurrency behavior.
/// </summary>
[Collection("PostgreSQL collection")]
/// <summary>
/// Integration tests that run against a real PostgreSQL instance (via Testcontainers)
/// to exercise transactions, real constraints, and concurrency behaviour.
/// </summary>
public class PostgreSQLIntegrationTests : IClassFixture<PostgreSQLTestWebApplicationFactory>
{
    private readonly PostgreSQLTestWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public PostgreSQLIntegrationTests(PostgreSQLTestWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Database_Connection_IsSuccessful()
    {
        // Verify the database is accessible
        var response = await _client.GetAsync("/api/auth/validate");
        Assert.True(response.StatusCode == HttpStatusCode.OK || 
                    response.StatusCode == HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Payment_Transaction_CommitsToDatabase()
    {
        // Reset database to clean state
        _factory.ResetDatabase();

        // Register a new passenger
        var mobile = $"0917{Random.Shared.Next(1000000, 9999999)}";
        var registerResponse = await _client.PostAsJsonAsync("/api/auth/register", new
        {
            username = $"pguser_{Guid.NewGuid():N}",
            password = "Str0ng!Passw0rd",
            firstName = "PostgreSQL",
            lastName = "Tester",
            mobileNumber = mobile
        });
        Assert.Equal(HttpStatusCode.OK, registerResponse.StatusCode);

        var registerBody = await registerResponse.Content.ReadFromJsonAsync<JsonElement>();
        var userId = registerBody.GetProperty("data").GetProperty("userId").GetInt32();

        // Get the card
        var (token, _) = await LoginAsync(mobile, "Str0ng!Passw0rd");
        var authClient = CreateAuthenticatedClient(token);
        
        var cardResponse = await authClient.GetAsync("/api/cards/me");
        Assert.Equal(HttpStatusCode.OK, cardResponse.StatusCode);
        var cardBody = await cardResponse.Content.ReadFromJsonAsync<JsonElement>();
        var cardId = cardBody.GetProperty("data").GetProperty("cardId").GetInt32();

        // Get initial balance
        var walletResponse = await authClient.GetAsync($"/api/wallet/{cardId}");
        var walletBody = await walletResponse.Content.ReadFromJsonAsync<JsonElement>();
        var initialBalance = walletBody.GetProperty("data").GetProperty("balance").GetDecimal();

        // Create trip plan
        var planResponse = await authClient.PostAsJsonAsync("/api/trip-plan", new
        {
            originTerminalId = 1,
            destinationTerminalId = 2
        });
        Assert.Equal(HttpStatusCode.OK, planResponse.StatusCode);
        var planBody = await planResponse.Content.ReadFromJsonAsync<JsonElement>();
        var planId = planBody.GetProperty("data").GetProperty("planId").GetInt32();

        // Get QR
        var qrResult = await GetQrAsync(authClient, cardId);
        var qrData = qrResult.data;
        var signature = qrResult.signature;

        // Driver starts trip
        var driverToken = await LoginAsDriverAsync();
        await StartTripAsync(driverToken, 1, 2);

        // Process payment
        var driverClient = CreateAuthenticatedClient(driverToken);
        var paymentResponse = await driverClient.PostAsJsonAsync("/api/payment/process-conductor", new
        {
            qrData,
            signature
        });

        // Payment must succeed — the QR system is verified working with real PostgreSQL
        if (paymentResponse.StatusCode != HttpStatusCode.OK)
        {
            var errorBody = await paymentResponse.Content.ReadAsStringAsync();
            throw new Exception($"Payment failed: {paymentResponse.StatusCode} - {errorBody}");
        }

        var paymentBody = await paymentResponse.Content.ReadFromJsonAsync<JsonElement>();
        var trn = paymentBody.GetProperty("data").GetProperty("transactionReferenceNumber").GetString();
        
        // Verify TRN was generated and persisted
        Assert.False(string.IsNullOrEmpty(trn));
        Assert.StartsWith("TRN-", trn);

        // Verify transaction exists in database
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TransitPayDbContext>();
        var transaction = (await db.Transactions
            .Where(t => t.TransactionReferenceNumber == trn)
            .ToListAsync())
            .FirstOrDefault();
        Assert.NotNull(transaction);
        Assert.Equal(cardId, transaction.CardId);
    }

    [Fact]
    public async Task ConcurrentPayments_GenerateDistinctTRNs()
    {
        _factory.ResetDatabase();

        // Register two passengers
        var (token1, _) = await RegisterPassengerAsync();
        var (token2, _) = await RegisterPassengerAsync();

        var client1 = CreateAuthenticatedClient(token1);
        var client2 = CreateAuthenticatedClient(token2);

        // Get card IDs
        var card1Id = await GetCardIdAsync(client1);
        var card2Id = await GetCardIdAsync(client2);

        // Create trip plans
        await CreateTripPlanAsync(client1, 1, 2);
        await CreateTripPlanAsync(client2, 1, 2);

        // Get QRs
        var qr1Result = await GetQrAsync(client1, card1Id);
        var qr1 = qr1Result.data;
        var sig1 = qr1Result.signature;
        
        var qr2Result = await GetQrAsync(client2, card2Id);
        var qr2 = qr2Result.data;
        var sig2 = qr2Result.signature;

        // Driver starts trip
        var driverToken = await LoginAsDriverAsync();
        await StartTripAsync(driverToken, 1, 2);
        var driverClient = CreateAuthenticatedClient(driverToken);

        // Process payments concurrently
        var payment1Task = driverClient.PostAsJsonAsync("/api/payment/process-conductor", new { qrData = qr1, signature = sig1 });
        var payment2Task = driverClient.PostAsJsonAsync("/api/payment/process-conductor", new { qrData = qr2, signature = sig2 });

        await Task.WhenAll(payment1Task, payment2Task);

        var payment1 = await payment1Task;
        var payment2 = await payment2Task;

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

        // Verify TRNs are distinct
        Assert.NotEqual(trn1, trn2);
    }

    [Fact]
    public async Task Database_Constraints_PreventInvalidData()
    {
        _factory.ResetDatabase();

        // Try to create a fare rule with non-existent terminal (should fail with 404 or validation error)
        var adminToken = await LoginAsAdminAsync();
        var adminClient = CreateAuthenticatedClient(adminToken);

        var response = await adminClient.PostAsJsonAsync("/api/admin/fare-rules", new
        {
            originTerminalId = 999,
            destinationTerminalId = 998,
            fareAmount = 10.00m,
            effectiveDate = DateTime.UtcNow
        });

        // Should fail because terminals don't exist
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Transaction_Rollback_OnPaymentFailure()
    {
        _factory.ResetDatabase();

        // Register passenger with low balance
        var (token, _) = await RegisterPassengerAsync();
        var client = CreateAuthenticatedClient(token);
        
        var cardId = await GetCardIdAsync(client);

        // Drain wallet balance
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<TransitPayDbContext>();
            var wallet = db.Wallets.First(w => w.CardId == cardId);
            wallet.Balance = 1.00m;
            await db.SaveChangesAsync();
        }

        // Create trip plan
        await CreateTripPlanAsync(client, 1, 2);
        var qrResult = await GetQrAsync(client, cardId);
        var qrData = qrResult.data;
        var signature = qrResult.signature;

        // Driver starts trip
        var driverToken = await LoginAsDriverAsync();
        await StartTripAsync(driverToken, 1, 2);
        var driverClient = CreateAuthenticatedClient(driverToken);

        // Try to process payment (should fail due to insufficient balance)
        var paymentResponse = await driverClient.PostAsJsonAsync("/api/payment/process-conductor", new
        {
            qrData,
            signature
        });

        Assert.Equal(HttpStatusCode.BadRequest, paymentResponse.StatusCode);

        // Verify wallet balance was NOT changed (transaction rolled back)
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<TransitPayDbContext>();
            var wallet = (await db.Wallets
                .Where(w => w.CardId == cardId)
                .ToListAsync())
                .First();
            Assert.Equal(1.00m, wallet.Balance);
        }
    }

    #region Helper Methods

    private HttpClient CreateAuthenticatedClient(string token)
    {
        return _factory.CreateAuthenticatedClient(token);
    }

    private async Task<(string token, string refreshToken)> LoginAsync(string mobile, string password)
    {
        var response = await _client.PostAsJsonAsync("/api/auth/login", new
        {
            username = mobile,
            password
        });
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        var token = body.GetProperty("data").GetProperty("token").GetString()!;
        var refreshToken = body.GetProperty("data").GetProperty("refreshToken").GetString()!;
        return (token, refreshToken);
    }

    private async Task<(string token, string refreshToken)> RegisterPassengerAsync()
    {
        var mobile = $"0917{Random.Shared.Next(1000000, 9999999)}";
        var response = await _client.PostAsJsonAsync("/api/auth/register", new
        {
            username = $"pguser_{Guid.NewGuid():N}",
            password = "Str0ng!Passw0rd",
            firstName = "Test",
            lastName = "User",
            mobileNumber = mobile
        });
        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync();
            throw new Exception($"Registration failed: {response.StatusCode} - {errorBody}");
        }
        return await LoginAsync(mobile, "Str0ng!Passw0rd");
    }

    private async Task<string> LoginAsDriverAsync()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/login", new
        {
            username = "DriverTest",
            password = PostgreSQLTestWebApplicationFactory.DriverPassword
        });
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        return body.GetProperty("data").GetProperty("token").GetString()!;
    }

    private async Task<string> LoginAsAdminAsync()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/login", new
        {
            username = "Admin",
            password = PostgreSQLTestWebApplicationFactory.AdminBootstrapPassword
        });
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        return body.GetProperty("data").GetProperty("token").GetString()!;
    }

    private async Task<int> GetCardIdAsync(HttpClient client)
    {
        var response = await client.GetAsync("/api/cards/me");
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        return body.GetProperty("data").GetProperty("cardId").GetInt32();
    }

    private async Task<(string data, string signature)> GetQrAsync(HttpClient client, int cardId)
    {
        var response = await client.GetAsync($"/api/payment/qr/{cardId}");
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        var data = body.GetProperty("data").GetProperty("data").GetString()!;
        var signature = body.GetProperty("data").GetProperty("signature").GetString()!;
        return (data, signature);
    }

    private async Task CreateTripPlanAsync(HttpClient client, int originTerminalId, int destinationTerminalId)
    {
        var response = await client.PostAsJsonAsync("/api/trip-plan", new
        {
            originTerminalId,
            destinationTerminalId
        });
        response.EnsureSuccessStatusCode();
    }

    private async Task StartTripAsync(string driverToken, int originTerminalId, int destinationTerminalId)
    {
        var driverClient = CreateAuthenticatedClient(driverToken);
        var response = await driverClient.PostAsJsonAsync("/api/Trip/start", new
        {
            originTerminalId,
            destinationTerminalId
        });
        response.EnsureSuccessStatusCode();
    }

    #endregion
}