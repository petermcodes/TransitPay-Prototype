using TransitPay.API.DTOs.Payment;

namespace TransitPay.API.Interfaces;

/// <summary>
/// Service for managing payment sessions.
/// A payment session is created when a passenger selects a route and locks the fare.
/// </summary>
public interface IPaymentSessionService
{
    /// <summary>
    /// Creates a new PENDING payment session, or updates an existing PENDING session
    /// if the passenger changes the route before payment.
    /// The fare is locked at creation/update time and used for the session's lifetime.
    /// </summary>
    /// <param name="cardId">The TransitPay card ID.</param>
    /// <param name="userId">The passenger account (User) ID.</param>
    /// <param name="originStationId">The origin bus station.</param>
    /// <param name="destinationStationId">The destination bus station.</param>
    /// <returns>Payment session response with the locked fare.</returns>
    Task<PaymentSessionResponse> CreateOrUpdateSessionAsync(int cardId, int userId, int originStationId, int destinationStationId);

    /// <summary>
    /// Retrieves the active PENDING payment session for a card.
    /// Returns null if no active session exists.
    /// </summary>
    Task<PaymentSessionResponse?> GetActiveSessionAsync(int cardId);

    /// <summary>
    /// Marks a payment session as EXPIRED.
    /// </summary>
    Task ExpireSessionAsync(Guid paymentSessionId);
}