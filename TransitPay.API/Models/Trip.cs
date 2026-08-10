using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using TransitPay.API.Enums;

namespace TransitPay.API.Models;

/// <summary>
/// Represents a bus trip (journey) operated by a conductor/driver.
/// A trip has an origin station and a final destination station that identify the route.
/// Only one ACTIVE trip may exist per conductor at a time.
/// </summary>
[Table("trips")]
public class Trip
{
    [Key]
    [Column("trip_id")]
    public int TripId { get; set; }

    /// <summary>
    /// The conductor/driver (User) operating this trip.
    /// </summary>
    [ForeignKey(nameof(Driver))]
    [Column("driver_id")]
    public int DriverId { get; set; }

    /// <summary>
    /// The bus (vehicle) assigned to this trip.
    /// Nullable because no Bus entity exists yet; kept as a plain identifier column.
    /// </summary>
    [Column("bus_id")]
    public int? BusId { get; set; }

    /// <summary>
    /// The terminal where the trip starts. Never changes after the trip has started.
    /// Nullable because a trip can be started before the driver selects the origin.
    /// </summary>
    [ForeignKey(nameof(OriginTerminal))]
    [Column("origin_terminal_id")]
    public int? OriginTerminalId { get; set; }

    /// <summary>
    /// The final destination terminal of the trip (the route's terminus).
    /// Identifies the route together with the origin terminal.
    /// Nullable because a trip can be started before the driver selects the destination.
    /// </summary>
    [ForeignKey(nameof(FinalDestinationTerminal))]
    [Column("final_destination_terminal_id")]
    public int? FinalDestinationTerminalId { get; set; }

    /// <summary>
    /// The terminal where passengers are currently boarding.
    /// Initialized to the trip's origin terminal when the trip starts.
    /// Updated by the conductor when passengers board at a different terminal.
    /// </summary>
    [ForeignKey(nameof(CurrentBoardingOriginTerminal))]
    [Column("current_boarding_origin_terminal_id")]
    public int? CurrentBoardingOriginTerminalId { get; set; }

    /// <summary>
    /// When the current boarding origin was last updated.
    /// Null if the boarding origin has never been changed from the initial value.
    /// </summary>
    [Column("boarding_origin_updated_at")]
    public DateTime? BoardingOriginUpdatedAt { get; set; }

    /// <summary>
    /// Human-readable route name (e.g., "Central Terminal → Airport Terminal").
    /// Auto-generated from the origin and final destination terminal names.
    /// </summary>
    [Column("route_name")]
    public string RouteName { get; set; } = string.Empty;

    [Column("trip_status")]
    public TripStatus TripStatus { get; set; } = TripStatus.Pending;

    /// <summary>
    /// When the trip actually departed (set when the trip is started).
    /// </summary>
    [Column("started_at")]
    public DateTime? StartedAt { get; set; }

    /// <summary>
    /// When the trip ended (set when completed or cancelled while active).
    /// </summary>
    [Column("ended_at")]
    public DateTime? EndedAt { get; set; }

    /// <summary>
    /// Running count of passengers boarded on this trip.
    /// Incremented by the payment service for each successful payment.
    /// </summary>
    [Column("passenger_count")]
    public int PassengerCount { get; set; }

    /// <summary>
    /// Running total revenue collected on this trip.
    /// Incremented by the payment service for each successful payment.
    /// </summary>
    [Column("total_revenue")]
    public decimal TotalRevenue { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime? UpdatedAt { get; set; }

    [ConcurrencyCheck]
    [Column("row_version")]
    public byte[]? RowVersion { get; set; }

    public User? Driver { get; set; }
    public Terminal? OriginTerminal { get; set; }
    public Terminal? FinalDestinationTerminal { get; set; }
    public Terminal? CurrentBoardingOriginTerminal { get; set; }
    public ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
}