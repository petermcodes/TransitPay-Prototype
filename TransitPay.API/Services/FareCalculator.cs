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

    public FareCalculator(TransitPayDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <summary>
    /// Calculates the fare for a given route and card.
    /// Looks up the active fare rule, applies the card's approved discount,
    /// and returns the full fare breakdown.
    /// </summary>
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
    public decimal NormalFare { get; set; }
    public decimal? DiscountAmount { get; set; }
    public decimal? DiscountPercentage { get; set; }
    public decimal FinalFare { get; set; }
}