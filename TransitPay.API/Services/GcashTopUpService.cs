using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using TransitPay.API.Configuration;
using TransitPay.API.Data;
using TransitPay.API.DTOs.TopUp;
using TransitPay.API.Enums;
using TransitPay.API.Interfaces;
using TransitPay.API.Models;

namespace TransitPay.API.Services;

/// <summary>
/// Simulated GCash top-up service ("sandbox" digital payment gateway).
/// Mirrors the lifecycle of a real GCash redirect checkout without moving real money:
///   1. Initiate — creates a PENDING TOP_UP transaction plus a checkout session (payment intent).
///   2. Confirm  — validates the simulated GCash OTP, credits the wallet atomically and
///                 completes the transaction (single-use; idempotent on retries).
///   3. Cancel   — voids a pending session (transaction → CANCELLED, balance untouched).
/// Sessions expire after a configurable window; stale PENDING sessions are lazily expired
/// on the next initiate/status/confirm so no background job is needed.
/// Swapping this class for a real payment service provider (PayMongo, Xendit, ...) later
/// requires no changes to controllers, DTOs or the frontend flow.
/// </summary>
public class GcashTopUpService : IGcashTopUpService
{
    /// <summary>The fixed OTP accepted in simulation mode (surfaced as a sandbox hint in the app UI).</summary>
    public const string SimulatedOtp = "123456";

    /// <summary>Maximum wrong-OTP attempts before the payment fails.</summary>
    public const int MaxOtpAttempts = 3;

    /// <summary>PaymentMode recorded on the TOP_UP transaction.</summary>
    public const string PaymentMode = "GCash";

    private readonly TransitPayDbContext _dbContext;
    private readonly ITransactionReferenceNumberGenerator _trnGenerator;
    private readonly PaymentSettings _settings;
    private readonly ILogger<GcashTopUpService> _logger;

    /// <summary>
    /// Creates a new GcashTopUpService.
    /// </summary>
    public GcashTopUpService(
        TransitPayDbContext dbContext,
        ITransactionReferenceNumberGenerator trnGenerator,
        IOptions<PaymentSettings> settings,
        ILogger<GcashTopUpService> logger)
    {
        _dbContext = dbContext;
        _trnGenerator = trnGenerator;
        _settings = settings.Value;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<GcashTopUpSessionResult> InitiateAsync(int cardId, decimal amount, int userId)
    {
        amount = Math.Round(amount, 2);
        if (amount < _settings.Gcash.MinAmount || amount > _settings.Gcash.MaxAmount)
        {
            throw new InvalidOperationException(
                $"Amount must be between {_settings.Gcash.MinAmount:0} and {_settings.Gcash.MaxAmount:0}.");
        }

        var wallet = await _dbContext.Wallets
            .Include(w => w.Card)
            .FirstOrDefaultAsync(w => w.CardId == cardId);

        if (wallet == null || wallet.Card == null)
        {
            throw new InvalidOperationException("Wallet not found.");
        }

        // Ownership validation: the card must belong to the authenticated passenger
        if (wallet.Card.UserId != userId)
        {
            throw new InvalidOperationException("Card not found.");
        }

        if (wallet.Status != CardStatus.ACTIVE)
        {
            throw new InvalidOperationException("Wallet is not active.");
        }

        // Lazily expire stale pending sessions for this card before opening a new one
        await ExpireStaleSessionsAsync(cardId);

        // Single-active-session invariant: the user started a fresh top-up instead of
        // resuming, so void any still-open checkout for this card (transaction → CANCELLED)
        await CancelOpenSessionsAsync(cardId);

        var now = DateTime.UtcNow;
        var tnr = await _trnGenerator.GenerateNextAsync();

        var transaction = new Transaction
        {
            CardId = cardId,
            Amount = amount,
            TransactionType = TransactionType.TOP_UP,
            TransactionName = "GCash top-up",
            Status = TransactionStatus.PENDING,
            PaymentMode = PaymentMode,
            RegularFare = 0,
            FinalFare = 0,
            RemainingBalance = wallet.Balance,
            TransactionReferenceNumber = tnr,
            CreatedAt = now,
            UpdatedAt = now
        };
        _dbContext.Transactions.Add(transaction);

        var session = new GcashTopUpSession
        {
            SessionId = Guid.NewGuid(),
            CardId = cardId,
            UserId = userId,
            Amount = amount,
            Transaction = transaction,
            Status = GcashSessionStatus.PENDING,
            ExpiresAt = now.AddMinutes(_settings.Gcash.SessionExpiryMinutes),
            CreatedAt = now,
            UpdatedAt = now
        };
        _dbContext.GcashTopUpSessions.Add(session);

        await _dbContext.SaveChangesAsync();

        _logger.LogInformation(
            "GCash top-up session {SessionId} initiated for card {CardId} ({Amount} PHP, TNR {Tnr})",
            session.SessionId, cardId, amount, tnr);

        return ToSessionResult(session);
    }

    /// <inheritdoc />
    public async Task<GcashTopUpConfirmResult> ConfirmAsync(Guid sessionId, string otp, int userId)
    {
        var session = await _dbContext.GcashTopUpSessions
            .Include(s => s.Transaction)
            .FirstOrDefaultAsync(s => s.SessionId == sessionId);

        if (session == null || session.UserId != userId)
        {
            throw new InvalidOperationException("Top-up session not found.");
        }

        // Idempotent retry: the payment already went through (e.g., the first response
        // was lost). Report success again without crediting the wallet a second time.
        if (session.Status == GcashSessionStatus.COMPLETED)
        {
            var completedBalance = await GetCurrentBalanceAsync(session.CardId);
            return new GcashTopUpConfirmResult
            {
                Success = true,
                Message = "Payment already completed.",
                SessionStatus = GcashSessionStatus.COMPLETED.ToString(),
                TransactionReferenceNumber = session.Transaction?.TransactionReferenceNumber,
                GcashReference = session.GcashReference,
                NewBalance = completedBalance
            };
        }

        if (session.Status != GcashSessionStatus.PENDING)
        {
            throw new InvalidOperationException("Top-up session is no longer active.");
        }

        // Lazy expiry at confirm time
        if (session.ExpiresAt <= DateTime.UtcNow)
        {
            await MarkSessionAsync(session, GcashSessionStatus.EXPIRED, TransactionStatus.CANCELLED);
            throw new InvalidOperationException("Top-up session has expired. Please start a new payment.");
        }

        var normalizedOtp = new string((otp ?? string.Empty).Where(char.IsDigit).ToArray());
        if (normalizedOtp != SimulatedOtp)
        {
            return await HandleWrongOtpAsync(session);
        }

        return await CompletePaymentAsync(session);
    }

    /// <summary>
    /// Records a wrong-OTP attempt: fails the payment after <see cref="MaxOtpAttempts"/>
    /// attempts, otherwise keeps the session open and reports attempts remaining.
    /// </summary>
    private async Task<GcashTopUpConfirmResult> HandleWrongOtpAsync(GcashTopUpSession session)
    {
        session.OtpAttempts++;
        session.UpdatedAt = DateTime.UtcNow;

        if (session.OtpAttempts >= MaxOtpAttempts)
        {
            await MarkSessionAsync(session, GcashSessionStatus.FAILED, TransactionStatus.FAILED);
            _logger.LogWarning(
                "GCash top-up session {SessionId} failed after {Attempts} wrong OTP attempts",
                session.SessionId, session.OtpAttempts);

            return new GcashTopUpConfirmResult
            {
                Success = false,
                Message = "Too many incorrect codes. The payment has failed. Please start a new top-up.",
                SessionStatus = GcashSessionStatus.FAILED.ToString(),
                AttemptsRemaining = 0
            };
        }

        await _dbContext.SaveChangesAsync();
        return new GcashTopUpConfirmResult
        {
            Success = false,
            Message = $"Incorrect GCash code. {MaxOtpAttempts - session.OtpAttempts} attempt(s) remaining.",
            SessionStatus = GcashSessionStatus.PENDING.ToString(),
            AttemptsRemaining = MaxOtpAttempts - session.OtpAttempts
        };
    }

    /// <summary>
    /// Credits the wallet and completes the transaction + session in one atomic
    /// database transaction, then returns the receipt data.
    /// </summary>
    private async Task<GcashTopUpConfirmResult> CompletePaymentAsync(GcashTopUpSession session)
    {
        await using var dbTransaction = await _dbContext.Database.BeginTransactionAsync();

        var wallet = await _dbContext.Wallets.FirstOrDefaultAsync(w => w.CardId == session.CardId);
        if (wallet == null)
        {
            throw new InvalidOperationException("Wallet not found.");
        }

        var now = DateTime.UtcNow;
        wallet.Balance += session.Amount;
        wallet.UpdatedAt = now;

        session.Status = GcashSessionStatus.COMPLETED;
        session.CompletedAt = now;
        session.UpdatedAt = now;
        session.GcashReference = GenerateGcashReference();

        if (session.Transaction != null)
        {
            session.Transaction.Status = TransactionStatus.COMPLETED;
            session.Transaction.RemainingBalance = wallet.Balance;
            session.Transaction.UpdatedAt = now;
        }

        await _dbContext.SaveChangesAsync();
        await dbTransaction.CommitAsync();

        _logger.LogInformation(
            "GCash top-up session {SessionId} completed: {Amount} PHP credited to card {CardId} (balance {Balance})",
            session.SessionId, session.Amount, session.CardId, wallet.Balance);

        return new GcashTopUpConfirmResult
        {
            Success = true,
            Message = "Payment successful.",
            SessionStatus = GcashSessionStatus.COMPLETED.ToString(),
            TransactionReferenceNumber = session.Transaction?.TransactionReferenceNumber,
            GcashReference = session.GcashReference,
            NewBalance = wallet.Balance
        };
    }

    /// <inheritdoc />
    public async Task<GcashTopUpSessionResult> CancelAsync(Guid sessionId, int userId)
    {
        var session = await _dbContext.GcashTopUpSessions
            .Include(s => s.Transaction)
            .FirstOrDefaultAsync(s => s.SessionId == sessionId);

        if (session == null || session.UserId != userId)
        {
            throw new InvalidOperationException("Top-up session not found.");
        }

        // Only open sessions can be voided; terminal states are returned as-is (idempotent)
        if (session.Status == GcashSessionStatus.PENDING)
        {
            await MarkSessionAsync(session, GcashSessionStatus.CANCELLED, TransactionStatus.CANCELLED);
            _logger.LogInformation("GCash top-up session {SessionId} cancelled by user {UserId}", sessionId, userId);
        }

        return ToSessionResult(session);
    }

    /// <inheritdoc />
    public async Task<GcashTopUpSessionResult?> GetStatusAsync(Guid sessionId, int userId)
    {
        var session = await _dbContext.GcashTopUpSessions
            .Include(s => s.Transaction)
            .FirstOrDefaultAsync(s => s.SessionId == sessionId);

        if (session == null || session.UserId != userId)
        {
            return null;
        }

        // Surface lazy expiry to status polling
        if (session.Status == GcashSessionStatus.PENDING && session.ExpiresAt <= DateTime.UtcNow)
        {
            await MarkSessionAsync(session, GcashSessionStatus.EXPIRED, TransactionStatus.CANCELLED);
        }

        return ToSessionResult(session);
    }

    /// <inheritdoc />
    public async Task<GcashTopUpSessionResult?> GetActiveSessionAsync(int cardId, int userId)
    {
        // Ownership validation: the card must belong to the authenticated passenger
        var wallet = await _dbContext.Wallets
            .Include(w => w.Card)
            .FirstOrDefaultAsync(w => w.CardId == cardId);

        if (wallet == null || wallet.Card == null)
        {
            throw new InvalidOperationException("Wallet not found.");
        }

        if (wallet.Card.UserId != userId)
        {
            throw new InvalidOperationException("Card not found.");
        }

        // Expire anything stale first, then surface the single open session (if any)
        await ExpireStaleSessionsAsync(cardId);

        var session = await _dbContext.GcashTopUpSessions
            .Include(s => s.Transaction)
            .FirstOrDefaultAsync(s => s.CardId == cardId && s.Status == GcashSessionStatus.PENDING);

        return session == null ? null : ToSessionResult(session);
    }

    /// <summary>
    /// Expires any still-PENDING sessions for the card whose window has elapsed,
    /// cancelling their linked transactions. Keeps history accurate without a
    /// background scheduler.
    /// </summary>
    private async Task ExpireStaleSessionsAsync(int cardId)
    {
        var now = DateTime.UtcNow;
        var staleSessions = await _dbContext.GcashTopUpSessions
            .Include(s => s.Transaction)
            .Where(s => s.CardId == cardId
                        && s.Status == GcashSessionStatus.PENDING
                        && s.ExpiresAt <= now)
            .ToListAsync();

        foreach (var stale in staleSessions)
        {
            stale.Status = GcashSessionStatus.EXPIRED;
            stale.UpdatedAt = now;

            if (stale.Transaction != null)
            {
                stale.Transaction.Status = TransactionStatus.CANCELLED;
                stale.Transaction.UpdatedAt = now;
            }
        }

        if (staleSessions.Count > 0)
        {
            await _dbContext.SaveChangesAsync();
            _logger.LogInformation(
                "Expired {Count} stale GCash top-up session(s) for card {CardId}", staleSessions.Count, cardId);
        }
    }

    /// <summary>
    /// Cancels any still-open PENDING sessions for the card (the user started a new
    /// top-up instead of resuming the previous one). Keeps exactly one open checkout
    /// session per card; voided sessions leave CANCELLED transactions in the history.
    /// </summary>
    private async Task CancelOpenSessionsAsync(int cardId)
    {
        var now = DateTime.UtcNow;
        var openSessions = await _dbContext.GcashTopUpSessions
            .Include(s => s.Transaction)
            .Where(s => s.CardId == cardId && s.Status == GcashSessionStatus.PENDING)
            .ToListAsync();

        foreach (var open in openSessions)
        {
            open.Status = GcashSessionStatus.CANCELLED;
            open.UpdatedAt = now;

            if (open.Transaction != null)
            {
                open.Transaction.Status = TransactionStatus.CANCELLED;
                open.Transaction.UpdatedAt = now;
            }
        }

        if (openSessions.Count > 0)
        {
            await _dbContext.SaveChangesAsync();
            _logger.LogInformation(
                "Auto-cancelled {Count} open GCash top-up session(s) for card {CardId} (new payment started)",
                openSessions.Count, cardId);
        }
    }

    /// <summary>Transitions a session to a terminal state and its transaction to the matching state.</summary>
    private async Task MarkSessionAsync(
        GcashTopUpSession session, GcashSessionStatus sessionStatus, TransactionStatus transactionStatus)
    {
        var now = DateTime.UtcNow;
        session.Status = sessionStatus;
        session.UpdatedAt = now;

        if (session.Transaction != null)
        {
            session.Transaction.Status = transactionStatus;
            session.Transaction.UpdatedAt = now;
        }

        await _dbContext.SaveChangesAsync();
    }

    /// <summary>Fetches the wallet's current balance for receipt rendering.</summary>
    private async Task<decimal?> GetCurrentBalanceAsync(int cardId)
    {
        var wallet = await _dbContext.Wallets.FirstOrDefaultAsync(w => w.CardId == cardId);
        return wallet?.Balance;
    }

    /// <summary>Generates a simulated GCash reference number, e.g., "GC-1A2B3C4D".</summary>
    private static string GenerateGcashReference()
        => $"GC-{Guid.NewGuid().ToString("N")[..8].ToUpperInvariant()}";

    private static GcashTopUpSessionResult ToSessionResult(GcashTopUpSession session) => new()
    {
        SessionId = session.SessionId,
        CardId = session.CardId,
        Amount = session.Amount,
        TransactionReferenceNumber = session.Transaction?.TransactionReferenceNumber,
        Status = session.Status.ToString(),
        ExpiresAt = session.ExpiresAt,
        GcashReference = session.GcashReference
    };
}
