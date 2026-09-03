using TransitPay.API.DTOs.TripPlan;
using TransitPay.API.Models;

namespace TransitPay.API.Interfaces;

/// <summary>
/// Service for managing passenger Trip Plans.
/// A Trip Plan represents a single passenger journey: it locks the fare for a given
/// origin → destination route onto a card, stores the full fare breakdown at creation
/// time, and expires 24 hours after it is created or updated.
/// This service is the single source of truth for creating, updating, cancelling, and
/// querying the plan that the conductor payment flow consumes as its destination.
/// </summary>
public interface ITripPlanService
{
    /// <summary>
    /// Creates a new ACTIVE trip plan for the passenger's card.
    /// Any existing ACTIVE plan for the same user + card is cancelled first, so a
    /// passenger can only ever hold one active plan per card. The fare is calculated
    /// up-front via <see cref="TransitPay.API.Services.FareCalculator"/> and snapshotted
    /// onto the plan so the conductor charge always matches what the passenger was quoted.
    /// </summary>
    /// <param name="userId">The authenticated passenger's user ID.</param>
    /// <param name="cardId">The transit card ID the plan is created for; the card must belong to the user.</param>
    /// <param name="originTerminalId">The planned boarding terminal ID.</param>
    /// <param name="destinationTerminalId">The planned alighting terminal ID.</param>
    /// <returns>The created plan with status "Active" and a 24-hour expiry.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the card does not belong to the user.</exception>
    Task<TripPlan> CreateTripPlanAsync(int userId, int cardId, int originTerminalId, int destinationTerminalId);

    /// <summary>
    /// Retrieves the current ACTIVE trip plan for a user's card, if any.
    /// Includes the origin/destination terminal navigation properties.
    /// </summary>
    /// <param name="userId">The passenger's user ID.</param>
    /// <param name="cardId">The transit card ID.</param>
    /// <returns>The active plan, or <c>null</c> when the user/card has no active plan.</returns>
    Task<TripPlan?> GetActiveTripPlanAsync(int userId, int cardId);

    /// <summary>
    /// Retrieves a specific trip plan by ID, scoped to the given user + card so
    /// passengers can only ever read their own plans.
    /// </summary>
    /// <param name="planId">The plan ID to load.</param>
    /// <param name="userId">The passenger's user ID (ownership scope).</param>
    /// <param name="cardId">The transit card ID (ownership scope).</param>
    /// <returns>The matching plan (any status), or <c>null</c> if not found or not owned.</returns>
    Task<TripPlan?> GetTripPlanByIdAsync(int planId, int userId, int cardId);

    /// <summary>
    /// Retrieves the full plan history for a user's card, newest first.
    /// Returns plans in every status (Active, Cancelled, Used).
    /// </summary>
    /// <param name="userId">The passenger's user ID.</param>
    /// <param name="cardId">The transit card ID.</param>
    /// <returns>All plans for the card ordered by creation time descending.</returns>
    Task<IEnumerable<TripPlan>> GetTripPlanHistoryAsync(int userId, int cardId);

    /// <summary>
    /// Changes the destination of an ACTIVE plan and re-calculates the fare for the
    /// new route so the stored fare breakdown stays in sync. The 24-hour expiry
    /// window is reset on update.
    /// </summary>
    /// <param name="planId">The ID of the active plan to update.</param>
    /// <param name="newDestinationTerminalId">The new alighting terminal ID.</param>
    /// <returns>The updated plan, or <c>null</c> when no active plan exists with that ID.</returns>
    Task<TripPlan?> UpdateTripPlanDestinationAsync(int planId, int newDestinationTerminalId);

    /// <summary>
    /// Cancels an ACTIVE trip plan (status → "Cancelled").
    /// Used when the passenger discards the plan or creates a replacement.
    /// </summary>
    /// <param name="planId">The ID of the active plan to cancel.</param>
    /// <returns><c>true</c> when the plan was found and cancelled; <c>false</c> when no active plan exists.</returns>
    Task<bool> CancelTripPlanAsync(int planId);
}
