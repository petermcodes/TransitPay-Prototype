namespace TransitPay.API.DTOs.TopUp;

/// <summary>
/// Outcome of confirming a simulated GCash top-up. Carries enough detail for the
/// app to render the receipt (TRN, GCash reference, new balance) or to let the user
/// retry a wrong OTP without losing the session.
/// </summary>
public class GcashTopUpConfirmResult
{
    /// <summary>Whether the payment completed successfully (wallet credited).</summary>
    public bool Success { get; set; }

    /// <summary>Human-readable outcome description for the UI.</summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>Session status after this attempt (PENDING / COMPLETED / FAILED).</summary>
    public string SessionStatus { get; set; } = string.Empty;

    /// <summary>Wrong-OTP attempts left before the payment fails (0 when terminal).</summary>
    public int AttemptsRemaining { get; set; }

    /// <summary>The Transaction Reference Number of the completed top-up (when COMPLETED).</summary>
    public string? TransactionReferenceNumber { get; set; }

    /// <summary>Simulated GCash reference number for the receipt (when COMPLETED).</summary>
    public string? GcashReference { get; set; }

    /// <summary>The wallet balance after the credit (when COMPLETED).</summary>
    public decimal? NewBalance { get; set; }
}
