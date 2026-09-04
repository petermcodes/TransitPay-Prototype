using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using TransitPay.API.Enums;

namespace TransitPay.API.Models;

/// <summary>
/// A simulated GCash top-up checkout session ("payment intent") for the digital
/// wallet top-up flow. Each session is bound to the PENDING <see cref="Transaction"/>
/// it will complete, the card/wallet it credits, and the user who initiated it.
/// Sessions are single-use: confirming moves the session to COMPLETED, credits the
/// wallet and completes the transaction; cancellation, wrong OTP or expiry terminate
/// the session without ever touching the balance. Kept for audit/reconciliation of
/// simulated gateway activity (mirrors what a real PSP would store).
/// </summary>
[Table("gcash_topup_sessions")]
public class GcashTopUpSession
{
    /// <summary>Primary key. The public checkout-session identifier (payment intent ID).</summary>
    [Key]
    [Column("session_id")]
    public Guid SessionId { get; set; }

    /// <summary>The transit card whose wallet will be credited.</summary>
    [ForeignKey(nameof(Card))]
    [Column("card_id")]
    public int CardId { get; set; }

    /// <summary>The TOP_UP transaction this session will complete when paid.</summary>
    [ForeignKey(nameof(Transaction))]
    [Column("transaction_id")]
    public int TransactionId { get; set; }

    /// <summary>
    /// The user who initiated the top-up. Stored (denormalized) so every confirm/cancel
    /// request can be verified against the initiator, not just the card owner.
    /// </summary>
    [Column("user_id")]
    public int UserId { get; set; }

    /// <summary>The amount to credit on successful payment (peso).</summary>
    [Column("amount")]
    public decimal Amount { get; set; }

    /// <summary>The GCash mobile number entered at checkout (stored masked by callers; PII-minimal).</summary>
    [Column("mobile_number")]
    [MaxLength(20)]
    public string? MobileNumber { get; set; }

    /// <summary>Lifecycle status of this checkout session.</summary>
    [Column("status")]
    public GcashSessionStatus Status { get; set; } = GcashSessionStatus.PENDING;

    /// <summary>Number of incorrect OTP attempts made against this session.</summary>
    [Column("otp_attempts")]
    public int OtpAttempts { get; set; }

    /// <summary>Simulated GCash reference number issued on successful payment (e.g., "GC-1A2B3C4D").</summary>
    [Column("gcash_reference")]
    [MaxLength(50)]
    public string? GcashReference { get; set; }

    /// <summary>UTC instant after which the session can no longer be confirmed.</summary>
    [Column("expires_at")]
    public DateTime ExpiresAt { get; set; }

    /// <summary>When the session was created (UTC).</summary>
    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>When the session last changed state (UTC).</summary>
    [Column("updated_at")]
    public DateTime? UpdatedAt { get; set; }

    /// <summary>When the payment was completed (UTC). Null until COMPLETED.</summary>
    [Column("completed_at")]
    public DateTime? CompletedAt { get; set; }

    /// <summary>EF Core optimistic concurrency token.</summary>
    [ConcurrencyCheck]
    [Column("row_version")]
    public byte[]? RowVersion { get; set; }

    /// <summary>Navigation property to the card being topped up.</summary>
    public Card? Card { get; set; }

    /// <summary>Navigation property to the TOP_UP transaction this session completes.</summary>
    public Transaction? Transaction { get; set; }
}
