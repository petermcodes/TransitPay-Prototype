using TransitPay.API.Enums;

namespace TransitPay.API.DTOs.Card;

/// <summary>
/// Detailed card representation used by the legacy <c>GET /api/cards/{cardNumber}</c>
/// and <c>POST /api/cards</c> endpoints. Replicates the exact JSON serialization
/// of the <see cref="TransitPay.API.Models.Card"/> entity to preserve backward
/// compatibility with existing consumers. This DTO decouples the service layer
/// from EF Core entities while keeping the response shape byte-for-byte identical.
/// </summary>
public class CardDetailsDto
{
    /// <summary>Gets or sets the unique card identifier.</summary>
    public int CardId { get; set; }

    /// <summary>Gets or sets the user ID the card belongs to, if any.</summary>
    public int? UserId { get; set; }

    /// <summary>
    /// Gets or sets the masked card number (e.g., "411111******1111").
    /// Full card numbers are never exposed via API responses for security compliance.
    /// </summary>
    public string MaskedCardNumber { get; set; } = string.Empty;

    /// <summary>Gets or sets the issue date.</summary>
    public DateTime IssueDate { get; set; }

    /// <summary>Gets or sets the expiry date, if any.</summary>
    public DateTime? ExpiryDate { get; set; }

    /// <summary>Gets or sets the card status enum value (serialized as int, matching the legacy entity).</summary>
    public CardStatus Status { get; set; }

    /// <summary>Gets or sets the passenger type enum value (serialized as int, matching the legacy entity).</summary>
    public PassengerType PassengerType { get; set; }

    /// <summary>Gets or sets the creation timestamp.</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>Gets or sets the last update timestamp, if any.</summary>
    public DateTime? UpdatedAt { get; set; }

    /// <summary>Gets or sets the soft-delete timestamp, if any.</summary>
    public DateTime? DeletedAt { get; set; }

    // NOTE: RowVersion is configured on the Card entity as an EF Core concurrency
    // token via [Timestamp], but is intentionally NOT included in this DTO to
    // preserve the exact legacy serialized response (which never exposed it).
}
