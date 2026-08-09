using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TransitPay.API.Models;

/// <summary>
/// Represents a passenger's application for a discount type.
/// Tracks the approval workflow for discount assignments.
/// </summary>
[Table("discount_applications")]
public class DiscountApplication
{
    [Key]
    [Column("discount_application_id")]
    public int DiscountApplicationId { get; set; }

    /// <summary>
    /// The card being applied for the discount.
    /// </summary>
    [ForeignKey(nameof(Card))]
    [Column("card_id")]
    public int CardId { get; set; }

    /// <summary>
    /// The user (passenger) who submitted this application.
    /// </summary>
    [ForeignKey(nameof(User))]
    [Column("user_id")]
    public int UserId { get; set; }

    /// <summary>
    /// The discount type being applied for.
    /// </summary>
    [ForeignKey(nameof(DiscountType))]
    [Column("discount_type_id")]
    public int DiscountTypeId { get; set; }

    /// <summary>
    /// The discount program this application belongs to (if any).
    /// Links the application to a discount program definition.
    /// </summary>
    [ForeignKey(nameof(DiscountProgram))]
    [Column("discount_program_id")]
    public int? DiscountProgramId { get; set; }

    /// <summary>
    /// Current status of the application.
    /// </summary>
    [Column("status")]
    public DiscountApplicationStatus Status { get; set; } = DiscountApplicationStatus.Pending;

    /// <summary>
    /// The admin who approved this application.
    /// Null if not approved or rejected.
    /// </summary>
    [ForeignKey(nameof(ApprovedByUser))]
    [Column("approved_by")]
    public int? ApprovedBy { get; set; }

    /// <summary>
    /// When the application was approved.
    /// </summary>
    [Column("approved_at")]
    public DateTime? ApprovedAt { get; set; }

    /// <summary>
    /// When the application was rejected.
    /// </summary>
    [Column("rejected_at")]
    public DateTime? RejectedAt { get; set; }

    /// <summary>
    /// Reason for rejection (if rejected).
    /// </summary>
    [Column("rejection_reason")]
    [MaxLength(500)]
    public string? RejectionReason { get; set; }

    /// <summary>
    /// Supporting document or discount ID uploaded by passenger.
    /// </summary>
    [Column("discount_document")]
    [MaxLength(500)]
    public string? DiscountDocument { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime? UpdatedAt { get; set; }

    [Column("deleted_at")]
    public DateTime? DeletedAt { get; set; }

    // Navigation properties
    public Card? Card { get; set; }
    public DiscountType? DiscountType { get; set; }
    public DiscountProgram? DiscountProgram { get; set; }
    public User? ApprovedByUser { get; set; }
    public User? User { get; set; }
}

/// <summary>
/// Status of a discount application.
/// </summary>
public enum DiscountApplicationStatus
{
    /// <summary>
    /// Application is pending admin review.
    /// </summary>
    Pending = 0,

    /// <summary>
    /// Application has been approved by admin.
    /// </summary>
    Approved = 1,

    /// <summary>
    /// Application has been rejected by admin.
    /// </summary>
    Rejected = 2,

    /// <summary>
    /// Application has expired.
    /// </summary>
    Expired = 3
}