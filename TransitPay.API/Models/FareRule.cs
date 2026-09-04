using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using TransitPay.API.Enums;

namespace TransitPay.API.Models;

/// <summary>
/// Represents a fare matrix entry: the fare amount charged for travelling between a
/// pair of terminals for a given vehicle and passenger type.
/// The matching rule that is active and effective is used by
/// <see cref="TransitPay.API.Services.FareCalculator"/> (and therefore by Trip Plans
/// and conductor payments).
/// </summary>
[Table("fare_rules")]
public class FareRule
{
    /// <summary>Primary key.</summary>
    [Key]
    [Column("fare_id")]
    public int FareId { get; set; }

    /// <summary>The boarding terminal for this fare rule.</summary>
    [ForeignKey(nameof(OriginTerminal))]
    [Column("origin_terminal_id")]
    public int OriginTerminalId { get; set; }

    /// <summary>The alighting terminal for this fare rule.</summary>
    [ForeignKey(nameof(DestinationTerminal))]
    [Column("destination_terminal_id")]
    public int DestinationTerminalId { get; set; }

    /// <summary>The vehicle type this fare applies to (e.g., BUS).</summary>
    [Column("vehicle_type")]
    public VehicleType VehicleType { get; set; }

    /// <summary>The passenger type this fare applies to (e.g., Passenger, Senior).</summary>
    [Column("passenger_type")]
    public PassengerType PassengerType { get; set; }

    /// <summary>The fare amount charged for this route/vehicle/passenger combination.</summary>
    [Column("fare_amount")]
    public decimal FareAmount { get; set; }

    /// <summary>The date from which this fare rule is effective.</summary>
    [Column("effective_date")]
    public DateTime EffectiveDate { get; set; } = DateTime.UtcNow;

    /// <summary>Whether this fare rule is currently active. Inactive rules are ignored by fare lookup.</summary>
    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    /// <summary>When the fare rule was created (UTC).</summary>
    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>When the fare rule was last updated.</summary>
    [Column("updated_at")]
    public DateTime? UpdatedAt { get; set; }

    /// <summary>Soft-delete timestamp. Null while the record is live.</summary>
    [Column("deleted_at")]
    public DateTime? DeletedAt { get; set; }

    /// <summary>Navigation property to the origin terminal.</summary>
    public Terminal? OriginTerminal { get; set; }

    /// <summary>Navigation property to the destination terminal.</summary>
    public Terminal? DestinationTerminal { get; set; }
}
