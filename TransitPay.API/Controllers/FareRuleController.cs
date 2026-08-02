using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TransitPay.API.Data;
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
    public async Task<IActionResult> CreateFareRule([FromBody] FareRule fareRule)
    {
        _dbContext.FareRules.Add(fareRule);
        await _dbContext.SaveChangesAsync();
        return Ok(new { success = true, message = "Fare rule created successfully.", data = fareRule });
    }
}
