using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TransitPay.API.Data;
using TransitPay.API.Interfaces;

namespace TransitPay.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PaymentController : ControllerBase
{
    private readonly IPaymentService _paymentService;

    public PaymentController(IPaymentService paymentService)
    {
        _paymentService = paymentService;
    }

    [HttpPost("fare")]
    public async Task<IActionResult> PayFare([FromBody] PaymentRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new { success = false, message = "Validation failed.", errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage) });
        }

        var result = await _paymentService.ProcessPaymentAsync(request.CardId, request.StationId, request.Amount);
        return Ok(result);
    }

    [HttpGet("fare/{cardId}/{stationId}")]
    public async Task<IActionResult> PreviewFare(int cardId, int stationId)
    {
        var dbContext = HttpContext.RequestServices.GetRequiredService<TransitPayDbContext>();
        var fareRule = await dbContext.FareRules
            .Where(fr => fr.IsActive && fr.DestinationStationId == stationId)
            .OrderByDescending(fr => fr.EffectiveDate)
            .FirstOrDefaultAsync();

        return Ok(new { success = true, data = new { cardId, stationId, fareAmount = fareRule?.FareAmount ?? 0m } });
    }
}

public class PaymentRequest
{
    [Required(ErrorMessage = "Card ID is required.")]
    public int CardId { get; set; }

    [Required(ErrorMessage = "Station ID is required.")]
    public int StationId { get; set; }

    [Range(0, 100000, ErrorMessage = "Amount must be between 0 and 100,000.")]
    public decimal Amount { get; set; }
}