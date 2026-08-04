using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using TransitPay.API.Enums;

namespace TransitPay.API.Models;

/// <summary>
/// Represents a pending payment session created when a passenger selects a route.
/// The fare is locked at creation time and used for the lifetime of the session.
/// Each card may have only one active session (PENDING/SCANNING/PROCESSING) at a time.
/// </summary>
[Table("payment_sessions")]
public class PaymentSession
{
    [Key]
    [Column("payment_session_id")]
    public Guid PaymentSessionId { get; set; } = Guid.NewGuid();

    /// <summary>
    /// The TransitPay card associated with this session.
    /// </summary>
    [ForeignKey(nameof(Card))]
    [Column("card_id")]
    public int CardId { get; set; }

    /// <summary>
    /// The passenger account (User) that owns this session.
    /// Improves traceability and supports future multi-card scenarios.
    /// </summary>
    [ForeignKey(nameof(User))]
    [Column("user_id")]
    public int UserId { get; set; }

    [ForeignKey(nameof(OriginStation))]
    [Column("origin_station_id")]
    public int OriginStationId { get; set; }

    [ForeignKey(nameof(DestinationStation))]
    [Column("destination_station_id")]
    public int DestinationStationId { get; set; }

    /// <summary>
    /// The locked fare for this session, determined by the backend at creation time.
    /// This fare is NOT recalculated during driver scan.
    /// </summary>
    [Column("fare")]
    public decimal Fare { get; set; }

    [Column("status")]
    public PaymentSessionStatus Status { get; set; } = PaymentSessionStatus.PENDING;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// When this session expires (CreatedAt + 10 minutes).
    /// Expired sessions are marked EXPIRED and rejected.
    /// </summary>
    [Column("expires_at")]
    public DateTime ExpiresAt { get; set; } = DateTime.UtcNow.AddMinutes(10);

    public Card? Card { get; set; }
    public User? User { get; set; }
    public Station? OriginStation { get; set; }
    public Station? DestinationStation { get; set; }
}