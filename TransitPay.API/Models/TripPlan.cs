using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TransitPay.API.Models;

/// <summary>
/// Represents a passenger's planned journey (Trip Plan).
/// A plan locks the fare for a given origin → destination route onto a card at
/// creation time. The conductor payment flow charges the snapshotted fare recorded
/// here, so the charged amount always matches what the passenger was quoted.
/// A plan is Active until it is Cancelled, Used (paid) or expires 24 hours after
/// creation/update.
/// </summary>
[Table("trip_plans")]
public class TripPlan
{
    /// <summary>Primary key.</summary>
    [Key]
    [Column("plan_id")]
    public int PlanId { get; set; }

    /// <summary>The passenger (User) who owns this plan.</summary>
    [Column("user_id")]
    public int UserId { get; set; }

    /// <summary>The transit card the plan is bound to. Only one ACTIVE plan is allowed per card.</summary>
    [Column("card_id")]
    public int CardId { get; set; }

    /// <summary>Navigation property to the owning user.</summary>
    [ForeignKey(nameof(UserId))]
    public User? User { get; set; }

    /// <summary>Navigation property to the bound card.</summary>
    [ForeignKey(nameof(CardId))]
    public Card? Card { get; set; }

    /// <summary>The planned boarding terminal ID.</summary>
    [Column("origin_terminal_id")]
    public int OriginTerminalId { get; set; }

    /// <summary>Navigation property to the origin terminal.</summary>
    [ForeignKey(nameof(OriginTerminalId))]
    public Terminal? OriginTerminal { get; set; }

    /// <summary>The planned alighting terminal ID.</summary>
    [Column("destination_terminal_id")]
    public int DestinationTerminalId { get; set; }

    /// <summary>Navigation property to the destination terminal.</summary>
    [ForeignKey(nameof(DestinationTerminalId))]
    public Terminal? DestinationTerminal { get; set; }

    /// <summary>Plan state: "Active", "Cancelled", or "Used".</summary>
    [Column("status")]
    [MaxLength(20)]
    public string Status { get; set; } = "Active"; // Active, Cancelled, Used

    /// <summary>When the plan was created (UTC).</summary>
    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>When the plan expires (UTC). Plans are only valid for 24 hours.</summary>
    [Column("expires_at")]
    public DateTime? ExpiresAt { get; set; }

    /// <summary>When the plan was consumed by a payment (status "Used"). Null until paid.</summary>
    [Column("used_at")]
    public DateTime? UsedAt { get; set; }

    /// <summary>When the plan was last updated (e.g., destination change).</summary>
    [Column("updated_at")]
    public DateTime? UpdatedAt { get; set; }

    /// <summary>The base (undiscounted) fare locked in for this route at creation time.</summary>
    [Column("normal_fare")]
    public decimal NormalFare { get; set; } = 20.00m;

    /// <summary>The absolute discount amount locked in, or null when no discount applied.</summary>
    [Column("discount_amount")]
    public decimal? DiscountAmount { get; set; }

    /// <summary>The snapshotted discount percentage, or null when no discount applied.</summary>
    [Column("discount_percentage")]
    public decimal? DiscountPercentage { get; set; }

    /// <summary>The final fare the passenger will be charged (normal fare minus discount).</summary>
    [Column("final_fare_price")]
    public decimal FinalFarePrice { get; set; } = 20.00m;
}
