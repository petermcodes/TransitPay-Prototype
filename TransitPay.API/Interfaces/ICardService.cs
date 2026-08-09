using TransitPay.API.DTOs.Card;
using TransitPay.API.Exceptions;

namespace TransitPay.API.Interfaces;

/// <summary>
/// Service for retrieving and managing Transit Cards.
/// Pure data access and business rules — no authorization logic.
/// The service has no knowledge of JWT claims, user roles, or HTTP responses.
/// Authorization is the controller's responsibility.
/// </summary>
public interface ICardService
{
    /// <summary>
    /// Retrieves the Transit Card for the specified user.
    /// </summary>
    /// <param name="userId">The user ID whose card to retrieve.</param>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    /// <returns>A masked <see cref="CardDto"/>, or null if no active card exists for the user.</returns>
    Task<CardDto?> GetCardByUserIdAsync(int userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a card by its full card number.
    /// </summary>
    /// <param name="cardNumber">The full card number to look up.</param>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    /// <returns>A <see cref="CardDetailsDto"/> with the legacy response shape, or null if not found.</returns>
    Task<CardDetailsDto?> GetCardByNumberAsync(string cardNumber, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new Transit Card and its associated Wallet atomically.
    /// </summary>
    /// <param name="request">The card creation request containing the card number and optional user ID.</param>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    /// <returns>A <see cref="CardCreatedDto"/> representing the newly created card.</returns>
    /// <exception cref="DuplicateCardException">Thrown when a card with the same card number already exists.</exception>
    Task<CardCreatedDto> CreateCardAsync(CardRequestDto request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates a card by its full card number and returns its details with wallet balance.
    /// </summary>
    /// <param name="cardNumber">The full card number to validate.</param>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    /// <returns>A <see cref="CardValidationDto"/> with card and balance info, or null if not found.</returns>
    Task<CardValidationDto?> ValidateCardAsync(string cardNumber, CancellationToken cancellationToken = default);
}