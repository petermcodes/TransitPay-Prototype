using Microsoft.EntityFrameworkCore;
using TransitPay.API.Data;
using TransitPay.API.Enums;
using TransitPay.API.Models;

namespace TransitPay.API.Services;

/// <summary>
/// Shared fare calculation logic used by both the FareController and TripPlanService.
/// Ensures a single source of truth for fare rule lookup and discount application.
/// </summary>
public class FareCalculator
{
    private readonly TransitPayDbContext _dbContext;

    /// <summary>
    /// Creates a new FareCalculator.
    /// </summary>
    public FareCalculator(TransitPayDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <summary>
    /// Calculates the fare for a given route and card.
    /// Looks up the active fare rule, applies the card's approved discount,
    /// and returns the full fare breakdown.
    /// </summary>
    /// <param name="originTerminalId">The boarding terminal ID.</param>
    /// <param name="destinationTerminalId">The alighting terminal ID.</param>
    /// <param name="cardId">The transit card ID used to resolve the passenger's active discount.</param>
    /// <param name="vehicleType">Optional vehicle type filter for the fare rule lookup (defaults to any).</param>
    /// <param name="passengerType">Optional passenger type filter for the fare rule lookup (defaults to any).</param>
    /// <returns>A <see cref="FareCalculationResult"/> with the normal fare, discount breakdown, and final fare.</returns>
    public async Task<FareCalculationResult> CalculateFareAsync(
        int originTerminalId, int destinationTerminalId, int cardId,
        VehicleType? vehicleType = null, PassengerType? passengerType = null)
    {
        // Get the fare rule for this route, optionally filtered by vehicle/passenger type
        var fareRuleQuery = _dbContext.FareRules
            .Where(f =>
                f.OriginTerminalId == originTerminalId &&
                f.DestinationTerminalId == destinationTerminalId &&
                f.IsActive &&
                f.EffectiveDate <= DateTime.UtcNow);

        if (vehicleType.HasValue)
        {
            fareRuleQuery = fareRuleQuery.Where(f => f.VehicleType == vehicleType.Value);
        }

        if (passengerType.HasValue)
        {
            fareRuleQuery = fareRuleQuery.Where(f => f.PassengerType == passengerType.Value);
        }

        var fareRule = await fareRuleQuery
            .FirstOrDefaultAsync();

        // Use the fare rule amount as the normal fare; fall back to 0.00 if none exists
        var normalFare = fareRule?.FareAmount ?? 0.00m;

        decimal? discountAmount = null;
        decimal? discountPercentage = null;

        // Check for active discount from PassengerDiscounts (single source of truth)
        // The discount percentage is snapshotted at approval time
        var activeDiscount = await _dbContext.PassengerDiscounts
            .Include(pd => pd.DiscountProgram)
            .Where(pd => pd.CardId == cardId &&
                         pd.Status == PassengerDiscountStatus.Active &&
                         (pd.ExpiresAt == null || pd.ExpiresAt > DateTime.UtcNow))
            .OrderByDescending(pd => pd.ApprovedAt)
            .FirstOrDefaultAsync();

        if (activeDiscount != null && activeDiscount.DiscountProgram != null)
        {
            discountPercentage = activeDiscount.DiscountProgram.DiscountPercentage;
            discountAmount = normalFare * (discountPercentage.Value / 100m);
        }

        var finalFare = normalFare - (discountAmount ?? 0);

        return new FareCalculationResult
        {
            NormalFare = normalFare,
            DiscountAmount = discountAmount,
            DiscountPercentage = discountPercentage,
            FinalFare = finalFare
        };
    }
}

/// <summary>
/// Result of a fare calculation.
/// </summary>
public class FareCalculationResult
{
    /// <summary>The base fare from the fare matrix before any discount.</summary>
    public decimal NormalFare { get; set; }

    /// <summary>The absolute discount amount, or <c>null</c> when the card has no active discount.</summary>
    public decimal? DiscountAmount { get; set; }

    /// <summary>The snapshotted discount percentage, or <c>null</c> when no discount applies.</summary>
    public decimal? DiscountPercentage { get; set; }

    /// <summary>The fare the passenger is actually charged (normal fare minus discount).</summary>
    public decimal FinalFare { get; set; }
}