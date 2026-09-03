using TransitPay.API.Models;

namespace TransitPay.API.Interfaces;

/// <summary>
/// Service for managing trip lifecycle (start, end, cancel, query, boarding origin).
/// Enforces business rules such as "one active trip per driver" and
/// "only active trips may accept payments".
/// </summary>
public interface ITripService
{
    /// <summary>
    /// Starts a new trip for a driver.
    /// The trip is immediately created as ACTIVE (no Pending state).
    /// The current boarding origin is initialized to the trip's origin station.
    /// Validates that the driver does not already have an active trip.
    /// </summary>
    /// <param name="driverId">The driver's user ID.</param>
    /// <param name="originTerminalId">The origin terminal ID.</param>
    /// <param name="finalDestinationTerminalId">The final destination terminal ID.</param>
    /// <returns>The created trip with Active status.</returns>
    /// <exception cref="InvalidOperationException">Thrown when driver already has an active trip.</exception>
    Task<Trip> StartTripAsync(int driverId, int? originTerminalId, int? finalDestinationTerminalId);

    /// <summary>
    /// Ends an active trip by setting the end time and marking it as Completed.
    /// </summary>
    /// <param name="tripId">The trip ID to end.</param>
    /// <returns>The updated trip with Completed status.</returns>
    /// <exception cref="InvalidOperationException">Thrown when trip is not active.</exception>
    Task<Trip> EndTripAsync(int tripId);

    /// <summary>
    /// Retrieves the currently active trip for a driver, if any.
    /// Includes the current boarding origin station.
    /// This is the source of truth for resuming an unfinished trip after an app restart.
    /// </summary>
    /// <param name="driverId">The driver's user ID.</param>
    /// <returns>The active trip, or null if the driver has no active trip.</returns>
    Task<Trip?> GetActiveTripAsync(int driverId);

    /// <summary>
    /// Updates the current boarding origin for an active trip.
    /// Only active trips may be updated.
    /// This is the single code path for both manual conductor overrides and
    /// future GPS/geofencing auto-updates.
    /// </summary>
    /// <param name="tripId">The trip ID to update.</param>
    /// <param name="newOriginStationId">The new boarding origin station ID.</param>
    /// <returns>The updated trip.</returns>
    /// <exception cref="InvalidOperationException">Thrown when trip is not found, not active, or station is invalid/inactive.</exception>
    Task<Trip> UpdateCurrentBoardingOriginAsync(int tripId, int newOriginStationId);

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