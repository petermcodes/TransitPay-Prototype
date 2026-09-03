using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using TransitPay.API.Enums;

namespace TransitPay.API.Models;

/// <summary>
/// Represents a physical transit card. Each card has one associated wallet and one
/// active QR code, and belongs to a user. Cards are soft-deleted and versionsed with
/// an optimistic concurrency token; the card number is PII and is always masked in
/// API responses (see <see cref="TransitPay.API.Utilities.CardFormatter"/>).
/// </summary>
[Table("cards")]
public class Card
{
    /// <summary>Primary key.</summary>
    [Key]
    [Column("card_id")]
    public int CardId { get; set; }

    /// <summary>The user this card is issued to (null for unregistered cards).</summary>
    [ForeignKey(nameof(User))]
    [Column("user_id")]
    public int? UserId { get; set; }

    /// <summary>The full 16-digit card number. Sensitive PII — never returned unmasked by the API.</summary>
    [Column("card_number")]
    public string CardNumber { get; set; } = string.Empty;

    /// <summary>When the card was issued (UTC).</summary>
    [Column("issue_date")]
    public DateTime IssueDate { get; set; } = DateTime.UtcNow;

    /// <summary>When the card expires (typically 1–5 years after issue).</summary>
    [Column("expiry_date")]
    public DateTime? ExpiryDate { get; set; }

    /// <summary>The card lifecycle status (ACTIVE, INACTIVE, SUSPENDED, EXPIRED).</summary>
    [Column("status")]
    public CardStatus Status { get; set; } = CardStatus.ACTIVE;

    /// <summary>
    /// The passenger type associated with this card (Regular, Student, Senior, etc.).
    /// Used for fare rule lookup to apply the correct fare.
    /// </summary>
    [Column("passenger_type")]
    public PassengerType PassengerType { get; set; } = PassengerType.Passenger;

    /// <summary>When the card record was created (UTC).</summary>
    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>When the card record was last updated.</summary>
    [Column("updated_at")]
    public DateTime? UpdatedAt { get; set; }

    /// <summary>Soft-delete timestamp. Null while the record is live.</summary>
    [Column("deleted_at")]
    public DateTime? DeletedAt { get; set; }

    /// <summary>
    /// EF Core optimistic concurrency token.
    /// Configured via the [ConcurrencyCheck] attribute — automatically included in
    /// UPDATE WHERE clauses to prevent lost updates. Not exposed in DTOs to
    /// preserve the exact legacy serialized response.
    /// </summary>
    [ConcurrencyCheck]
    [Column("row_version")]
    public byte[]? RowVersion { get; set; }

    /// <summary>Navigation property to the owning user.</summary>
    public User? User { get; set; }

    /// <summary>Navigation property to the card's wallet (one-to-one).</summary>
    public Wallet? Wallet { get; set; }

    /// <summary>Navigation property to the card's active QR code.</summary>
    public QRCode? QRCode { get; set; }

    /// <summary>Navigation property to all transactions made with this card.</summary>
    public ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
}