using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using TransitPay.API.Enums;

namespace TransitPay.API.Models;

[Table("transactions")]
public class Transaction
{
    [Key]
    [Column("transaction_id")]
    public int TransactionId { get; set; }

    [ForeignKey(nameof(Card))]
    [Column("card_id")]
    public int? CardId { get; set; }

    /// <summary>
    /// The payment session that produced this transaction.
    /// Links the transaction to the session for audit purposes.
    /// </summary>
    [ForeignKey(nameof(PaymentSession))]
    [Column("payment_session_id")]
    public Guid? PaymentSessionId { get; set; }

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
    /// The origin station where the passenger boarded.
    /// </summary>
    [ForeignKey(nameof(OriginStation))]
    [Column("origin_station_id")]
    public int? OriginStationId { get; set; }

    /// <summary>
    /// The destination station (where the passenger is going).
    /// This was previously called StationId.
    /// </summary>
    [ForeignKey(nameof(Station))]
    [Column("station_id")]
    public int? StationId { get; set; }

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
    /// The discount type applied (if any).
    /// Links to the DiscountType for historical/audit purposes.
    /// </summary>
    [ForeignKey(nameof(DiscountType))]
    [Column("discount_type_id")]
    public int? DiscountTypeId { get; set; }

    [Column("amount")]
    public decimal Amount { get; set; }

    [Column("transaction_type")]
    public TransactionType TransactionType { get; set; }

    [Column("transaction_name")]
    public string TransactionName { get; set; } = string.Empty;

    /// <summary>
    /// The status of this transaction (PENDING, COMPLETED, FAILED, CANCELLED).
    /// </summary>
    [Column("status")]
    public TransactionStatus Status { get; set; } = TransactionStatus.COMPLETED;

    /// <summary>
    /// Unique human-readable Transaction Reference Number (TRN).
    /// Format: TRN-YYYYMMDD-XXXXXX (e.g., TRN-20260804-000001).
    /// Generated inside the same DB transaction as the wallet deduction.
    /// </summary>
    [Column("transaction_reference_number")]
    public string? TransactionReferenceNumber { get; set; }

    /// <summary>
    /// Unique reference number for receipts (e.g., "TPFR-20260804-0001").
    /// Kept for backward compatibility with existing receipts.
    /// </summary>
    [Column("reference_number")]
    public string? ReferenceNumber { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime? UpdatedAt { get; set; }

    [Column("deleted_at")]
    public DateTime? DeletedAt { get; set; }

    public Card? Card { get; set; }
    public PaymentSession? PaymentSession { get; set; }
    public User? Driver { get; set; }
    public Trip? Trip { get; set; }
    public Station? Station { get; set; }
    public Station? OriginStation { get; set; }
    public FareRule? FareRule { get; set; }
    public DiscountType? DiscountType { get; set; }
}
