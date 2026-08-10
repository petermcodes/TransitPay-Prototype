using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace TransitPay.API.Tests.Integration;

public class AdminIntegrationTests : TestBase
{
    public AdminIntegrationTests(TestWebApplicationFactory factory) : base(factory) { }

    [Fact]
    public async Task GetUsers_ReturnsPaginatedUsers()
    {
        var adminToken = await LoginAsAdminAsync();
        var adminClient = CreateAuthenticatedClient(adminToken);

        var response = await adminClient.GetAsync("/api/admin/users?page=1&pageSize=10");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        var data = body.GetProperty("data");
        // Verify the response contains a data array (may be empty in test environment)
        Assert.Equal(JsonValueKind.Array, data.ValueKind);
    }

    [Fact]
    public async Task GetDrivers_ReturnsPaginatedDrivers()
    {
        var adminToken = await LoginAsAdminAsync();
        var adminClient = CreateAuthenticatedClient(adminToken);

        var response = await adminClient.GetAsync("/api/admin/drivers?page=1&pageSize=10");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(body.GetProperty("data").GetArrayLength() >= 1);
    }

    [Fact]
    public async Task CreateTerminal_GetTerminals_ListsNewTerminal()
    {
        var adminToken = await LoginAsAdminAsync();
        var adminClient = CreateAuthenticatedClient(adminToken);

        var createResponse = await adminClient.PostAsJsonAsync("/api/admin/terminals", new
        {
            terminalName = "Harbor Terminal"
        });
        Assert.Equal(HttpStatusCode.OK, createResponse.StatusCode);

        var getResponse = await adminClient.GetAsync("/api/admin/terminals");
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

        var body = await getResponse.Content.ReadFromJsonAsync<JsonElement>();
        var terminals = body.GetProperty("data");
        Assert.True(terminals.GetArrayLength() >= 3); // 2 seeded + 1 new
    }

    [Fact]
    public async Task CreateFareRule_CalculateFare_UsesNewRule()
    {
        var adminToken = await LoginAsAdminAsync();
        var adminClient = CreateAuthenticatedClient(adminToken);

        // Create a new terminal first
        var terminalResponse = await adminClient.PostAsJsonAsync("/api/admin/terminals", new
        {
            terminalName = "Harbor Terminal"
        });
        Assert.Equal(HttpStatusCode.OK, terminalResponse.StatusCode);
        var terminalBody = await terminalResponse.Content.ReadFromJsonAsync<JsonElement>();
        var newTerminalId = terminalBody.GetProperty("data").GetProperty("terminalId").GetInt32();

        // Create a new fare rule for the new route (terminal 1 -> new terminal)
        var createResponse = await adminClient.PostAsJsonAsync("/api/admin/fare-rules", new
        {
            originTerminalId = 1,
            destinationTerminalId = newTerminalId,
            fareAmount = 25.00m,
            effectiveDate = DateTime.UtcNow
        });
        Assert.Equal(HttpStatusCode.OK, createResponse.StatusCode);

        // Verify fare calculation
        var fareResponse = await Client.GetAsync($"/api/fare/calculate?originTerminalId=1&destinationTerminalId={newTerminalId}&cardId=1");
        Assert.Equal(HttpStatusCode.OK, fareResponse.StatusCode);

        var fareBody = await fareResponse.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(25.00m, fareBody.GetProperty("data").GetProperty("finalFare").GetDecimal());
    }

    [Fact]
    public async Task GetTransactions_ReturnsTransactions()
    {
        var adminToken = await LoginAsAdminAsync();
        var adminClient = CreateAuthenticatedClient(adminToken);

        var response = await adminClient.GetAsync("/api/admin/transactions?page=1&pageSize=10");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetReportSummary_ReturnsMetrics()
    {
        var adminToken = await LoginAsAdminAsync();
        var adminClient = CreateAuthenticatedClient(adminToken);

        var response = await adminClient.GetAsync("/api/admin/reports/summary");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(body.GetProperty("success").GetBoolean());
    }

    [Fact]
    public async Task GetTrips_ReturnsPaginatedTrips()
    {
        var adminToken = await LoginAsAdminAsync();
        var adminClient = CreateAuthenticatedClient(adminToken);

        var response = await adminClient.GetAsync("/api/admin/trips?page=1&pageSize=10");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Passenger_AccessingAdminEndpoints_ReturnsForbidden()
    {
        var (passengerToken, _) = await RegisterAndLoginPassengerAsync();
        var passengerClient = CreateAuthenticatedClient(passengerToken);

        var response = await passengerClient.GetAsync("/api/admin/users");
        Assert.True(response.StatusCode == HttpStatusCode.Forbidden ||
                    response.StatusCode == HttpStatusCode.Unauthorized);
    }
}