using TransitPay.API.Models;

namespace TransitPay.API.Interfaces;

/// <summary>
/// Service for managing trip lifecycle (start, end, cancel, query).
/// Enforces business rules such as "one active trip per driver" and
/// "only active trips may accept payments".
/// </summary>
public interface ITripService
{
    /// <summary>
    /// Starts a new trip for a driver.
    /// Validates that the driver does not already have an active trip.
    /// </summary>
    /// <param name="driverId">The driver's user ID.</param>
    /// <param name="originStationId">The origin station ID.</param>
    /// <param name="finalDestinationStationId">The final destination station ID.</param>
    /// <returns>The created trip with Pending status.</returns>
    /// <exception cref="InvalidOperationException">Thrown when driver already has an active trip.</exception>
    Task<Trip> StartTripAsync(int driverId, int originStationId, int finalDestinationStationId);

    /// <summary>
    /// Ends an active trip by setting the end time and marking it as Completed.
    /// </summary>
    /// <param name="tripId">The trip ID to end.</param>
    /// <returns>The updated trip with Completed status.</returns>
    /// <exception cref="InvalidOperationException">Thrown when trip is not active.</exception>
    Task<Trip> EndTripAsync(int tripId);

    /// <summary>
    /// Retrieves the currently active trip for a driver, if any.
    /// </summary>
    /// <param name="driverId">The driver's user ID.</param>
    /// <returns>The active trip, or null if the driver has no active trip.</returns>
    Task<Trip?> GetActiveTripAsync(int driverId);

    /// <summary>
    /// Cancels an active or pending trip.
    /// Cancelled trips cannot be resumed.
    /// </summary>
    /// <param name="tripId">The trip ID to cancel.</param>
    /// <returns>The cancelled trip.</returns>
    /// <exception cref="InvalidOperationException">Thrown when trip is already completed or cancelled.</exception>
    Task<Trip> CancelTripAsync(int tripId);

    /// <summary>
    /// Retrieves trip history for a driver with pagination.
    /// Returns all trips regardless of status (Active, Completed, Cancelled, Pending).
    /// </summary>
    /// <param name="driverId">The driver's user ID.</param>
    /// <param name="page">Page number (1-based).</param>
    /// <param name="pageSize">Number of items per page (max 100).</param>
    /// <returns>Tuple of trips list and pagination metadata.</returns>
    Task<(List<Trip> Trips, int TotalCount, int Page, int PageSize)> GetTripHistoryAsync(int driverId, int page = 1, int pageSize = 20);
}
