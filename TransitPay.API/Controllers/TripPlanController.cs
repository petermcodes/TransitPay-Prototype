using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TransitPay.API.Data;
using TransitPay.API.DTOs.TripPlan;
using TransitPay.API.Enums;
using TransitPay.API.Interfaces;
using TransitPay.API.Models;
using TransitPay.API.Utilities;

namespace TransitPay.API.Controllers;

/// <summary>
/// Trip Plan endpoints for the passenger app: create an active plan (locking the fare),
/// query the active plan, change the destination, cancel, and browse plan history.
/// All endpoints resolve the authenticated user's card from JWT claims.
/// </summary>
[ApiController]
[Route("api/trip-plan")]
[Authorize]
public class TripPlanController : ControllerBase
{
    private readonly ITripPlanService _tripPlanService;
    private readonly ILogger<TripPlanController> _logger;
    private readonly TransitPayDbContext _dbContext;

    /// <summary>
    /// Creates a new TripPlanController.
    /// </summary>
    public TripPlanController(ITripPlanService tripPlanService, ILogger<TripPlanController> logger, TransitPayDbContext dbContext)
    {
        _tripPlanService = tripPlanService;
        _logger = logger;
        _dbContext = dbContext;
    }

    private (int UserId, int CardId) GetUserAndCardIdFromClaims()
    {
        var userId = User.GetAuthenticatedUserId();
        if (userId == null)
        {
            throw new InvalidOperationException("User not authenticated.");
        }

        // Query the database for the user's active card
        var card = _dbContext.Cards
            .FirstOrDefault(c => c.UserId == userId && c.Status == CardStatus.ACTIVE && c.DeletedAt == null);

        if (card == null)
        {
            throw new InvalidOperationException("No active card found for user. Please link a transit card first.");
        }

        return (userId.Value, card.CardId);
    }

    /// <summary>
    /// Creates a new trip plan for the authenticated passenger.
    /// If an active plan exists, it will be cancelled and replaced.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreateTripPlan([FromBody] CreateTripPlanRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new { success = false, message = "Validation failed.", errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage) });
        }

        try
        {
            var (userId, cardId) = GetUserAndCardIdFromClaims();
            var plan = await _tripPlanService.CreateTripPlanAsync(userId, cardId, request.OriginTerminalId, request.DestinationTerminalId);

            var response = new TripPlanResponse
            {
                PlanId = plan.PlanId,
                CardId = plan.CardId,
                OriginTerminalId = plan.OriginTerminalId,
                OriginTerminalName = plan.OriginTerminal?.TerminalName ?? string.Empty,
                DestinationTerminalId = plan.DestinationTerminalId,
                DestinationTerminalName = plan.DestinationTerminal?.TerminalName ?? string.Empty,
                Status = plan.Status,
                CreatedAt = plan.CreatedAt,
                ExpiresAt = plan.ExpiresAt,
                UsedAt = plan.UsedAt,
                NormalFare = plan.NormalFare,
                DiscountAmount = plan.DiscountAmount,
                DiscountPercentage = plan.DiscountPercentage,
                FinalFarePrice = plan.FinalFarePrice
            };

            return Ok(new { success = true, data = response });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning("Failed to create trip plan: {Message}", ex.Message);
            return BadRequest(new { success = false, message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating trip plan");
            return StatusCode(500, new { success = false, message = "An error occurred while creating the trip plan." });
        }
    }

    /// <summary>
    /// Gets the authenticated passenger's active trip plan.
    /// </summary>
    [HttpGet("active")]
    public async Task<IActionResult> GetActiveTripPlan()
    {
        try
        {
            var (userId, cardId) = GetUserAndCardIdFromClaims();
            var plan = await _tripPlanService.GetActiveTripPlanAsync(userId, cardId);

            if (plan == null)
            {
                return NotFound(new { success = false, message = "No active trip plan found." });
            }

            var response = new TripPlanResponse
            {
                PlanId = plan.PlanId,
                CardId = plan.CardId,
                OriginTerminalId = plan.OriginTerminalId,
                OriginTerminalName = plan.OriginTerminal?.TerminalName ?? string.Empty,
                DestinationTerminalId = plan.DestinationTerminalId,
                DestinationTerminalName = plan.DestinationTerminal?.TerminalName ?? string.Empty,
                Status = plan.Status,
                CreatedAt = plan.CreatedAt,
                ExpiresAt = plan.ExpiresAt,
                UsedAt = plan.UsedAt,
                NormalFare = plan.NormalFare,
                DiscountAmount = plan.DiscountAmount,
                DiscountPercentage = plan.DiscountPercentage,
                FinalFarePrice = plan.FinalFarePrice
            };

            return Ok(new { success = true, data = response });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting active trip plan");
            return StatusCode(500, new { success = false, message = "An error occurred while retrieving the trip plan." });
        }
    }

    /// <summary>
    /// Updates the destination of an active trip plan (change of mind).
    /// </summary>
    [HttpPut("{planId}")]
    public async Task<IActionResult> UpdateTripPlanDestination(int planId, [FromBody] UpdateTripPlanDestinationRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new { success = false, message = "Validation failed.", errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage) });
        }

        try
        {
            var plan = await _tripPlanService.UpdateTripPlanDestinationAsync(planId, request.NewDestinationTerminalId);

            if (plan == null)
            {
                return NotFound(new { success = false, message = "Trip plan not found or no longer active." });
            }

            var response = new TripPlanResponse
            {
                PlanId = plan.PlanId,
                CardId = plan.CardId,
                OriginTerminalId = plan.OriginTerminalId,
                OriginTerminalName = plan.OriginTerminal?.TerminalName ?? string.Empty,
                DestinationTerminalId = plan.DestinationTerminalId,
                DestinationTerminalName = plan.DestinationTerminal?.TerminalName ?? string.Empty,
                Status = plan.Status,
                CreatedAt = plan.CreatedAt,
                ExpiresAt = plan.ExpiresAt,
                UsedAt = plan.UsedAt,
                NormalFare = plan.NormalFare,
                DiscountAmount = plan.DiscountAmount,
                DiscountPercentage = plan.DiscountPercentage,
                FinalFarePrice = plan.FinalFarePrice
            };

            return Ok(new { success = true, data = response });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating trip plan destination");
            return StatusCode(500, new { success = false, message = "An error occurred while updating the trip plan." });
        }
    }

    /// <summary>
    /// Cancels an active trip plan.
    /// </summary>
    [HttpDelete("{planId}")]
    public async Task<IActionResult> CancelTripPlan(int planId)
    {
        try
        {
            var success = await _tripPlanService.CancelTripPlanAsync(planId);

            if (!success)
            {
                return NotFound(new { success = false, message = "Trip plan not found or no longer active." });
            }

            return Ok(new { success = true, message = "Trip plan cancelled successfully." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling trip plan");
            return StatusCode(500, new { success = false, message = "An error occurred while cancelling the trip plan." });
        }
    }

    /// <summary>
    /// Gets a specific trip plan by ID.
    /// </summary>
    [HttpGet("{planId}")]
    public async Task<IActionResult> GetTripPlanById(int planId)
    {
        try
        {
            var (userId, cardId) = GetUserAndCardIdFromClaims();
            var plan = await _tripPlanService.GetTripPlanByIdAsync(planId, userId, cardId);

            if (plan == null)
            {
                return NotFound(new { success = false, message = "Trip plan not found." });
            }

            var response = new TripPlanResponse
            {
                PlanId = plan.PlanId,
                CardId = plan.CardId,
                OriginTerminalId = plan.OriginTerminalId,
                OriginTerminalName = plan.OriginTerminal?.TerminalName ?? string.Empty,
                DestinationTerminalId = plan.DestinationTerminalId,
                DestinationTerminalName = plan.DestinationTerminal?.TerminalName ?? string.Empty,
                Status = plan.Status,
                CreatedAt = plan.CreatedAt,
                ExpiresAt = plan.ExpiresAt,
                UsedAt = plan.UsedAt,
                NormalFare = plan.NormalFare,
                DiscountAmount = plan.DiscountAmount,
                DiscountPercentage = plan.DiscountPercentage,
                FinalFarePrice = plan.FinalFarePrice
            };

            return Ok(new { success = true, data = response });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting trip plan by ID");
            return StatusCode(500, new { success = false, message = "An error occurred while retrieving the trip plan." });
        }
    }

    /// <summary>
    /// Gets the authenticated passenger's trip plan history.
    /// </summary>
    [HttpGet("history")]
    public async Task<IActionResult> GetTripPlanHistory()
    {
        try
        {
            var (userId, cardId) = GetUserAndCardIdFromClaims();
            var plans = await _tripPlanService.GetTripPlanHistoryAsync(userId, cardId);

            var response = plans.Select(plan => new TripPlanResponse
            {
                PlanId = plan.PlanId,
                CardId = plan.CardId,
                OriginTerminalId = plan.OriginTerminalId,
                OriginTerminalName = plan.OriginTerminal?.TerminalName ?? string.Empty,
                DestinationTerminalId = plan.DestinationTerminalId,
                DestinationTerminalName = plan.DestinationTerminal?.TerminalName ?? string.Empty,
                Status = plan.Status,
                CreatedAt = plan.CreatedAt,
                ExpiresAt = plan.ExpiresAt,
                UsedAt = plan.UsedAt,
                NormalFare = plan.NormalFare,
                DiscountAmount = plan.DiscountAmount,
                DiscountPercentage = plan.DiscountPercentage,
                FinalFarePrice = plan.FinalFarePrice
            });

            return Ok(new { success = true, data = response });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting trip plan history");
            return StatusCode(500, new { success = false, message = "An error occurred while retrieving trip plan history." });
        }
    }
}
