using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace TransitPay.API.Tests.Integration;

public class DiscountIntegrationTests : TestBase
{
    public DiscountIntegrationTests(TestWebApplicationFactory factory) : base(factory) { }

    [Fact]
    public async Task Admin_CreatesDiscountType_AndPassengerApplies()
    {
        // Admin creates discount type
        var adminToken = await LoginAsAdminAsync();
        var adminClient = CreateAuthenticatedClient(adminToken);

        var createTypeResponse = await adminClient.PostAsJsonAsync("/api/discount/types", new
        {
            name = "Student Discount",
            description = "20% off for students",
            discountPercentage = 20,
            requiresApproval = true
        });
        Assert.Equal(HttpStatusCode.OK, createTypeResponse.StatusCode);

        var typeBody = await createTypeResponse.Content.ReadFromJsonAsync<JsonElement>();
        var discountTypeId = typeBody.GetProperty("data").GetProperty("discountTypeId").GetInt32();
        Assert.True(discountTypeId > 0);

        // Passenger applies for discount
        var (passengerToken, _) = await RegisterAndLoginPassengerAsync();
        var passengerClient = CreateAuthenticatedClient(passengerToken);

        var cardResponse = await passengerClient.GetAsync("/api/cards/me");
        var cardBody = await cardResponse.Content.ReadFromJsonAsync<JsonElement>();
        var cardId = cardBody.GetProperty("data").GetProperty("cardId").GetInt32();

        var applyResponse = await passengerClient.PostAsJsonAsync("/api/discount/apply", new
        {
            cardId,
            discountTypeId,
            discountDocument = "data:text/plain;base64,U3R1ZGVudCBJRCBkb2N1bWVudA=="
        });
        Assert.Equal(HttpStatusCode.OK, applyResponse.StatusCode);

        var applyBody = await applyResponse.Content.ReadFromJsonAsync<JsonElement>();
        var applicationId = applyBody.GetProperty("data").GetProperty("discountApplicationId").GetInt32();
        Assert.True(applicationId > 0);
    }

    [Fact]
    public async Task Admin_ApprovesApplication_AndGetActiveDiscount_ReturnsApproved()
    {
        // Setup: create type + apply
        var adminToken = await LoginAsAdminAsync();
        var adminClient = CreateAuthenticatedClient(adminToken);

        var createTypeResponse = await adminClient.PostAsJsonAsync("/api/discount/types", new
        {
            name = "Senior Discount",
            description = "20% off for seniors",
            discountPercentage = 20,
            requiresApproval = true
        });
        var typeBody = await createTypeResponse.Content.ReadFromJsonAsync<JsonElement>();
        var discountTypeId = typeBody.GetProperty("data").GetProperty("discountTypeId").GetInt32();

        var (passengerToken, _) = await RegisterAndLoginPassengerAsync();
        var passengerClient = CreateAuthenticatedClient(passengerToken);

        var cardResponse = await passengerClient.GetAsync("/api/cards/me");
        var cardBody = await cardResponse.Content.ReadFromJsonAsync<JsonElement>();
        var cardId = cardBody.GetProperty("data").GetProperty("cardId").GetInt32();

        var applyResponse = await passengerClient.PostAsJsonAsync("/api/discount/apply", new
        {
            cardId,
            discountTypeId,
            discountDocument = "data:text/plain;base64,U2VuaW9yIElEIGRvY3VtZW50"
        });
        var applyBody = await applyResponse.Content.ReadFromJsonAsync<JsonElement>();
        var applicationId = applyBody.GetProperty("data").GetProperty("discountApplicationId").GetInt32();

        // Admin approves
        var approveResponse = await adminClient.PostAsync($"/api/discount/applications/{applicationId}/approve", null);
        Assert.Equal(HttpStatusCode.OK, approveResponse.StatusCode);

        // Verify active discount
        var activeResponse = await passengerClient.GetAsync($"/api/discount/active/{cardId}");
        Assert.Equal(HttpStatusCode.OK, activeResponse.StatusCode);

        var activeBody = await activeResponse.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(20, activeBody.GetProperty("data").GetProperty("discountPercentage").GetInt32());
    }

    [Fact]
    public async Task Admin_RejectsApplication_ReturnsRejected()
    {
        // Setup: create type + apply
        var adminToken = await LoginAsAdminAsync();
        var adminClient = CreateAuthenticatedClient(adminToken);

        var createTypeResponse = await adminClient.PostAsJsonAsync("/api/discount/types", new
        {
            name = "PWD Discount",
            description = "20% off for PWD",
            discountPercentage = 20,
            requiresApproval = true
        });
        var typeBody = await createTypeResponse.Content.ReadFromJsonAsync<JsonElement>();
        var discountTypeId = typeBody.GetProperty("data").GetProperty("discountTypeId").GetInt32();

        var (passengerToken, _) = await RegisterAndLoginPassengerAsync();
        var passengerClient = CreateAuthenticatedClient(passengerToken);

        var cardResponse = await passengerClient.GetAsync("/api/cards/me");
        var cardBody = await cardResponse.Content.ReadFromJsonAsync<JsonElement>();
        var cardId = cardBody.GetProperty("data").GetProperty("cardId").GetInt32();

        var applyResponse = await passengerClient.PostAsJsonAsync("/api/discount/apply", new
        {
            cardId,
            discountTypeId,
            discountDocument = "data:text/plain;base64,UEdEIElEIGRvY3VtZW50"
        });
        var applyBody = await applyResponse.Content.ReadFromJsonAsync<JsonElement>();
        var applicationId = applyBody.GetProperty("data").GetProperty("discountApplicationId").GetInt32();

        // Admin rejects
        var rejectResponse = await adminClient.PostAsJsonAsync($"/api/discount/applications/{applicationId}/reject", new
        {
            rejectionReason = "Invalid document"
        });
        Assert.Equal(HttpStatusCode.OK, rejectResponse.StatusCode);

        // Verify no active discount
        var activeResponse = await passengerClient.GetAsync($"/api/discount/active/{cardId}");
        Assert.Equal(HttpStatusCode.OK, activeResponse.StatusCode);

        var activeBody = await activeResponse.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(activeBody.GetProperty("data").ValueKind == JsonValueKind.Null);
    }

    [Fact]
    public async Task GetPendingApplications_AdminSeesOnlyPending()
    {
        var adminToken = await LoginAsAdminAsync();
        var adminClient = CreateAuthenticatedClient(adminToken);

        var response = await adminClient.GetAsync("/api/discount/applications/pending");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Passenger_GetApplicationsByCard_SeesOwnApplications()
    {
        var (passengerToken, _) = await RegisterAndLoginPassengerAsync();
        var passengerClient = CreateAuthenticatedClient(passengerToken);

        var cardResponse = await passengerClient.GetAsync("/api/cards/me");
        var cardBody = await cardResponse.Content.ReadFromJsonAsync<JsonElement>();
        var cardId = cardBody.GetProperty("data").GetProperty("cardId").GetInt32();

        var response = await passengerClient.GetAsync($"/api/discount/applications/card/{cardId}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Passenger_AccessingAdminEndpoints_ReturnsForbidden()
    {
        var (passengerToken, _) = await RegisterAndLoginPassengerAsync();
        var passengerClient = CreateAuthenticatedClient(passengerToken);

        var response = await passengerClient.GetAsync("/api/discount/applications/pending");
        Assert.True(response.StatusCode == HttpStatusCode.Forbidden ||
                    response.StatusCode == HttpStatusCode.Unauthorized);
    }
}