using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TransitPay.API.Data;
using TransitPay.API.Enums;
using TransitPay.API.Interfaces;
using TransitPay.API.Models;
using TransitPay.API.Utilities;

namespace TransitPay.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Driver,Admin")]
public class TripController : ControllerBase
{
    private readonly ITripService _tripService;
    private readonly TransitPayDbContext _dbContext;
    private readonly ILogger<TripController> _logger;

    public TripController(ITripService tripService, TransitPayDbContext dbContext, ILogger<TripController> logger)
    {
        _tripService = tripService;
        _dbContext = dbContext;
        _logger = logger;
    }

    /// <summary>
    /// Starts a new trip for the authenticated driver.
    /// </summary>
    [HttpPost("start")]
    public async Task<IActionResult> StartTrip([FromBody] StartTripRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new { success = false, message = "Validation failed.", errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage) });
        }

        var driverId = User.GetAuthenticatedUserId();
        if (driverId == null)
        {
            return Unauthorized(new { success = false, message = "Driver not authenticated." });
        }

        try
        {
            var trip = await _tripService.StartTripAsync(
                driverId.Value,
                request.OriginTerminalId,
                request.FinalDestinationTerminalId);

            return Ok(new
            {
                success = true,
                message = "Trip started successfully.",
                data = new
                {
                    trip.TripId,
                    trip.DriverId,
                    trip.OriginTerminalId,
                    trip.FinalDestinationTerminalId,
                    trip.CurrentBoardingOriginTerminalId,
                    trip.RouteName,
                    trip.TripStatus,
                    trip.StartedAt,
                    trip.CreatedAt
                }
            });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning("Failed to start trip: {Message}", ex.Message);
            return BadRequest(new { success = false, message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting trip for driver {DriverId}", driverId);
            return StatusCode(500, new { success = false, message = "An error occurred while starting the trip." });
        }
    }

    /// <summary>
    /// Ends an active trip.
    /// </summary>
    [HttpPost("{tripId}/end")]
    public async Task<IActionResult> EndTrip(int tripId)
    {
        // Ownership validation: the trip must belong to the authenticated driver (or Admin)
        if (!await CanManageTripAsync(tripId))
        {
            return NotFound(new { success = false, message = "Trip not found." });
        }

        try
        {
            var trip = await _tripService.EndTripAsync(tripId);

            return Ok(new
            {
                success = true,
                message = "Current boarding origin updated successfully.",
                data = new
                {
                    trip.TripId,
                    trip.CurrentBoardingOriginTerminalId,
                    trip.BoardingOriginUpdatedAt,
                    trip.UpdatedAt
                }
            });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning("Failed to end trip {TripId}: {Message}", tripId, ex.Message);
            return BadRequest(new { success = false, message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error ending trip {TripId}", tripId);
            return StatusCode(500, new { success = false, message = "An error occurred while ending the trip." });
        }
    }

    /// <summary>
    /// Retrieves the active trip for the authenticated driver.
    /// </summary>
    [HttpGet("active")]
    public async Task<IActionResult> GetActiveTrip()
    {
        var driverId = User.GetAuthenticatedUserId();
        if (driverId == null)
        {
            return Unauthorized(new { success = false, message = "Driver not authenticated." });
        }

        try
        {
            var trip = await _tripService.GetActiveTripAsync(driverId.Value);

            if (trip == null)
            {
                return Ok(new { success = true, message = "No active trip found.", data = (Trip?)null });
            }

            return Ok(new
            {
                success = true,
                message = "Active trip retrieved successfully.",
                data = new
                {
                    trip.TripId,
                    trip.DriverId,
                    trip.OriginTerminalId,
                    OriginTerminalName = trip.OriginTerminal?.TerminalName,
                    trip.FinalDestinationTerminalId,
                    FinalDestinationTerminalName = trip.FinalDestinationTerminal?.TerminalName,
                    trip.CurrentBoardingOriginTerminalId,
                    CurrentBoardingOriginTerminalName = trip.CurrentBoardingOriginTerminal?.TerminalName,
                    trip.BoardingOriginUpdatedAt,
                    trip.RouteName,
                    trip.TripStatus,
                    trip.StartedAt,
                    trip.PassengerCount,
                    trip.TotalRevenue
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving active trip for driver {DriverId}", driverId);
            return StatusCode(500, new { success = false, message = "An error occurred while retrieving the active trip." });
        }
    }

    /// <summary>
    /// Updates the current boarding origin for an active trip.
    /// The conductor changes this only when passengers begin boarding at a different station.
    /// </summary>
    [HttpPut("{tripId}/boarding-origin")]
    public async Task<IActionResult> UpdateBoardingOrigin(int tripId, [FromBody] UpdateBoardingOriginRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new { success = false, message = "Validation failed.", errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage) });
        }

        // Ownership validation: the trip must belong to the authenticated driver (or Admin)
        if (!await CanManageTripAsync(tripId))
        {
            return NotFound(new { success = false, message = "Trip not found." });
        }

        try
        {
            var trip = await _tripService.UpdateCurrentBoardingOriginAsync(tripId, request.OriginTerminalId);

            return Ok(new
            {
                success = true,
                message = "Current boarding origin updated successfully.",
                data = new
                {
                    trip.TripId,
                    trip.CurrentBoardingOriginTerminalId,
                    trip.BoardingOriginUpdatedAt,
                    trip.UpdatedAt
                }
            });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning("Failed to update boarding origin for trip {TripId}: {Message}", tripId, ex.Message);
            return BadRequest(new { success = false, message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating boarding origin for trip {TripId}", tripId);
            return StatusCode(500, new { success = false, message = "An error occurred while updating the boarding origin." });
        }
    }

    /// <summary>
    /// Cancels a trip.
    /// </summary>
    [HttpPost("{tripId}/cancel")]
    public async Task<IActionResult> CancelTrip(int tripId)
    {
        // Ownership validation: the trip must belong to the authenticated driver (or Admin)
        if (!await CanManageTripAsync(tripId))
        {
            return NotFound(new { success = false, message = "Trip not found." });
        }

        try
        {
            var trip = await _tripService.CancelTripAsync(tripId);

            return Ok(new
            {
                success = true,
                message = "Trip cancelled successfully.",
                data = new
                {
                    trip.TripId,
                    trip.TripStatus,
                    trip.EndedAt,
                    trip.UpdatedAt
                }
            });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning("Failed to cancel trip {TripId}: {Message}", tripId, ex.Message);
            return BadRequest(new { success = false, message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling trip {TripId}", tripId);
            return StatusCode(500, new { success = false, message = "An error occurred while cancelling the trip." });
        }
    }

    /// <summary>
    /// Retrieves trip history for the authenticated driver with pagination.
    /// </summary>
    [HttpGet("history")]
    public async Task<IActionResult> GetTripHistory([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var driverId = User.GetAuthenticatedUserId();
        if (driverId == null)
        {
            return Unauthorized(new { success = false, message = "Driver not authenticated." });
        }

        try
        {
            // Validate pagination parameters
            page = Math.Max(1, page);
            pageSize = Math.Clamp(pageSize, 1, 100);

            var (trips, totalCount, actualPage, actualPageSize) = await _tripService.GetTripHistoryAsync(
                driverId.Value, page, pageSize);

            var totalPages = (int)Math.Ceiling(totalCount / (double)actualPageSize);

            return Ok(new
            {
                success = true,
                message = "Trip history retrieved successfully.",
                data = trips.Select(t => new
                {
                    t.TripId,
                    t.DriverId,
                    t.OriginTerminalId,
                    OriginTerminalName = t.OriginTerminal?.TerminalName,
                    t.FinalDestinationTerminalId,
                    FinalDestinationTerminalName = t.FinalDestinationTerminal?.TerminalName,
                    t.RouteName,
                    t.TripStatus,
                    t.StartedAt,
                    t.EndedAt,
                    t.PassengerCount,
                    t.TotalRevenue,
                    t.CreatedAt
                }),
                pagination = new
                {
                    page = actualPage,
                    pageSize = actualPageSize,
                    totalCount,
                    totalPages
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving trip history for driver {DriverId}", driverId);
            return StatusCode(500, new { success = false, message = "An error occurred while retrieving trip history." });
        }
    }

    /// <summary>
    /// Determines whether the authenticated user can manage a specific trip.
    /// Drivers may manage only their own trips. Admins may manage any trip.
    /// </summary>
    private async Task<bool> CanManageTripAsync(int tripId)
    {
        var userId = User.GetAuthenticatedUserId();
        if (userId == null)
        {
            return false;
        }

        var isAdmin = User.IsInRole(nameof(RoleName.Admin));

        if (isAdmin)
        {
            return true;
        }

        var trip = await _dbContext.Trips
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.TripId == tripId);

        return trip != null && trip.DriverId == userId.Value;
    }
}

/// <summary>
/// Request DTO for starting a trip.
/// Origin and destination are optional — a trip can be started immediately
/// and the driver can select them afterward for scanning.
/// </summary>
public class StartTripRequest
{
    public int? OriginTerminalId { get; set; }

    public int? FinalDestinationTerminalId { get; set; }
}

/// <summary>
/// Request DTO for updating the current boarding origin of an active trip.
/// </summary>
public class UpdateBoardingOriginRequest
{
    [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "Origin terminal ID is required.")]
    [System.ComponentModel.DataAnnotations.Range(1, int.MaxValue, ErrorMessage = "Invalid origin terminal ID.")]
    public int OriginTerminalId { get; set; }
}
