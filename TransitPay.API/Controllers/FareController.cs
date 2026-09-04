using Microsoft.AspNetCore.Mvc;
using TransitPay.API.Services;

namespace TransitPay.API.Controllers;

/// <summary>
/// Fare calculation endpoints. Uses the shared <see cref="FareCalculator"/> so the
/// fare quote always matches what the payment flow actually charges.
/// </summary>
[ApiController]
[Route("api/fare")]
public class FareController : ControllerBase
{
    private readonly FareCalculator _fareCalculator;
    private readonly ILogger<FareController> _logger;

    /// <summary>
    /// Creates a new FareController.
    /// </summary>
    public FareController(FareCalculator fareCalculator, ILogger<FareController> logger)
    {
        _fareCalculator = fareCalculator;
        _logger = logger;
    }

    /// <summary>
    /// Calculates the fare for a given route and card.
    /// Returns normal fare, discount info, and final fare.
    /// </summary>
    [HttpGet("calculate")]
    public async Task<IActionResult> CalculateFare([FromQuery] int originTerminalId, [FromQuery] int destinationTerminalId, [FromQuery] int cardId)
    {
        try
        {
            // Use the shared FareCalculator (single source of truth)
            var fare = await _fareCalculator.CalculateFareAsync(originTerminalId, destinationTerminalId, cardId);

            return Ok(new
            {
                success = true,
                data = new
                {
                    normalFare = fare.NormalFare,
                    discountPercentage = fare.DiscountPercentage,
                    discountAmount = fare.DiscountAmount,
                    finalFare = fare.FinalFare
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating fare");
            return StatusCode(500, new
            {
                success = false,
                message = "Failed to calculate fare"
            });
        }
    }
}