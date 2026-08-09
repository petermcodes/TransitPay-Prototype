using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using TransitPay.API.Enums;

namespace TransitPay.API.Models;

[Table("cards")]
public class Card
{
    [Key]
    [Column("card_id")]
    public int CardId { get; set; }

    [ForeignKey(nameof(User))]
    [Column("user_id")]
    public int? UserId { get; set; }

    [Column("card_number")]
    public string CardNumber { get; set; } = string.Empty;

    [Column("issue_date")]
    public DateTime IssueDate { get; set; } = DateTime.UtcNow;

    [Column("expiry_date")]
    public DateTime? ExpiryDate { get; set; }

    [Column("status")]
    public CardStatus Status { get; set; } = CardStatus.ACTIVE;

    /// <summary>
    /// The passenger type associated with this card (Regular, Student, Senior, etc.).
    /// Used for fare rule lookup to apply the correct fare.
    /// </summary>
    [Column("passenger_type")]
    public PassengerType PassengerType { get; set; } = PassengerType.Passenger;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime? UpdatedAt { get; set; }

    [Column("deleted_at")]
    public DateTime? DeletedAt { get; set; }

    /// <summary>
    /// EF Core optimistic concurrency token.
    /// Configured via the [Timestamp] attribute — automatically included in
    /// UPDATE WHERE clauses to prevent lost updates. Not exposed in DTOs to
    /// preserve the exact legacy serialized response.
    /// </summary>
    [Timestamp]
    [Column("row_version")]
    public byte[] RowVersion { get; set; } = [];

    public User? User { get; set; }
    public Wallet? Wallet { get; set; }
    public QRCode? QRCode { get; set; }
    public ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
}