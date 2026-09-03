using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using TransitPay.API.Enums;

namespace TransitPay.API.Models;

/// <summary>
/// Represents a financial transaction: a fare payment collected by a driver or a
/// wallet top-up made by the passenger. Every payment stores a full snapshot of the
/// route, fare breakdown, and resulting wallet balance for audit and reconciliation.
/// </summary>
[Table("transactions")]
public class Transaction
{
    /// <summary>Primary key.</summary>
    [Key]
    [Column("transaction_id")]
    public int TransactionId { get; set; }

    /// <summary>The card involved in the transaction (null for some legacy records).</summary>
    [ForeignKey(nameof(Card))]
    [Column("card_id")]
    public int? CardId { get; set; }

    /// <summary>
    /// The driver (User) who processed this payment via QR scan.
    /// Null if the driver is unknown.
    /// </summary>
    [ForeignKey(nameof(Driver))]
    [Column("driver_id")]
    public int? DriverId { get; set; }

    /// <summary>
    /// The trip this payment transaction belongs to.
    /// Every payment transaction must belong to one trip.
    /// </summary>
    [ForeignKey(nameof(Trip))]
    [Column("trip_id")]
    public int? TripId { get; set; }

    /// <summary>
    /// The origin terminal where the passenger boarded.
    /// </summary>
    [ForeignKey(nameof(OriginTerminal))]
    [Column("origin_terminal_id")]
    public int? OriginTerminalId { get; set; }

    /// <summary>
    /// Snapshot of the origin terminal name at the time of the transaction.
    /// Preserved for historical/audit purposes even if the terminal is later renamed.
    /// </summary>
    [Column("origin_terminal_name")]
    [MaxLength(100)]
    public string? OriginTerminalName { get; set; }

    /// <summary>
    /// The destination terminal (where the passenger is going).
    /// </summary>
    [ForeignKey(nameof(Terminal))]
    [Column("terminal_id")]
    public int? TerminalId { get; set; }

    /// <summary>
    /// Snapshot of the destination terminal name at the time of the transaction.
    /// Preserved for historical/audit purposes even if the terminal is later renamed.
    /// </summary>
    [Column("destination_terminal_name")]
    [MaxLength(100)]
    public string? DestinationTerminalName { get; set; }

    /// <summary>
    /// The fare rule ID that was applied for this transaction.
    /// Links to the FareRule used for historical/audit purposes.
    /// </summary>
    [ForeignKey(nameof(FareRule))]
    [Column("fare_id")]
    public int? FareId { get; set; }

    /// <summary>
    /// The regular fare amount before any discount was applied.
    /// Stored for historical/audit purposes.
    /// </summary>
    [Column("regular_fare")]
    public decimal RegularFare { get; set; }

    /// <summary>
    /// The discount percentage applied (e.g., 50.00 for 50% off).
    /// Null if no discount was applied.
    /// </summary>
    [Column("discount_percentage")]
    public decimal? DiscountPercentage { get; set; }

    /// <summary>
    /// The discount amount deducted from the regular fare.
    /// Calculated as: RegularFare * (DiscountPercentage / 100).
    /// </summary>
    [Column("discount_amount")]
    public decimal? DiscountAmount { get; set; }

    /// <summary>
    /// The final fare amount charged after discount.
    /// This is the actual amount deducted from the wallet.
    /// </summary>
    [Column("final_fare")]
    public decimal FinalFare { get; set; }

    /// <summary>
    /// The wallet balance remaining after this payment was deducted.
    /// Stored for historical/audit purposes.
    /// </summary>
    [Column("remaining_balance")]
    public decimal RemainingBalance { get; set; }

    /// <summary>
    /// The discount type applied (if any).
    /// Links to the DiscountType for historical/audit purposes.
    /// </summary>
    [ForeignKey(nameof(DiscountType))]
    [Column("discount_type_id")]
    public int? DiscountTypeId { get; set; }

    /// <summary>The total amount moved by this transaction (equals FinalFare for payments).</summary>
    [Column("amount")]
    public decimal Amount { get; set; }

    /// <summary>The kind of movement: PAYMENT, TOP_UP, REFUND, or FARE.</summary>
    [Column("transaction_type")]
    public TransactionType TransactionType { get; set; }

    /// <summary>Human-readable name describing the transaction (e.g., "Fare payment: A → B").</summary>
    [Column("transaction_name")]
    public string TransactionName { get; set; } = string.Empty;

    /// <summary>
    /// The status of this transaction (PENDING, COMPLETED, FAILED, CANCELLED).
    /// </summary>
    [Column("status")]
    public TransactionStatus Status { get; set; } = TransactionStatus.COMPLETED;

    /// <summary>
    /// Unique human-readable Transaction Reference Number (TNR).
    /// Format: TNR-XXXXXXXX-XXXXXXXX (e.g., TNR-3F9A2C1E-BD7F4A6C).
    /// Generated using GUID v4 for guaranteed uniqueness.
    /// </summary>
    [Column("transaction_reference_number")]
    public string? TransactionReferenceNumber { get; set; }

    /// <summary>
    /// Unique reference number for receipts (e.g., "TPFR-20260804-0001").
    /// Kept for backward compatibility with existing receipts.
    /// </summary>
    [Column("reference_number")]
    public string? ReferenceNumber { get; set; }

    /// <summary>
    /// Business idempotency key that uniquely identifies a single scan/charge event.
    /// Used to prevent duplicate payments on network retries or double scans.
    /// Nullable for backward compatibility with pre-existing transactions.
    /// </summary>
    [Column("payment_request_key")]
    [MaxLength(64)]
    public string? PaymentRequestKey { get; set; }

    /// <summary>
    /// The payment mode used for top-ups (e.g., "GCash", "PayMaya", "Bank Transfer", "Admin").
    /// Null for fare payments.
    /// </summary>
    [Column("payment_mode")]
    [MaxLength(50)]
    public string? PaymentMode { get; set; }

    /// <summary>When the transaction was created (UTC).</summary>
    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>When the transaction was last updated.</summary>
    [Column("updated_at")]
    public DateTime? UpdatedAt { get; set; }

    /// <summary>Soft-delete timestamp. Null while the record is live.</summary>
    [Column("deleted_at")]
    public DateTime? DeletedAt { get; set; }

    /// <summary>EF Core optimistic concurrency token.</summary>
    [ConcurrencyCheck]
    [Column("row_version")]
    public byte[]? RowVersion { get; set; }

    /// <summary>Navigation property to the card involved.</summary>
    public Card? Card { get; set; }

    /// <summary>Navigation property to the driver who processed the payment.</summary>
    public User? Driver { get; set; }

    /// <summary>Navigation property to the trip the payment belongs to.</summary>
    public Trip? Trip { get; set; }

    /// <summary>Navigation property to the destination terminal.</summary>
    public Terminal? Terminal { get; set; }

    /// <summary>Navigation property to the origin terminal.</summary>
    public Terminal? OriginTerminal { get; set; }

    /// <summary>Navigation property to the fare rule applied.</summary>
    public FareRule? FareRule { get; set; }

    /// <summary>Navigation property to the discount type applied (if any).</summary>
    public DiscountType? DiscountType { get; set; }
}