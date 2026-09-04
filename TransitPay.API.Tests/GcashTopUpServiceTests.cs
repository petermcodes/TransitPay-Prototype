using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using TransitPay.API.Configuration;
using TransitPay.API.Data;
using TransitPay.API.Enums;
using TransitPay.API.Models;
using TransitPay.API.Services;
using Xunit;

namespace TransitPay.API.Tests;

/// <summary>
/// Unit tests for the simulated GCash top-up service using an in-memory EF Core
/// database: session lifecycle (pending → completed/failed/cancelled/expired),
/// the sandbox OTP flow, ownership enforcement, amount validation and idempotent
/// confirmation (no double credit).
/// </summary>
public class GcashTopUpServiceTests
{
    private static TransitPayDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<TransitPayDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            // The service wraps wallet credit + transaction completion in a DB
            // transaction; the in-memory provider no-ops them (warning ignored).
            .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        return new TransitPayDbContext(options);
    }

    private static GcashTopUpService CreateService(TransitPayDbContext context) =>
        new(context, new TransactionReferenceNumberGenerator(),
            Options.Create(new PaymentSettings()), NullLogger<GcashTopUpService>.Instance);

    private static async Task<(Card Card, Wallet Wallet)> SeedCardAsync(
        TransitPayDbContext context, int userId = 10, decimal balance = 100m)
    {
        var card = new Card
        {
            CardNumber = "4111-1111-1111-4242",
            UserId = userId,
            Status = CardStatus.ACTIVE,
            PassengerType = PassengerType.Passenger,
            IssueDate = DateTime.UtcNow,
            ExpiryDate = DateTime.UtcNow.AddYears(2),
            CreatedAt = DateTime.UtcNow
        };
        context.Cards.Add(card);

        var wallet = new Wallet
        {
            Card = card,
            Balance = balance,
            Status = CardStatus.ACTIVE,
            CreatedAt = DateTime.UtcNow
        };
        context.Wallets.Add(wallet);
        await context.SaveChangesAsync();
        return (card, wallet);
    }

    [Fact]
    public async Task Initiate_CreatesPendingSession_AndPendingTopUpTransaction()
    {
        using var context = CreateContext();
        var (card, wallet) = await SeedCardAsync(context, balance: 100m);

        var service = CreateService(context);
        var result = await service.InitiateAsync(card.CardId, 150m, userId: 10);

        Assert.NotEqual(Guid.Empty, result.SessionId);
        Assert.Equal("PENDING", result.Status);
        Assert.Equal(150m, result.Amount);
        Assert.StartsWith("TNR-", result.TransactionReferenceNumber);
        Assert.True(result.ExpiresAt > DateTime.UtcNow);

        var transaction = await context.Transactions.SingleAsync(t => t.CardId == card.CardId);
        Assert.Equal(TransactionType.TOP_UP, transaction.TransactionType);
        Assert.Equal(TransactionStatus.PENDING, transaction.Status);
        Assert.Equal(GcashTopUpService.PaymentMode, transaction.PaymentMode);
        Assert.Equal("GCash top-up", transaction.TransactionName);
        Assert.Equal(wallet.Balance, transaction.RemainingBalance);

        var session = await context.GcashTopUpSessions.SingleAsync(s => s.SessionId == result.SessionId);
        Assert.Equal(GcashSessionStatus.PENDING, session.Status);
        Assert.Equal(transaction.TransactionId, session.TransactionId);
        Assert.Equal(10, session.UserId);
    }

    [Fact]
    public async Task Initiate_DoesNotChangeBalance()
    {
        using var context = CreateContext();
        var (card, wallet) = await SeedCardAsync(context, balance: 100m);

        var service = CreateService(context);
        await service.InitiateAsync(card.CardId, 150m, userId: 10);

        var walletAfter = await context.Wallets.SingleAsync(w => w.CardId == card.CardId);
        Assert.Equal(100m, walletAfter.Balance);
        Assert.Equal(100m, wallet.Balance);
    }

    [Fact]
    public async Task Initiate_RejectsAmountOutOfRange()
    {
        using var context = CreateContext();
        var (card, _) = await SeedCardAsync(context);

        var service = CreateService(context);

        var exZero = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.InitiateAsync(card.CardId, 0m, userId: 10));
        Assert.Contains("Amount must be between", exZero.Message);

        var exTooLarge = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.InitiateAsync(card.CardId, 20000m, userId: 10));
        Assert.Contains("Amount must be between", exTooLarge.Message);
    }

    [Fact]
    public async Task Initiate_RejectsCardOwnedByAnotherUser()
    {
        using var context = CreateContext();
        var (card, _) = await SeedCardAsync(context, userId: 10);

        var service = CreateService(context);
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.InitiateAsync(card.CardId, 100m, userId: 99));

        Assert.Equal("Card not found.", ex.Message);
    }

    [Fact]
    public async Task Confirm_WithCorrectOtp_CreditsWallet_AndCompletesTransaction()
    {
        using var context = CreateContext();
        var (card, _) = await SeedCardAsync(context, balance: 100m);

        var service = CreateService(context);
        var session = await service.InitiateAsync(card.CardId, 150m, userId: 10);
        var result = await service.ConfirmAsync(session.SessionId, "123456", userId: 10);

        Assert.True(result.Success);
        Assert.Equal("COMPLETED", result.SessionStatus);
        Assert.Equal(250m, result.NewBalance);
        Assert.NotNull(result.GcashReference);
        Assert.StartsWith("GC-", result.GcashReference);
        Assert.Equal(session.TransactionReferenceNumber, result.TransactionReferenceNumber);

        var walletAfter = await context.Wallets.SingleAsync(w => w.CardId == card.CardId);
        Assert.Equal(250m, walletAfter.Balance);

        var transaction = await context.Transactions.SingleAsync(t => t.CardId == card.CardId);
        Assert.Equal(TransactionStatus.COMPLETED, transaction.Status);
        Assert.Equal(250m, transaction.RemainingBalance);

        var sessionEntity = await context.GcashTopUpSessions.SingleAsync(s => s.SessionId == session.SessionId);
        Assert.Equal(GcashSessionStatus.COMPLETED, sessionEntity.Status);
        Assert.NotNull(sessionEntity.CompletedAt);
    }

    [Fact]
    public async Task Confirm_WithWrongOtp_DoesNotCredit_ButKeepsSessionOpen()
    {
        using var context = CreateContext();
        var (card, _) = await SeedCardAsync(context, balance: 100m);

        var service = CreateService(context);
        var session = await service.InitiateAsync(card.CardId, 100m, userId: 10);
        var result = await service.ConfirmAsync(session.SessionId, "999999", userId: 10);

        Assert.False(result.Success);
        Assert.Equal("PENDING", result.SessionStatus);
        Assert.Equal(2, result.AttemptsRemaining);
        Assert.Contains("Incorrect GCash code", result.Message);

        var walletAfter = await context.Wallets.SingleAsync(w => w.CardId == card.CardId);
        Assert.Equal(100m, walletAfter.Balance);

        var sessionEntity = await context.GcashTopUpSessions.SingleAsync(s => s.SessionId == session.SessionId);
        Assert.Equal(GcashSessionStatus.PENDING, sessionEntity.Status);
        Assert.Equal(1, sessionEntity.OtpAttempts);

        var transaction = await context.Transactions.SingleAsync(t => t.CardId == card.CardId);
        Assert.Equal(TransactionStatus.PENDING, transaction.Status);
    }

    [Fact]
    public async Task Confirm_WithThreeWrongOtps_FailsPayment()
    {
        using var context = CreateContext();
        var (card, _) = await SeedCardAsync(context, balance: 100m);

        var service = CreateService(context);
        var session = await service.InitiateAsync(card.CardId, 100m, userId: 10);

        var first = await service.ConfirmAsync(session.SessionId, "111111", userId: 10);
        var second = await service.ConfirmAsync(session.SessionId, "222222", userId: 10);
        var third = await service.ConfirmAsync(session.SessionId, "333333", userId: 10);

        Assert.False(first.Success);
        Assert.Equal(2, first.AttemptsRemaining);
        Assert.False(second.Success);
        Assert.Equal(1, second.AttemptsRemaining);
        Assert.False(third.Success);
        Assert.Equal(0, third.AttemptsRemaining);
        Assert.Equal("FAILED", third.SessionStatus);

        var walletAfter = await context.Wallets.SingleAsync(w => w.CardId == card.CardId);
        Assert.Equal(100m, walletAfter.Balance);

        var sessionEntity = await context.GcashTopUpSessions.SingleAsync(s => s.SessionId == session.SessionId);
        Assert.Equal(GcashSessionStatus.FAILED, sessionEntity.Status);

        var transaction = await context.Transactions.SingleAsync(t => t.CardId == card.CardId);
        Assert.Equal(TransactionStatus.FAILED, transaction.Status);

        // A failed session can no longer be confirmed, even with the correct OTP
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.ConfirmAsync(session.SessionId, "123456", userId: 10));
    }

    [Fact]
    public async Task Confirm_AfterCompletion_IsIdempotent_AndDoesNotDoubleCredit()
    {
        using var context = CreateContext();
        var (card, _) = await SeedCardAsync(context, balance: 100m);

        var service = CreateService(context);
        var session = await service.InitiateAsync(card.CardId, 150m, userId: 10);

        await service.ConfirmAsync(session.SessionId, "123456", userId: 10);
        var retry = await service.ConfirmAsync(session.SessionId, "123456", userId: 10);

        Assert.True(retry.Success);
        Assert.Equal("Payment already completed.", retry.Message);
        Assert.Equal(250m, retry.NewBalance);

        var walletAfter = await context.Wallets.SingleAsync(w => w.CardId == card.CardId);
        Assert.Equal(250m, walletAfter.Balance);
    }

    [Fact]
    public async Task Confirm_WithExpiredSession_Throws_AndMarksCancelledWithoutCredit()
    {
        using var context = CreateContext();
        var (card, _) = await SeedCardAsync(context, balance: 100m);

        var service = CreateService(context);
        var session = await service.InitiateAsync(card.CardId, 100m, userId: 10);

        // Backdate the session past its expiry window
        var sessionEntity = await context.GcashTopUpSessions.SingleAsync(s => s.SessionId == session.SessionId);
        sessionEntity.ExpiresAt = DateTime.UtcNow.AddMinutes(-1);
        await context.SaveChangesAsync();

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.ConfirmAsync(session.SessionId, "123456", userId: 10));
        Assert.Contains("expired", ex.Message);

        var walletAfter = await context.Wallets.SingleAsync(w => w.CardId == card.CardId);
        Assert.Equal(100m, walletAfter.Balance);

        var expired = await context.GcashTopUpSessions.SingleAsync(s => s.SessionId == session.SessionId);
        Assert.Equal(GcashSessionStatus.EXPIRED, expired.Status);

        var transaction = await context.Transactions.SingleAsync(t => t.CardId == card.CardId);
        Assert.Equal(TransactionStatus.CANCELLED, transaction.Status);
    }

    [Fact]
    public async Task Cancel_MarksPendingSession_AndTransactionCancelled_WithoutCredit()
    {
        using var context = CreateContext();
        var (card, _) = await SeedCardAsync(context, balance: 100m);

        var service = CreateService(context);
        var session = await service.InitiateAsync(card.CardId, 200m, userId: 10);
        var result = await service.CancelAsync(session.SessionId, userId: 10);

        Assert.Equal("CANCELLED", result.Status);

        var walletAfter = await context.Wallets.SingleAsync(w => w.CardId == card.CardId);
        Assert.Equal(100m, walletAfter.Balance);

        var sessionEntity = await context.GcashTopUpSessions.SingleAsync(s => s.SessionId == session.SessionId);
        Assert.Equal(GcashSessionStatus.CANCELLED, sessionEntity.Status);

        var transaction = await context.Transactions.SingleAsync(t => t.CardId == card.CardId);
        Assert.Equal(TransactionStatus.CANCELLED, transaction.Status);
    }

    [Fact]
    public async Task Confirm_And_Cancel_RejectAnotherUser()
    {
        using var context = CreateContext();
        var (card, _) = await SeedCardAsync(context, userId: 10);

        var service = CreateService(context);
        var session = await service.InitiateAsync(card.CardId, 100m, userId: 10);

        var confirmEx = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.ConfirmAsync(session.SessionId, "123456", userId: 99));
        Assert.Equal("Top-up session not found.", confirmEx.Message);

        var cancelEx = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.CancelAsync(session.SessionId, userId: 99));
        Assert.Equal("Top-up session not found.", cancelEx.Message);

        var status = await service.GetStatusAsync(session.SessionId, userId: 99);
        Assert.Null(status);
    }

    [Fact]
    public async Task Initiate_ExpiresStalePendingSessions_ForTheSameCard()
    {
        using var context = CreateContext();
        var (card, _) = await SeedCardAsync(context, balance: 100m);

        var service = CreateService(context);
        var first = await service.InitiateAsync(card.CardId, 50m, userId: 10);

        // Backdate the first session so it is stale
        var firstEntity = await context.GcashTopUpSessions.SingleAsync(s => s.SessionId == first.SessionId);
        firstEntity.ExpiresAt = DateTime.UtcNow.AddMinutes(-1);
        await context.SaveChangesAsync();

        var second = await service.InitiateAsync(card.CardId, 75m, userId: 10);

        var stale = await context.GcashTopUpSessions.SingleAsync(s => s.SessionId == first.SessionId);
        Assert.Equal(GcashSessionStatus.EXPIRED, stale.Status);

        var staleTransaction = await context.Transactions.SingleAsync(t => t.TransactionId == stale.TransactionId);
        Assert.Equal(TransactionStatus.CANCELLED, staleTransaction.Status);

        var fresh = await context.GcashTopUpSessions.SingleAsync(s => s.SessionId == second.SessionId);
        Assert.Equal(GcashSessionStatus.PENDING, fresh.Status);
    }

    [Fact]
    public async Task GetStatus_LazilyExpiresStaleSession()
    {
        using var context = CreateContext();
        var (card, _) = await SeedCardAsync(context, userId: 10);

        var service = CreateService(context);
        var session = await service.InitiateAsync(card.CardId, 50m, userId: 10);

        var sessionEntity = await context.GcashTopUpSessions.SingleAsync(s => s.SessionId == session.SessionId);
        sessionEntity.ExpiresAt = DateTime.UtcNow.AddMinutes(-1);
        await context.SaveChangesAsync();

        var status = await service.GetStatusAsync(session.SessionId, userId: 10);

        Assert.NotNull(status);
        Assert.Equal("EXPIRED", status!.Status);
    }

    [Fact]
    public async Task GetActiveSession_ReturnsTheOpenPendingSession()
    {
        using var context = CreateContext();
        var (card, _) = await SeedCardAsync(context, userId: 10);

        var service = CreateService(context);
        var created = await service.InitiateAsync(card.CardId, 100m, userId: 10);

        var active = await service.GetActiveSessionAsync(card.CardId, userId: 10);

        Assert.NotNull(active);
        Assert.Equal(created.SessionId, active!.SessionId);
        Assert.Equal("PENDING", active.Status);
        Assert.Equal(100m, active.Amount);
        Assert.Equal(created.TransactionReferenceNumber, active.TransactionReferenceNumber);
    }

    [Fact]
    public async Task GetActiveSession_ReturnsNull_WhenNoSessionExists()
    {
        using var context = CreateContext();
        var (card, _) = await SeedCardAsync(context, userId: 10);

        var service = CreateService(context);
        var active = await service.GetActiveSessionAsync(card.CardId, userId: 10);

        Assert.Null(active);
    }

    [Fact]
    public async Task GetActiveSession_LazilyExpiresStaleSession_AndReturnsNull()
    {
        using var context = CreateContext();
        var (card, _) = await SeedCardAsync(context, userId: 10);

        var service = CreateService(context);
        await service.InitiateAsync(card.CardId, 100m, userId: 10);

        // Simulate the session expiring while the app was closed
        var sessionEntity = await context.GcashTopUpSessions.SingleAsync();
        sessionEntity.ExpiresAt = DateTime.UtcNow.AddMinutes(-1);
        await context.SaveChangesAsync();

        var active = await service.GetActiveSessionAsync(card.CardId, userId: 10);

        Assert.Null(active);

        var expired = await context.GcashTopUpSessions.SingleAsync();
        Assert.Equal(GcashSessionStatus.EXPIRED, expired.Status);
        var transaction = await context.Transactions.SingleAsync();
        Assert.Equal(TransactionStatus.CANCELLED, transaction.Status);
    }

    [Fact]
    public async Task GetActiveSession_RejectsCardOwnedByAnotherUser()
    {
        using var context = CreateContext();
        var (card, _) = await SeedCardAsync(context, userId: 10);

        var service = CreateService(context);
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.GetActiveSessionAsync(card.CardId, userId: 99));

        Assert.Equal("Card not found.", ex.Message);
    }

    [Fact]
    public async Task Initiate_AutoCancelsExistingOpenSession()
    {
        using var context = CreateContext();
        var (card, _) = await SeedCardAsync(context, userId: 10);

        var service = CreateService(context);
        var first = await service.InitiateAsync(card.CardId, 50m, userId: 10);
        var second = await service.InitiateAsync(card.CardId, 75m, userId: 10);

        // Starting a fresh top-up voids the abandoned checkout (single-active-session invariant)
        var firstEntity = await context.GcashTopUpSessions.SingleAsync(s => s.SessionId == first.SessionId);
        Assert.Equal(GcashSessionStatus.CANCELLED, firstEntity.Status);
        var firstTransaction = await context.Transactions.SingleAsync(t => t.TransactionId == firstEntity.TransactionId);
        Assert.Equal(TransactionStatus.CANCELLED, firstTransaction.Status);

        var secondEntity = await context.GcashTopUpSessions.SingleAsync(s => s.SessionId == second.SessionId);
        Assert.Equal(GcashSessionStatus.PENDING, secondEntity.Status);
    }
}
