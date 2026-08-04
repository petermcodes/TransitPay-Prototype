using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TransitPay.API.Interfaces;
using TransitPay.API.Models;

namespace TransitPay.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Driver,Admin")]
public class TripController : ControllerBase
{
    private readonly ITripService _tripService;
    private readonly ILogger<TripController> _logger;

    public TripController(ITripService tripService, ILogger<TripController> logger)
    {
        _tripService = tripService;
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

        var driverId = GetUserIdFromClaims();
        if (driverId == null)
        {
            return Unauthorized(new { success = false, message = "Driver not authenticated." });
        }

        try
        {
            var trip = await _tripService.StartTripAsync(
                driverId.Value,
                request.OriginStationId,
                request.FinalDestinationStationId);

            return Ok(new
            {
                success = true,
                message = "Trip started successfully.",
                data = new
                {
                    trip.TripId,
                    trip.DriverId,
                    trip.OriginStationId,
                    trip.FinalDestinationStationId,
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
        try
        {
            var trip = await _tripService.EndTripAsync(tripId);

            return Ok(new
            {
                success = true,
                message = "Trip ended successfully.",
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
        var driverId = GetUserIdFromClaims();
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
                    trip.OriginStationId,
                    OriginStationName = trip.OriginStation?.StationName,
                    trip.FinalDestinationStationId,
                    FinalDestinationStationName = trip.FinalDestinationStation?.StationName,
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
    /// Cancels a trip.
    /// </summary>
    [HttpPost("{tripId}/cancel")]
    public async Task<IActionResult> CancelTrip(int tripId)
    {
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
        var driverId = GetUserIdFromClaims();
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
                    t.OriginStationId,
                    OriginStationName = t.OriginStation?.StationName,
                    t.FinalDestinationStationId,
                    FinalDestinationStationName = t.FinalDestinationStation?.StationName,
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
    /// Extracts the authenticated user's ID from the JWT claims.
    /// </summary>
    private int? GetUserIdFromClaims()
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub")
            ?? User.FindFirstValue("userId");

        if (int.TryParse(userIdClaim, out var userId))
        {
            return userId;
        }

        return null;
    }
}

/// <summary>
/// Request DTO for starting a trip.
/// </summary>
public class StartTripRequest
{
    [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "Origin station ID is required.")]
    public int OriginStationId { get; set; }

    [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "Final destination station ID is required.")]
    public int FinalDestinationStationId { get; set; }
}