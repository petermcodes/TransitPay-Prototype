using Microsoft.EntityFrameworkCore;
using TransitPay.API.Data;
using TransitPay.API.DTOs.TripPlan;
using TransitPay.API.Enums;
using TransitPay.API.Interfaces;
using TransitPay.API.Models;

namespace TransitPay.API.Services;

/// <summary>
/// Service for managing passenger Trip Plans.
/// Creates, reads, updates, and cancels the active journey plan that a passenger
/// sets up before boarding. The fare is locked in at plan creation/update time by
/// the shared <see cref="FareCalculator"/>, and the stored fare breakdown is what the
/// conductor payment flow charges — so the charged amount always matches what the
/// passenger was quoted. Plans expire 24 hours after creation or update.
/// </summary>
public class TripPlanService : ITripPlanService
{
    private readonly TransitPayDbContext _dbContext;
    private readonly ILogger<TripPlanService> _logger;
    private readonly FareCalculator _fareCalculator;

    /// <summary>
    /// Creates a new TripPlanService. The shared <see cref="FareCalculator"/> is injected
    /// so fare calculation stays as a single source of truth.
    /// </summary>
    public TripPlanService(TransitPayDbContext dbContext, ILogger<TripPlanService> logger, FareCalculator fareCalculator)
    {
        _dbContext = dbContext;
        _logger = logger;
        _fareCalculator = fareCalculator;
    }

    /// <inheritdoc />
    public async Task<TripPlan> CreateTripPlanAsync(int userId, int cardId, int originTerminalId, int destinationTerminalId)
    {
        // Cancel any existing active plan for this user and card
        var existing = await _dbContext.TripPlans
            .FirstOrDefaultAsync(tp => tp.UserId == userId && tp.CardId == cardId && tp.Status == "Active");

        if (existing != null)
        {
            existing.Status = "Cancelled";
            existing.UpdatedAt = DateTime.UtcNow;
        }

        // Get the card to verify it belongs to the user
        var card = await _dbContext.Cards
            .FirstOrDefaultAsync(c => c.CardId == cardId && c.UserId == userId);

        if (card == null)
        {
            throw new InvalidOperationException("Card not found or does not belong to the user.");
        }

        // Calculate the fare using the shared FareCalculator (single source of truth)
        // Pass VehicleType.BUS and card's PassengerType to ensure fare matches what conductor will charge
        var fare = await _fareCalculator.CalculateFareAsync(
            originTerminalId, destinationTerminalId, cardId,
            VehicleType.BUS, card.PassengerType);
        var normalFare = fare.NormalFare;
        var discountAmount = fare.DiscountAmount;
        var discountPercentage = fare.DiscountPercentage;
        var finalFare = fare.FinalFare;

        var plan = new TripPlan
        {
            UserId = userId,
            CardId = cardId,
            OriginTerminalId = originTerminalId,
            DestinationTerminalId = destinationTerminalId,
            Status = "Active",
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddHours(24),
            NormalFare = normalFare,
            DiscountAmount = discountAmount,
            DiscountPercentage = discountPercentage,
            FinalFarePrice = finalFare
        };

        _dbContext.TripPlans.Add(plan);
        await _dbContext.SaveChangesAsync();

        return plan;
    }

    /// <inheritdoc />
    public async Task<TripPlan?> GetActiveTripPlanAsync(int userId, int cardId)
    {
        return await _dbContext.TripPlans
            .Include(tp => tp.OriginTerminal)
            .Include(tp => tp.DestinationTerminal)
            .FirstOrDefaultAsync(tp => tp.UserId == userId && tp.CardId == cardId && tp.Status == "Active");
    }

    /// <inheritdoc />
    public async Task<TripPlan?> GetTripPlanByIdAsync(int planId, int userId, int cardId)
    {
        return await _dbContext.TripPlans
            .Include(tp => tp.OriginTerminal)
            .Include(tp => tp.DestinationTerminal)
            .FirstOrDefaultAsync(tp => tp.PlanId == planId && tp.UserId == userId && tp.CardId == cardId);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<TripPlan>> GetTripPlanHistoryAsync(int userId, int cardId)
    {
        return await _dbContext.TripPlans
            .Include(tp => tp.OriginTerminal)
            .Include(tp => tp.DestinationTerminal)
            .Where(tp => tp.UserId == userId && tp.CardId == cardId)
            .OrderByDescending(tp => tp.CreatedAt)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<TripPlan?> UpdateTripPlanDestinationAsync(int planId, int newDestinationTerminalId)
    {
        var plan = await _dbContext.TripPlans
            .FirstOrDefaultAsync(tp => tp.PlanId == planId && tp.Status == "Active");

        if (plan == null)
        {
            return null;
        }

        plan.DestinationTerminalId = newDestinationTerminalId;
        plan.UpdatedAt = DateTime.UtcNow;
        plan.ExpiresAt = DateTime.UtcNow.AddHours(24);

        // Recalculate the fare for the new route so stored values stay in sync
        // Pass VehicleType.BUS and card's PassengerType to ensure fare matches what conductor will charge
        var card = await _dbContext.Cards.FirstOrDefaultAsync(c => c.CardId == plan.CardId);
        if (card != null)
        {
            var fare = await _fareCalculator.CalculateFareAsync(
                plan.OriginTerminalId, newDestinationTerminalId, plan.CardId,
                VehicleType.BUS, card.PassengerType);
            plan.NormalFare = fare.NormalFare;
            plan.DiscountAmount = fare.DiscountAmount;
            plan.DiscountPercentage = fare.DiscountPercentage;
            plan.FinalFarePrice = fare.FinalFare;
        }

        await _dbContext.SaveChangesAsync();
        return plan;
    }

    /// <inheritdoc />
    public async Task<bool> CancelTripPlanAsync(int planId)
    {
        var plan = await _dbContext.TripPlans
            .FirstOrDefaultAsync(tp => tp.PlanId == planId && tp.Status == "Active");

        if (plan == null)
        {
            return false;
        }

        plan.Status = "Cancelled";
        plan.UpdatedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync();
        return true;
    }

}
