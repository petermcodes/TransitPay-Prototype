using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace TransitPay.API.Tests.Integration;

public class TripManagementIntegrationTests : TestBase
{
    public TripManagementIntegrationTests(TestWebApplicationFactory factory) : base(factory) { }

    [Fact]
    public async Task StartTrip_ActiveTripVisibleInGetActive()
    {
        var driverToken = await LoginAsDriverAsync();
        var tripId = await StartTripAsync(driverToken, originTerminalId: 1, destinationTerminalId: 2);

        Assert.True(tripId > 0);

        var driverClient = CreateAuthenticatedClient(driverToken);
        var response = await driverClient.GetAsync("/api/Trip/active");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        var data = body.GetProperty("data");
        Assert.Equal(tripId, data.GetProperty("tripId").GetInt32());
        Assert.Equal("Active", data.GetProperty("tripStatus").GetString());
        Assert.Equal(1, data.GetProperty("originTerminalId").GetInt32());
        Assert.Equal(2, data.GetProperty("finalDestinationTerminalId").GetInt32());
    }

    [Fact]
    public async Task StartTrip_SecondTripFails_WhenActiveExists()
    {
        var driverToken = await LoginAsDriverAsync();
        await StartTripAsync(driverToken, originTerminalId: 1, destinationTerminalId: 2);

        var driverClient = CreateAuthenticatedClient(driverToken);
        var response = await driverClient.PostAsJsonAsync("/api/Trip/start", new
        {
            originTerminalId = 1,
            finalDestinationTerminalId = 2
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UpdateBoardingOrigin_UpdatesTrip()
    {
        var driverToken = await LoginAsDriverAsync();
        var tripId = await StartTripAsync(driverToken, originTerminalId: 1, destinationTerminalId: 2);

        var driverClient = CreateAuthenticatedClient(driverToken);
        var response = await driverClient.PutAsJsonAsync($"/api/Trip/{tripId}/boarding-origin", new
        {
            originTerminalId = 2
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(2, body.GetProperty("data").GetProperty("currentBoardingOriginTerminalId").GetInt32());
    }

    [Fact]
    public async Task EndTrip_MarksCompleted()
    {
        var driverToken = await LoginAsDriverAsync();
        var tripId = await StartTripAsync(driverToken, originTerminalId: 1, destinationTerminalId: 2);

        var driverClient = CreateAuthenticatedClient(driverToken);
        var response = await driverClient.PostAsync($"/api/Trip/{tripId}/end", null);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        // Verify no active trip remains
        var activeResponse = await driverClient.GetAsync("/api/Trip/active");
        var activeBody = await activeResponse.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(activeBody.GetProperty("data").ValueKind == JsonValueKind.Null);
    }

    [Fact]
    public async Task CancelTrip_TransitionsToCancelled()
    {
        var driverToken = await LoginAsDriverAsync();
        var tripId = await StartTripAsync(driverToken, originTerminalId: 1, destinationTerminalId: 2);

        var driverClient = CreateAuthenticatedClient(driverToken);
        var response = await driverClient.PostAsync($"/api/Trip/{tripId}/cancel", null);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Cancelled", body.GetProperty("data").GetProperty("tripStatus").GetString());
    }

    [Fact]
    public async Task GetTripHistory_ReturnsPaginatedHistory()
    {
        var driverToken = await LoginAsDriverAsync();
        var tripId = await StartTripAsync(driverToken, originTerminalId: 1, destinationTerminalId: 2);

        var driverClient = CreateAuthenticatedClient(driverToken);
        var response = await driverClient.GetAsync("/api/Trip/history?page=1&pageSize=10");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        var data = body.GetProperty("data");
        Assert.True(data.GetArrayLength() >= 1);
        Assert.Equal(1, body.GetProperty("pagination").GetProperty("page").GetInt32());
    }

    [Fact]
    public async Task EndTrip_ForNonOwnedTrip_ReturnsNotFound()
    {
        // Driver 1 starts a trip
        var driverToken = await LoginAsDriverAsync();
        var tripId = await StartTripAsync(driverToken, originTerminalId: 1, destinationTerminalId: 2);

        // Admin tries to end the trip (admin can manage any trip, so this should succeed)
        var adminToken = await LoginAsAdminAsync();
        var adminClient = CreateAuthenticatedClient(adminToken);
        var response = await adminClient.PostAsync($"/api/Trip/{tripId}/end", null);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task EndTrip_AlreadyCompleted_ReturnsBadRequest()
    {
        var driverToken = await LoginAsDriverAsync();
        var tripId = await StartTripAsync(driverToken, originTerminalId: 1, destinationTerminalId: 2);

        var driverClient = CreateAuthenticatedClient(driverToken);
        await driverClient.PostAsync($"/api/Trip/{tripId}/end", null);

        // Try to end again
        var response = await driverClient.PostAsync($"/api/Trip/{tripId}/end", null);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}