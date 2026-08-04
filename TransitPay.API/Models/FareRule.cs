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

    [ForeignKey(nameof(OriginStation))]
    [Column("origin_station_id")]
    public int OriginStationId { get; set; }

    [ForeignKey(nameof(DestinationStation))]
    [Column("destination_station_id")]
    public int DestinationStationId { get; set; }

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

    public Station? OriginStation { get; set; }
    public Station? DestinationStation { get; set; }
}
