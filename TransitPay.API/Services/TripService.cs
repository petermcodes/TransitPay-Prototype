using Microsoft.EntityFrameworkCore;
using TransitPay.API.Data;
using TransitPay.API.Enums;
using TransitPay.API.Interfaces;
using TransitPay.API.Models;

namespace TransitPay.API.Services;

/// <summary>
/// Service for managing trip lifecycle.
/// Enforces business rules:
/// - Starting a trip immediately creates an ACTIVE trip (no Pending state)
/// - Driver cannot start a second active trip
/// - Current boarding origin is initialized to the trip origin and persisted
/// - Ending a trip stores EndTime
/// - Cancelled trips cannot be resumed
/// - Only ACTIVE trips may accept payments
/// </summary>
public class TripService : ITripService
{
    private readonly TransitPayDbContext _dbContext;
    private readonly ILogger<TripService> _logger;

    /// <summary>
    /// Creates a new TripService.
    /// </summary>
    public TripService(TransitPayDbContext dbContext, ILogger<TripService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<Trip> StartTripAsync(int driverId, int? originTerminalId, int? finalDestinationTerminalId)
    {
        try
        {
            _logger.LogInformation("Starting trip for driver {DriverId}", driverId);

            // Business Rule: Driver cannot start a second active trip
            // The database has a filtered unique index on driver_id WHERE trip_status = 1 (Active)
            // But we check here to provide a better error message before hitting the DB constraint
            var hasActiveTrip = await _dbContext.Trips
                .AnyAsync(t => t.DriverId == driverId && t.TripStatus == TripStatus.Active);

            if (hasActiveTrip)
            {
                _logger.LogWarning("Driver {DriverId} attempted to start a trip while already having an active trip", driverId);
                throw new InvalidOperationException("You already have an active trip. Please end or cancel the current trip before starting a new one.");
            }

            // Origin and destination are optional — a trip can be started immediately
            // and the driver selects them afterward for scanning.
            string routeName = "Trip";
            int? currentBoardingOriginTerminalId = null;

            if (originTerminalId.HasValue && finalDestinationTerminalId.HasValue)
            {
                // Get terminal names for route name
                var originTerminal = await _dbContext.Terminals.FindAsync(originTerminalId.Value);
                var destinationTerminal = await _dbContext.Terminals.FindAsync(finalDestinationTerminalId.Value);

                if (originTerminal == null || destinationTerminal == null)
                {
                    throw new InvalidOperationException("Invalid origin or destination terminal.");
                }

                routeName = $"{originTerminal.TerminalName} → {destinationTerminal.TerminalName}";
                currentBoardingOriginTerminalId = originTerminalId.Value;
            }

            // Business Rule: Starting a trip immediately creates an ACTIVE trip.
            // The current boarding origin is initialized to the trip's origin terminal when provided.
            var trip = new Trip
            {
                DriverId = driverId,
                OriginTerminalId = originTerminalId,
                FinalDestinationTerminalId = finalDestinationTerminalId,
                CurrentBoardingOriginTerminalId = currentBoardingOriginTerminalId,
                RouteName = routeName,
                TripStatus = TripStatus.Active,
                StartedAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow
            };

            _dbContext.Trips.Add(trip);
            await _dbContext.SaveChangesAsync();

            _logger.LogInformation("Trip {TripId} started successfully for driver {DriverId}", trip.TripId, driverId);

            return trip;
        }
        catch (DbUpdateConcurrencyException ex)
        {
            _logger.LogWarning(ex, "Trip start conflicted for driver {DriverId}", driverId);
            throw new InvalidOperationException("The trip record was changed by another request. Please retry.", ex);
        }
    }

    /// <inheritdoc />
    public async Task<Trip> EndTripAsync(int tripId)
    {
        try
        {
            _logger.LogInformation("Ending trip {TripId}", tripId);

            var trip = await _dbContext.Trips.FindAsync(tripId);
            if (trip == null)
            {
                throw new InvalidOperationException("Trip not found.");
            }

            // Business Rule: Only active trips can be ended
            if (trip.TripStatus != TripStatus.Active)
            {
                _logger.LogWarning("Attempted to end trip {TripId} with status {Status}", tripId, trip.TripStatus);
                throw new InvalidOperationException($"Cannot end a trip with status '{trip.TripStatus}'. Only active trips can be ended.");
            }

            // Business Rule: Ending a Trip stores EndTime
            trip.TripStatus = TripStatus.Completed;
            trip.EndedAt = DateTime.UtcNow;
            trip.UpdatedAt = DateTime.UtcNow;

            await _dbContext.SaveChangesAsync();

            _logger.LogInformation("Trip {TripId} ended successfully", tripId);

            return trip;
        }
        catch (DbUpdateConcurrencyException ex)
        {
            _logger.LogWarning(ex, "Trip end conflicted for trip {TripId}", tripId);
            throw new InvalidOperationException("The trip record was changed by another request. Please retry.", ex);
        }
    }

    /// <inheritdoc />
    public async Task<Trip?> GetActiveTripAsync(int driverId)
    {
        _logger.LogInformation("Retrieving active trip for driver {DriverId}", driverId);

        // Return the driver's active trip (if any), including the current boarding origin.
        // This is the source of truth for resuming an unfinished trip after an app restart.
        var activeTrip = await _dbContext.Trips
            .Include(t => t.OriginTerminal)
            .Include(t => t.FinalDestinationTerminal)
            .Include(t => t.CurrentBoardingOriginTerminal)
            .FirstOrDefaultAsync(t => t.DriverId == driverId && t.TripStatus == TripStatus.Active);

        return activeTrip;
    }

    /// <inheritdoc />
    public async Task<Trip> UpdateCurrentBoardingOriginAsync(int tripId, int newOriginTerminalId)
    {
        try
        {
            _logger.LogInformation("Updating current boarding origin for trip {TripId} to terminal {NewOriginTerminalId}",
                tripId, newOriginTerminalId);

            var trip = await _dbContext.Trips.FindAsync(tripId);
            if (trip == null)
            {
                throw new InvalidOperationException("Trip not found.");
            }

            // Business Rule: Only active trips may change the boarding origin
            if (trip.TripStatus != TripStatus.Active)
            {
                _logger.LogWarning("Attempted to update boarding origin for trip {TripId} with status {Status}",
                    tripId, trip.TripStatus);
                throw new InvalidOperationException($"Cannot update the boarding origin for a trip with status '{trip.TripStatus}'. Only active trips can be updated.");
            }

            // Validate the new origin terminal exists and is active
            var terminal = await _dbContext.Terminals
                .FirstOrDefaultAsync(t => t.TerminalId == newOriginTerminalId && t.IsActive);

            if (terminal == null)
            {
                throw new InvalidOperationException("Invalid or inactive origin terminal.");
            }

            // Business Rule: Only update when the conductor explicitly changes the origin.
            // This method is the single code path for both manual conductor overrides and
            // future GPS/geofencing auto-updates — no payment workflow redesign is needed.
            trip.CurrentBoardingOriginTerminalId = newOriginTerminalId;
            trip.BoardingOriginUpdatedAt = DateTime.UtcNow;
            trip.UpdatedAt = DateTime.UtcNow;

            await _dbContext.SaveChangesAsync();

            _logger.LogInformation("Current boarding origin for trip {TripId} updated to terminal {NewOriginTerminalId}",
                tripId, newOriginTerminalId);

            return trip;
        }
        catch (DbUpdateConcurrencyException ex)
        {
            _logger.LogWarning(ex, "Boarding origin update conflicted for trip {TripId}", tripId);
            throw new InvalidOperationException("The trip record was changed by another request. Please retry.", ex);
        }
    }

    /// <inheritdoc />
    public async Task<Trip> CancelTripAsync(int tripId)
    {
        try
        {
            _logger.LogInformation("Cancelling trip {TripId}", tripId);

            var trip = await _dbContext.Trips.FindAsync(tripId);
            if (trip == null)
            {
                throw new InvalidOperationException("Trip not found.");
            }

            // Business Rule: Cancelled Trips cannot be resumed
            // Only allow cancelling Active or Pending trips
            if (trip.TripStatus == TripStatus.Completed)
            {
                _logger.LogWarning("Attempted to cancel completed trip {TripId}", tripId);
                throw new InvalidOperationException("Cannot cancel a completed trip.");
            }

            if (trip.TripStatus == TripStatus.Cancelled)
            {
                _logger.LogWarning("Attempted to cancel already cancelled trip {TripId}", tripId);
                throw new InvalidOperationException("Trip is already cancelled.");
            }

            // Business Rule: Cancelled Trips cannot be resumed
            trip.TripStatus = TripStatus.Cancelled;
            trip.EndedAt = DateTime.UtcNow;
            trip.UpdatedAt = DateTime.UtcNow;

            await _dbContext.SaveChangesAsync();

            _logger.LogInformation("Trip {TripId} cancelled successfully", tripId);

            return trip;
        }
        catch (DbUpdateConcurrencyException ex)
        {
            _logger.LogWarning(ex, "Trip cancel conflicted for trip {TripId}", tripId);
            throw new InvalidOperationException("The trip record was changed by another request. Please retry.", ex);
        }
    }

    /// <inheritdoc />
    public async Task<(List<Trip> Trips, int TotalCount, int Page, int PageSize)> GetTripHistoryAsync(int driverId, int page = 1, int pageSize = 20)
    {
        _logger.LogInformation("Retrieving trip history for driver {DriverId}, page {Page}, pageSize {PageSize}", driverId, page, pageSize);

        // Validate and clamp pagination parameters
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        // Query all trips for the driver regardless of status
        var query = _dbContext.Trips
            .Include(t => t.OriginTerminal)
            .Include(t => t.FinalDestinationTerminal)
            .Where(t => t.DriverId == driverId);

        // Get total count before pagination
        var totalCount = await query.CountAsync();

        // Apply pagination and ordering (most recent first)
        var trips = await query
            .OrderByDescending(t => t.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        _logger.LogInformation("Retrieved {Count} trips for driver {DriverId} (page {Page} of {TotalPages})",
            trips.Count, driverId, page, (int)Math.Ceiling(totalCount / (double)pageSize));

        return (trips, totalCount, page, pageSize);
    }
}