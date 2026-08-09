using TransitPay.API.Enums;

namespace TransitPay.API.DTOs.Card;

/// <summary>
/// Response DTO for the <c>GET /api/cards/validate/{cardNumber}</c> endpoint.
/// Replicates the exact JSON projection returned by the legacy endpoint:
/// <c>{ cardId, cardNumber, status, balance }</c>. The wallet balance is
/// included because this endpoint is used by drivers to validate a physical
/// card before processing a fare.
/// </summary>
public class CardValidationDto
{
    /// <summary>Gets or sets the unique card identifier.</summary>
    public int CardId { get; set; }

    /// <summary>Gets or sets the masked card number (e.g., "411111******1111").</summary>
    public string MaskedCardNumber { get; set; } = string.Empty;

    /// <summary>Gets or sets the card status enum value (serialized as int, matching the legacy projection).</summary>
    public CardStatus Status { get; set; }

    /// <summary>Gets or sets the current wallet balance for the card.</summary>
    public decimal Balance { get; set; }
}