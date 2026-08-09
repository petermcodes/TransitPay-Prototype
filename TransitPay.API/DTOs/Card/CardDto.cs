namespace TransitPay.API.DTOs.Card;

/// <summary>
/// A safe, masked representation of a Transit Card returned to authenticated
/// users by the <c>GET /api/cards/me</c> and <c>GET /api/cards/user/{userId}</c>
/// endpoints. Deliberately excludes the full card number, wallet balance,
/// and any personal information.
/// </summary>
public class CardDto
{
    /// <summary>Gets or sets the unique card identifier.</summary>
    public int CardId { get; set; }

    /// <summary>
    /// Gets or sets the masked card number (e.g., "•••• 4821").
    /// The full card number is never exposed through this DTO.
    /// </summary>
    public string MaskedCardNumber { get; set; } = string.Empty;

    /// <summary>Gets or sets the card status as a string (e.g., "ACTIVE", "SUSPENDED").</summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>Gets or sets the passenger type as a string (e.g., "Passenger", "Student").</summary>
    public string PassengerType { get; set; } = string.Empty;

    /// <summary>Gets or sets the date the card was issued.</summary>
    public DateTime IssueDate { get; set; }

    /// <summary>Gets or sets the card expiry date, if any.</summary>
    public DateTime? ExpiryDate { get; set; }
}