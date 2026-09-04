using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TransitPay.API.Data;
using TransitPay.API.Enums;
using TransitPay.API.Models;

namespace TransitPay.API.Controllers;

/// <summary>
/// Fare matrix (FareRule) management endpoints (Admin only).
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class FareRuleController : ControllerBase
{
    private readonly TransitPayDbContext _dbContext;

    /// <summary>
    /// Creates a new FareRuleController.
    /// </summary>
    public FareRuleController(TransitPayDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <summary>
    /// Retrieves all non-deleted fare rules with terminal names (Admin only).
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetFareRules()
    {
        var fareRules = await _dbContext.FareRules
            .Include(fr => fr.OriginTerminal)
            .Include(fr => fr.DestinationTerminal)
            .Where(fr => fr.DeletedAt == null)
            .Select(fr => new
            {
                fr.FareId,
                originTerminalName = fr.OriginTerminal!.TerminalName,
                destinationTerminalName = fr.DestinationTerminal!.TerminalName,
                fr.VehicleType,
                fr.PassengerType,
                fr.FareAmount,
                fr.EffectiveDate,
                fr.IsActive
            })
            .ToListAsync();
        return Ok(new { success = true, message = "Fare rules retrieved successfully.", data = fareRules });
    }

    /// <summary>
    /// Creates a new fare rule (Admin only). New rules default to BUS/Passenger types
    /// and start active with today's effective date.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreateFareRule([FromBody] FareRuleCreateRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new { success = false, message = "Validation failed.", errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage) });
        }

        var fareRule = new FareRule
        {
            OriginTerminalId = request.OriginTerminalId,
            DestinationTerminalId = request.DestinationTerminalId,
            VehicleType = VehicleType.BUS, // Default value
            PassengerType = PassengerType.Passenger, // Default value
            FareAmount = request.FareAmount,
            EffectiveDate = request.EffectiveDate,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        _dbContext.FareRules.Add(fareRule);
        await _dbContext.SaveChangesAsync();
        return Ok(new { success = true, message = "Fare rule created successfully.", data = fareRule });
    }
}

/// <summary>
/// Request DTO for creating a fare matrix entry.
/// </summary>
public class FareRuleCreateRequest
{
    /// <summary>The boarding terminal ID.</summary>
    [Required(ErrorMessage = "Origin terminal ID is required.")]
    public int OriginTerminalId { get; set; }

    /// <summary>The alighting terminal ID.</summary>
    [Required(ErrorMessage = "Destination terminal ID is required.")]
    public int DestinationTerminalId { get; set; }

    /// <summary>The fare amount charged for this route.</summary>
    [Range(0.01, 10000, ErrorMessage = "Fare amount must be greater than 0.")]
    public decimal FareAmount { get; set; }

    /// <summary>The date from which this fare rule takes effect.</summary>
    public DateTime EffectiveDate { get; set; } = DateTime.UtcNow;
}
