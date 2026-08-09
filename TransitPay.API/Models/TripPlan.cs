using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TransitPay.API.Models;

[Table("trip_plans")]
public class TripPlan
{
    [Key]
    [Column("plan_id")]
    public int PlanId { get; set; }

    [Column("user_id")]
    public int UserId { get; set; }

    [Column("card_id")]
    public int CardId { get; set; }

    [ForeignKey(nameof(UserId))]
    public User? User { get; set; }

    [ForeignKey(nameof(CardId))]
    public Card? Card { get; set; }

    [Column("origin_terminal_id")]
    public int OriginTerminalId { get; set; }

    [ForeignKey(nameof(OriginTerminalId))]
    public Terminal? OriginTerminal { get; set; }

    [Column("destination_terminal_id")]
    public int DestinationTerminalId { get; set; }

    [ForeignKey(nameof(DestinationTerminalId))]
    public Terminal? DestinationTerminal { get; set; }

    [Column("status")]
    [MaxLength(20)]
    public string Status { get; set; } = "Active"; // Active, Cancelled, Used

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("expires_at")]
    public DateTime? ExpiresAt { get; set; }

    [Column("used_at")]
    public DateTime? UsedAt { get; set; }

    [Column("updated_at")]
    public DateTime? UpdatedAt { get; set; }

    [Column("normal_fare")]
    public decimal NormalFare { get; set; } = 20.00m;

    [Column("discount_amount")]
    public decimal? DiscountAmount { get; set; }

    [Column("discount_percentage")]
    public decimal? DiscountPercentage { get; set; }

    [Column("final_fare_price")]
    public decimal FinalFarePrice { get; set; } = 20.00m;
}
