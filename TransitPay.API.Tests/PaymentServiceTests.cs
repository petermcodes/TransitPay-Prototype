using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using TransitPay.API.Data;
using TransitPay.API.Enums;
using TransitPay.API.Interfaces;
using TransitPay.API.Models;
using TransitPay.API.Services;
using Xunit;

namespace TransitPay.API.Tests;

public class PaymentServiceTests
{
    private static PaymentService CreateService(TransitPayDbContext context)
    {
        // Create a mock QR service that always returns the card ID
        var qrService = new MockQRService();
        var trnGenerator = new TransactionReferenceNumberGenerator(context);
        return new PaymentService(context, qrService, trnGenerator, new MockTripService(), new MockDiscountService(), NullLogger<PaymentService>.Instance);
    }

    private static PaymentSessionService CreateSessionService(TransitPayDbContext context)
    {
        return new PaymentSessionService(context, NullLogger<PaymentSessionService>.Instance);
    }

    private static TransitPayDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<TransitPayDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            // The in-memory provider doesn't support transactions, so suppress the warning
            .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        return new TransitPayDbContext(options);
    }

    private static void SeedStationsAndFare(TransitPayDbContext context, decimal fareAmount = 12.5m)
    {
        context.Stations.Add(new Station { StationId = 1, StationName = "Central", TownId = 1, IsActive = true });
        context.Stations.Add(new Station { StationId = 2, StationName = "Harbor", TownId = 1, IsActive = true });
        context.FareRules.Add(new FareRule
        {
            OriginStationId = 1,
            DestinationStationId = 2,
            VehicleType = VehicleType.BUS,
            PassengerType = PassengerType.Passenger,
            FareAmount = fareAmount,
            EffectiveDate = DateTime.UtcNow.AddDays(-1),
            IsActive = true
        });
    }

    private static async Task<(TransitPayDbContext context, Card card)> CreateCardAndSession(
        decimal balance = 50,
        decimal fareAmount = 12.5m,
        PaymentSessionStatus status = PaymentSessionStatus.PENDING,
        DateTime? expiresAt = null)
    {
        var context = CreateContext();
        var card = new Card
        {
            CardNumber = "4111111111111111",
            Status = CardStatus.ACTIVE,
            PassengerType = PassengerType.Passenger,
            IssueDate = DateTime.UtcNow,
            ExpiryDate = DateTime.UtcNow.AddYears(1)
        };
        context.Cards.Add(card);
        context.Wallets.Add(new Wallet { Card = card, Balance = balance, Status = CardStatus.ACTIVE });
        SeedStationsAndFare(context, fareAmount);
        await context.SaveChangesAsync();

        // Create a payment session for the card
        var session = new PaymentSession
        {
            PaymentSessionId = Guid.NewGuid(),
            CardId = card.CardId,
            UserId = 1,
            OriginStationId = 1,
            DestinationStationId = 2,
            Fare = fareAmount,
            Status = status,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = expiresAt ?? DateTime.UtcNow.AddMinutes(10)
        };
        context.PaymentSessions.Add(session);
        await context.SaveChangesAsync();

        return (context, card);
    }

    private static Task<PaymentService> CreateServiceWithSession(PaymentServiceTests t, TransitPayDbContext context)
        => Task.FromResult(CreateService(context));

    [Fact]
    public async Task CreateOrUpdateSession_CreatesNewSession_WithLockedFare()
    {
        var context = CreateContext();
        var card = new Card
        {
            CardNumber = "4111111111111111",
            Status = CardStatus.ACTIVE,
            PassengerType = PassengerType.Passenger,
            IssueDate = DateTime.UtcNow,
            ExpiryDate = DateTime.UtcNow.AddYears(1)
        };
        context.Cards.Add(card);
        context.Wallets.Add(new Wallet { Card = card, Balance = 50, Status = CardStatus.ACTIVE });
        SeedStationsAndFare(context, fareAmount: 12.5m);
        await context.SaveChangesAsync();

        var sessionService = CreateSessionService(context);
        var result = await sessionService.CreateOrUpdateSessionAsync(card.CardId, 1, 1, 2);

        Assert.NotNull(result);
        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.Equal(12.5m, result.Data!.LockedFare);
        Assert.Equal(1, result.Data.OriginStationId);
        Assert.Equal(2, result.Data.DestinationStationId);
        Assert.Equal(1, result.Data.UserId);
        Assert.Equal(PaymentSessionStatus.PENDING, result.Data.Status);
        Assert.True(result.Data.ExpiresAt > DateTime.UtcNow);

        // Only one session should exist
        var sessionCount = await context.PaymentSessions.CountAsync();
        Assert.Equal(1, sessionCount);
    }

    [Fact]
    public async Task CreateOrUpdateSession_UpdatesExistingSession_WhenRouteChanges()
    {
        var context = CreateContext();
        var card = new Card
        {
            CardNumber = "4111111111111111",
            Status = CardStatus.ACTIVE,
            PassengerType = PassengerType.Passenger,
            IssueDate = DateTime.UtcNow,
            ExpiryDate = DateTime.UtcNow.AddYears(1)
        };
        context.Cards.Add(card);
        context.Wallets.Add(new Wallet { Card = card, Balance = 50, Status = CardStatus.ACTIVE });

        // Add stations 1,2 and 1,3 fare
        context.Stations.Add(new Station { StationId = 1, StationName = "Central", TownId = 1, IsActive = true });
        context.Stations.Add(new Station { StationId = 2, StationName = "Harbor", TownId = 1, IsActive = true });
        context.Stations.Add(new Station { StationId = 3, StationName = "Airport", TownId = 1, IsActive = true });
        context.FareRules.Add(new FareRule
        {
            OriginStationId = 1,
            DestinationStationId = 2,
            VehicleType = VehicleType.BUS,
            PassengerType = PassengerType.Passenger,
            FareAmount = 12.5m,
            EffectiveDate = DateTime.UtcNow.AddDays(-1),
            IsActive = true
        });
        context.FareRules.Add(new FareRule
        {
            OriginStationId = 1,
            DestinationStationId = 3,
            VehicleType = VehicleType.BUS,
            PassengerType = PassengerType.Passenger,
            FareAmount = 20m,
            EffectiveDate = DateTime.UtcNow.AddDays(-1),
            IsActive = true
        });
        await context.SaveChangesAsync();

        var sessionService = CreateSessionService(context);
        var firstResult = await sessionService.CreateOrUpdateSessionAsync(card.CardId, 1, 1, 2);
        Assert.True(firstResult.Success);
        Assert.Equal(12.5m, firstResult.Data!.LockedFare);

        // Change route to 1→3
        var secondResult = await sessionService.CreateOrUpdateSessionAsync(card.CardId, 1, 1, 3);
        Assert.True(secondResult.Success);
        Assert.Equal(3, secondResult.Data!.DestinationStationId);
        Assert.Equal(20m, secondResult.Data.LockedFare);

        // Should still be only one session
        var sessionCount = await context.PaymentSessions.CountAsync();
        Assert.Equal(1, sessionCount);
    }

    [Fact]
    public async Task CreateOrUpdateSession_RejectsWhenSessionIsProcessing()
    {
        var context = CreateContext();
        var card = new Card
        {
            CardNumber = "4111111111111111",
            Status = CardStatus.ACTIVE,
            PassengerType = PassengerType.Passenger,
            IssueDate = DateTime.UtcNow,
            ExpiryDate = DateTime.UtcNow.AddYears(1)
        };
        context.Cards.Add(card);
        context.Wallets.Add(new Wallet { Card = card, Balance = 50, Status = CardStatus.ACTIVE });
        SeedStationsAndFare(context);
        await context.SaveChangesAsync();

        // Create a PROCESSING session directly
        context.PaymentSessions.Add(new PaymentSession
        {
            PaymentSessionId = Guid.NewGuid(),
            CardId = card.CardId,
            UserId = 1,
            OriginStationId = 1,
            DestinationStationId = 2,
            Fare = 12.5m,
            Status = PaymentSessionStatus.PROCESSING,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddMinutes(10)
        });
        await context.SaveChangesAsync();

        var sessionService = CreateSessionService(context);
        var result = await sessionService.CreateOrUpdateSessionAsync(card.CardId, 1, 1, 2);

        Assert.False(result.Success);
        Assert.Equal("Payment is currently being processed.", result.Message);
    }

    [Fact]
    public async Task ScanQR_RejectsWhenNoActiveSessionExists()
    {
        var context = CreateContext();
        var card = new Card
        {
            CardNumber = "4111111111111111",
            Status = CardStatus.ACTIVE,
            PassengerType = PassengerType.Passenger,
            IssueDate = DateTime.UtcNow,
            ExpiryDate = DateTime.UtcNow.AddYears(1)
        };
        context.Cards.Add(card);
        context.Wallets.Add(new Wallet { Card = card, Balance = 50, Status = CardStatus.ACTIVE });
        SeedStationsAndFare(context);
        await context.SaveChangesAsync();

        var service = CreateService(context);
        var result = await service.ProcessQRPaymentAsync("test-data", "test-signature", driverId: 5);

        Assert.False(result.Success);
        Assert.Contains("No active payment session", result.Message);
    }

    [Fact]
    public async Task ScanQR_RejectsExpiredSession()
    {
        var (context, _) = await CreateCardAndSession(expiresAt: DateTime.UtcNow.AddMinutes(-1));
        var service = CreateService(context);

        var result = await service.ProcessQRPaymentAsync("test-data", "test-signature", driverId: 5);

        Assert.False(result.Success);
        Assert.Contains("expired", result.Message, StringComparison.OrdinalIgnoreCase);

        // Session should be marked EXPIRED
        var session = await context.PaymentSessions.FirstAsync();
        Assert.Equal(PaymentSessionStatus.EXPIRED, session.Status);
    }

    [Fact]
    public async Task ScanQR_RejectsWhenSessionAlreadyCompleted()
    {
        var (context, _) = await CreateCardAndSession(status: PaymentSessionStatus.COMPLETED);
        var service = CreateService(context);

        var result = await service.ProcessQRPaymentAsync("test-data", "test-signature", driverId: 5);

        Assert.False(result.Success);
        Assert.Equal("Payment has already been completed.", result.Message);
    }

    [Fact]
    public async Task ScanQR_ChargesLockedFare_AndCreatesTransactionRecord()
    {
        var (context, card) = await CreateCardAndSession();
        var service = CreateService(context);

        var result = await service.ProcessQRPaymentAsync("test-data", "test-signature", driverId: 5);

        Assert.NotNull(result);
        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.Equal(12.5m, result.Data!.LockedFare);
        Assert.Equal(37.5m, result.Data.RemainingBalance);
        Assert.Equal(1, result.Data.OriginStationId);
        Assert.Equal(2, result.Data.DestinationStationId);
        Assert.Equal(5, result.Data.DriverId);
        Assert.NotNull(result.Data.TransactionReferenceNumber);
        Assert.StartsWith("TRN-", result.Data.TransactionReferenceNumber!);
        Assert.Matches(@"^TRN-\d{8}-\d{6}$", result.Data.TransactionReferenceNumber!);

        // Verify transaction record was created
        var tx = await context.Transactions.FirstAsync();
        Assert.Equal(TransactionType.PAYMENT, tx.TransactionType);
        Assert.Equal(12.5m, tx.Amount);
        Assert.Equal(5, tx.DriverId);
        Assert.NotNull(tx.PaymentSessionId);
        Assert.NotNull(tx.TransactionReferenceNumber);

        // Session should be COMPLETED
        var session = await context.PaymentSessions.FirstAsync();
        Assert.Equal(PaymentSessionStatus.COMPLETED, session.Status);

        // Wallet balance should be deducted atomically
        var wallet = await context.Wallets.FirstAsync();
        Assert.Equal(37.5m, wallet.Balance);
    }

    [Fact]
    public async Task ScanQR_RejectsInsufficientBalance_WithoutDeducting()
    {
        var (context, _) = await CreateCardAndSession(balance: 5);
        var service = CreateService(context);

        var result = await service.ProcessQRPaymentAsync("test-data", "test-signature", driverId: 5);

        Assert.False(result.Success);
        Assert.Equal("Insufficient balance.", result.Message);

        // No transaction should be recorded
        var txCount = await context.Transactions.CountAsync();
        Assert.Equal(0, txCount);

        // Wallet balance unchanged
        var wallet = await context.Wallets.FirstAsync();
        Assert.Equal(5m, wallet.Balance);

        // Session marked FAILED
        var session = await context.PaymentSessions.FirstAsync();
        Assert.Equal(PaymentSessionStatus.FAILED, session.Status);
    }

    [Fact]
    public async Task ScanQR_ChargesLockedFare_EvenWhenFareRuleChanges()
    {
        var (context, _) = await CreateCardAndSession(fareAmount: 12.5m);
        var service = CreateService(context);

        // Change the fare rule after session creation
        var fareRule = await context.FareRules.FirstAsync();
        fareRule.FareAmount = 25m;
        await context.SaveChangesAsync();

        var result = await service.ProcessQRPaymentAsync("test-data", "test-signature", driverId: 5);

        // Should still charge the LOCKED fare (12.5), not the new 25
        Assert.True(result.Success);
        Assert.Equal(12.5m, result.Data!.LockedFare);
        Assert.Equal(37.5m, result.Data.RemainingBalance);
    }

    [Fact]
    public async Task ScanQR_RejectsInactiveCard()
    {
        var context = CreateContext();
        var card = new Card
        {
            CardNumber = "4111111111111111",
            Status = CardStatus.SUSPENDED,
            PassengerType = PassengerType.Passenger,
            IssueDate = DateTime.UtcNow,
            ExpiryDate = DateTime.UtcNow.AddYears(1)
        };
        context.Cards.Add(card);
        context.Wallets.Add(new Wallet { Card = card, Balance = 50, Status = CardStatus.ACTIVE });
        SeedStationsAndFare(context);
        await context.SaveChangesAsync();

        context.PaymentSessions.Add(new PaymentSession
        {
            PaymentSessionId = Guid.NewGuid(),
            CardId = card.CardId,
            UserId = 1,
            OriginStationId = 1,
            DestinationStationId = 2,
            Fare = 12.5m,
            Status = PaymentSessionStatus.PENDING,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddMinutes(10)
        });
        await context.SaveChangesAsync();

        var service = CreateService(context);
        var result = await service.ProcessQRPaymentAsync("test-data", "test-signature", driverId: 5);

        Assert.False(result.Success);
        Assert.Equal("Card is not active.", result.Message);
    }

    [Fact]
    public async Task TrnGenerator_GeneratesSequentialUniqueReferenceNumbers()
    {
        var context = CreateContext();
        // Add two existing transactions with TRNs for today
        var today = DateTime.UtcNow.ToString("yyyyMMdd");
        context.Transactions.Add(new Models.Transaction
        {
            TransactionReferenceNumber = $"TRN-{today}-000001",
            TransactionType = TransactionType.PAYMENT,
            TransactionName = "Fare payment",
            CreatedAt = DateTime.UtcNow
        });
        context.Transactions.Add(new Models.Transaction
        {
            TransactionReferenceNumber = $"TRN-{today}-000002",
            TransactionType = TransactionType.PAYMENT,
            TransactionName = "Fare payment",
            CreatedAt = DateTime.UtcNow
        });
        await context.SaveChangesAsync();

        var generator = new TransactionReferenceNumberGenerator(context);
        var nextTrn = await generator.GenerateNextAsync();

        Assert.Equal($"TRN-{today}-000003", nextTrn);
    }
}

/// <summary>
/// Mock QR service for testing — always returns the provided card ID.
/// </summary>
public class MockQRService : IQRService
{
    public Task<DTOs.Payment.QRTicketResponse> GenerateOrRetrieveQRAsync(int cardId)
        => Task.FromResult(new DTOs.Payment.QRTicketResponse { CardId = cardId });

    public Task<DTOs.Payment.QRTicketResponse?> GetQRAsync(int cardId)
        => Task.FromResult<DTOs.Payment.QRTicketResponse?>(new DTOs.Payment.QRTicketResponse { CardId = cardId });

    public Task<DTOs.Payment.QRTicketResponse> RegenerateQRAsync(int cardId)
        => Task.FromResult(new DTOs.Payment.QRTicketResponse { CardId = cardId });

    public Task<int> ValidateQRAsync(string qrData, string signature)
        => Task.FromResult(1); // Always returns card ID 1 for testing
}

/// <summary>
/// Mock trip service for testing — always returns an active trip.
/// </summary>
public class MockTripService : ITripService
{
    public Task<Trip> StartTripAsync(int driverId, int originStationId, int finalDestinationStationId)
        => throw new NotImplementedException();

    public Task<Trip> EndTripAsync(int tripId)
        => throw new NotImplementedException();

    public Task<Trip?> GetActiveTripAsync(int driverId)
        => Task.FromResult<Trip?>(new Trip { TripStatus = TripStatus.Active });

    public Task<Trip> CancelTripAsync(int tripId)
        => throw new NotImplementedException();

    public Task<(List<Trip> Trips, int TotalCount, int Page, int PageSize)> GetTripHistoryAsync(int driverId, int page = 1, int pageSize = 20)
        => throw new NotImplementedException();
}

/// <summary>
/// Mock discount service for testing — returns no active discount.
/// </summary>
public class MockDiscountService : IDiscountService
{
    public Task<DiscountType> CreateDiscountTypeAsync(DiscountType discountType)
        => throw new NotImplementedException();

    public Task<DiscountType> UpdateDiscountTypeAsync(int discountTypeId, DiscountType discountType)
        => throw new NotImplementedException();

    public Task<bool> DeleteDiscountTypeAsync(int discountTypeId)
        => throw new NotImplementedException();

    public Task<bool> ActivateDiscountTypeAsync(int discountTypeId)
        => throw new NotImplementedException();

    public Task<bool> DeactivateDiscountTypeAsync(int discountTypeId)
        => throw new NotImplementedException();

    public Task<IEnumerable<DiscountType>> GetAllDiscountTypesAsync()
        => throw new NotImplementedException();

    public Task<DiscountType?> GetDiscountTypeByIdAsync(int discountTypeId)
        => throw new NotImplementedException();

    public Task<DiscountApplication> ApplyForDiscountAsync(int cardId, int discountTypeId, string? discountDocument = null)
        => throw new NotImplementedException();

    public Task<IEnumerable<DiscountApplication>> GetApplicationsByCardAsync(int cardId)
        => throw new NotImplementedException();

    public Task<DiscountApplication> ApproveDiscountApplicationAsync(int applicationId, int adminId)
        => throw new NotImplementedException();

    public Task<DiscountApplication> RejectDiscountApplicationAsync(int applicationId, int adminId, string? rejectionReason = null)
        => throw new NotImplementedException();

    public Task<IEnumerable<DiscountApplication>> GetPendingApplicationsAsync()
        => throw new NotImplementedException();

    public Task<IEnumerable<DiscountApplication>> GetAllApplicationsAsync()
        => throw new NotImplementedException();

    public Task<DiscountApplication?> GetActiveDiscountForCardAsync(int cardId)
        => Task.FromResult<DiscountApplication?>(null);

    public Task<decimal> CalculateDiscountedFareAsync(decimal regularFare, int? discountTypeId)
        => throw new NotImplementedException();
}
