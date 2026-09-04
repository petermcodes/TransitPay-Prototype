using TransitPay.API.DTOs.TopUp;

namespace TransitPay.API.Interfaces;

/// <summary>
/// Simulated GCash wallet top-up gateway ("sandbox" digital payments).
/// Mirrors the lifecycle of a real GCash redirect checkout without moving real money.
/// Implementations must enforce card ownership (userId) on every operation.
/// </summary>
public interface IGcashTopUpService
{
    /// <summary>
    /// Creates a PENDING TOP_UP transaction plus a checkout session (payment intent)
    /// for the given card. Throws <see cref="InvalidOperationException"/> when the
    /// wallet does not exist, is not owned by <paramref name="userId"/>, is inactive,
    /// or the amount is outside the configured range.
    /// </summary>
    Task<GcashTopUpSessionResult> InitiateAsync(int cardId, decimal amount, int userId);

    /// <summary>
    /// Confirms payment on a checkout session using the simulated GCash OTP.
    /// On success the wallet is credited atomically and the session/transaction are
    /// COMPLETED. Wrong OTPs consume attempts (3 max → FAILED). Confirming an already
    /// COMPLETED session is idempotent (no double credit). Throws
    /// <see cref="InvalidOperationException"/> for unknown sessions, foreign users or
    /// expired/terminated sessions.
    /// </summary>
    Task<GcashTopUpConfirmResult> ConfirmAsync(Guid sessionId, string otp, int userId);

    /// <summary>
    /// Cancels a PENDING checkout session (transaction → CANCELLED, no balance change).
    /// Cancelling a session in a terminal state is a no-op that returns its current state.
    /// </summary>
    Task<GcashTopUpSessionResult> CancelAsync(Guid sessionId, int userId);

    /// <summary>
    /// Returns the current state of a checkout session (lazily expiring stale pending
    /// sessions), or null when the session does not exist or belongs to another user.
    /// </summary>
    Task<GcashTopUpSessionResult?> GetStatusAsync(Guid sessionId, int userId);
}
