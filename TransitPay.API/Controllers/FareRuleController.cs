using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TransitPay.API.Data;
using TransitPay.API.Enums;
using TransitPay.API.Models;

namespace TransitPay.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class FareRuleController : ControllerBase
{
    private readonly TransitPayDbContext _dbContext;

    public FareRuleController(TransitPayDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpGet]
    public async Task<IActionResult> GetFareRules()
    {
        var fareRules = await _dbContext.FareRules
            .Include(fr => fr.OriginStation)
            .Include(fr => fr.DestinationStation)
            .Where(fr => fr.DeletedAt == null)
            .Select(fr => new
            {
                fr.FareId,
                originStationName = fr.OriginStation!.StationName,
                destinationStationName = fr.DestinationStation!.StationName,
                fr.VehicleType,
                fr.PassengerType,
                fr.FareAmount,
                fr.EffectiveDate,
                fr.IsActive
            })
            .ToListAsync();
        return Ok(new { success = true, message = "Fare rules retrieved successfully.", data = fareRules });
    }

    [HttpPost]
    public async Task<IActionResult> CreateFareRule([FromBody] FareRuleCreateRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new { success = false, message = "Validation failed.", errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage) });
        }

        var fareRule = new FareRule
        {
            OriginStationId = request.OriginStationId,
            DestinationStationId = request.DestinationStationId,
            VehicleType = Enum.Parse<VehicleType>(request.VehicleType),
            PassengerType = Enum.Parse<PassengerType>(request.PassengerType),
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

public class FareRuleCreateRequest
{
    [Required(ErrorMessage = "Origin station ID is required.")]
    public int OriginStationId { get; set; }

    [Required(ErrorMessage = "Destination station ID is required.")]
    public int DestinationStationId { get; set; }

    [Required(ErrorMessage = "Vehicle type is required.")]
    public string VehicleType { get; set; } = string.Empty;

    [Required(ErrorMessage = "Passenger type is required.")]
    public string PassengerType { get; set; } = string.Empty;

    [Range(0.01, 10000, ErrorMessage = "Fare amount must be greater than 0.")]
    public decimal FareAmount { get; set; }

    public DateTime EffectiveDate { get; set; } = DateTime.UtcNow;
}