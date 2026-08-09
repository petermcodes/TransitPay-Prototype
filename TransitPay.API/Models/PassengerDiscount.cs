using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TransitPay.API.Models;

/// <summary>
/// Represents an approved discount assigned to a passenger's card.
/// Materialized when a discount application is approved.
/// The discount percentage is snapshotted at approval time so future
/// program edits do not retroactively change an approved passenger's discount.
/// </summary>
[Table("passenger_discounts")]
public class PassengerDiscount
{
    [Key]
    [Column("passenger_discount_id")]
    public int PassengerDiscountId { get; set; }

    /// <summary>
    /// The card that has been granted the discount.
    /// </summary>
    [ForeignKey(nameof(Card))]
    [Column("card_id")]
    public int CardId { get; set; }

    /// <summary>
    /// The discount program this passenger discount belongs to.
    /// </summary>
    [ForeignKey(nameof(DiscountProgram))]
    [Column("discount_program_id")]
    public int? DiscountProgramId { get; set; }

    /// <summary>
    /// The discount percentage applied to this passenger.
    /// Snapshotted at approval time — NOT re-read from the program on every payment,
    /// so future program percentage changes only affect new approvals.
    /// </summary>
    [Column("discount_percentage")]
    public decimal DiscountPercentage { get; set; }

    /// <summary>
    /// The current status of this passenger discount.
    /// </summary>
    [Column("status")]
    public PassengerDiscountStatus Status { get; set; } = PassengerDiscountStatus.Active;

    /// <summary>
    /// The admin who approved this discount.
    /// </summary>
    [ForeignKey(nameof(ApprovedByUser))]
    [Column("approved_by")]
    public int? ApprovedBy { get; set; }

    /// <summary>
    /// When this discount was approved.
    /// </summary>
    [Column("approved_at")]
    public DateTime? ApprovedAt { get; set; }

    /// <summary>
    /// When this discount expires (if applicable).
    /// Null means it does not expire.
    /// </summary>
    [Column("expires_at")]
    public DateTime? ExpiresAt { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime? UpdatedAt { get; set; }

    [Column("deleted_at")]
    public DateTime? DeletedAt { get; set; }

    // Navigation properties
    public Card? Card { get; set; }
    public DiscountProgram? DiscountProgram { get; set; }
    public User? ApprovedByUser { get; set; }
}

/// <summary>
/// Status of a passenger's discount.
/// </summary>
public enum PassengerDiscountStatus
{
    /// <summary>
    /// The discount is currently applicable to the card.
    /// </summary>
    Active = 0,

    /// <summary>
    /// The discount has expired.
    /// </summary>
    Expired = 1,

    /// <summary>
    /// The discount was revoked (e.g., replaced by a new approval).
    /// </summary>
    Revoked = 2
}