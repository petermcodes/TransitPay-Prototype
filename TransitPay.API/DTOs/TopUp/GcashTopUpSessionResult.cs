using TransitPay.API.Enums;

namespace TransitPay.API.DTOs.TopUp;

/// <summary>
/// State of a simulated GCash top-up checkout session, as returned to the client
/// after initiate/cancel/status operations.
/// </summary>
public class GcashTopUpSessionResult
{
    /// <summary>The checkout session (payment intent) identifier.</summary>
    public Guid SessionId { get; set; }

    /// <summary>The card whose wallet will be credited.</summary>
    public int CardId { get; set; }

    /// <summary>The amount to credit on successful payment (peso).</summary>
    public decimal Amount { get; set; }

    /// <summary>The Transaction Reference Number of the linked TOP_UP transaction.</summary>
    public string? TransactionReferenceNumber { get; set; }

    /// <summary>Current session status (see <see cref="GcashSessionStatus"/>).</summary>
    public string Status { get; set; } = GcashSessionStatus.PENDING.ToString();

    /// <summary>UTC instant after which the session can no longer be confirmed.</summary>
    public DateTime ExpiresAt { get; set; }

    /// <summary>Simulated GCash reference number (set once COMPLETED).</summary>
    public string? GcashReference { get; set; }
}
