using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using TransitPay.API.Enums;

namespace TransitPay.API.Models;

[Table("fare_rules")]
public class FareRule
{
    [Key]
    [Column("fare_id")]
    public int FareId { get; set; }

    [ForeignKey(nameof(OriginTerminal))]
    [Column("origin_terminal_id")]
    public int OriginTerminalId { get; set; }

    [ForeignKey(nameof(DestinationTerminal))]
    [Column("destination_terminal_id")]
    public int DestinationTerminalId { get; set; }

    [Column("vehicle_type")]
    public VehicleType VehicleType { get; set; }

    [Column("passenger_type")]
    public PassengerType PassengerType { get; set; }

    [Column("fare_amount")]
    public decimal FareAmount { get; set; }

    [Column("effective_date")]
    public DateTime EffectiveDate { get; set; } = DateTime.UtcNow;

    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime? UpdatedAt { get; set; }

    [Column("deleted_at")]
    public DateTime? DeletedAt { get; set; }

    public Terminal? OriginTerminal { get; set; }
    public Terminal? DestinationTerminal { get; set; }
}
