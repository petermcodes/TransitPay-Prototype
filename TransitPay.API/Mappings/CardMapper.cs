using TransitPay.API.DTOs.Card;
using TransitPay.API.Models;
using TransitPay.API.Utilities;

namespace TransitPay.API.Mappings;

/// <summary>
/// Centralized mapping layer for Card entity → DTO conversions.
/// AutoMapper is intentionally not used in this solution; this static mapper
/// provides a single source of truth for all Card → DTO mappings, avoiding
/// duplicate mapping logic throughout the service layer.
/// </summary>
public static class CardMapper
{
    /// <summary>
    /// Maps a <see cref="Card"/> entity to a safe, masked <see cref="CardDto"/>.
    /// Used by the <c>GET /api/cards/me</c> and <c>GET /api/cards/user/{userId}</c>
    /// endpoints. The full card number is never exposed through this DTO.
    /// </summary>
    /// <param name="card">The card entity to map.</param>
    /// <returns>A masked <see cref="CardDto"/>.</returns>
    public static CardDto ToDto(Card card)
    {
        return new CardDto
        {
            CardId = card.CardId,
            MaskedCardNumber = CardFormatter.MaskCardNumber(card.CardNumber) ?? string.Empty,
            Status = card.Status.ToString(),
            PassengerType = card.PassengerType.ToString(),
            IssueDate = card.IssueDate,
            ExpiryDate = card.ExpiryDate
        };
    }

    /// <summary>
    /// Maps a <see cref="Card"/> entity to a <see cref="CardDetailsDto"/>.
    /// Used by the legacy <c>GET /api/cards/{cardNumber}</c> endpoint.
    /// Replicates the exact JSON serialization of the entity (full card number,
    /// int enum values, row version) to preserve backward compatibility.
    /// </summary>
    /// <param name="card">The card entity to map.</param>
    /// <returns>A <see cref="CardDetailsDto"/> with the legacy response shape.</returns>
    public static CardDetailsDto ToDetailsDto(Card card)
    {
        return new CardDetailsDto
        {
            CardId = card.CardId,
            UserId = card.UserId,
            MaskedCardNumber = CardFormatter.MaskCardNumber(card.CardNumber) ?? string.Empty,
            IssueDate = card.IssueDate,
            ExpiryDate = card.ExpiryDate,
            Status = card.Status,
            PassengerType = card.PassengerType,
            CreatedAt = card.CreatedAt,
            UpdatedAt = card.UpdatedAt,
            DeletedAt = card.DeletedAt
        };
    }

    /// <summary>
    /// Maps a <see cref="Card"/> entity to a <see cref="CardCreatedDto"/>.
    /// Used by the <c>POST /api/cards</c> endpoint.
    /// Replicates the exact JSON serialization of the created entity to preserve
    /// backward compatibility. Semantically distinct from <see cref="ToDetailsDto"/>
    /// to represent a creation response.
    /// </summary>
    /// <param name="card">The card entity to map.</param>
    /// <returns>A <see cref="CardCreatedDto"/> with the legacy response shape.</returns>
    public static CardCreatedDto ToCreatedDto(Card card)
    {
        return new CardCreatedDto
        {
            CardId = card.CardId,
            UserId = card.UserId,
            MaskedCardNumber = CardFormatter.MaskCardNumber(card.CardNumber) ?? string.Empty,
            IssueDate = card.IssueDate,
            ExpiryDate = card.ExpiryDate,
            Status = card.Status,
            PassengerType = card.PassengerType,
            CreatedAt = card.CreatedAt,
            UpdatedAt = card.UpdatedAt,
            DeletedAt = card.DeletedAt
        };
    }
}
