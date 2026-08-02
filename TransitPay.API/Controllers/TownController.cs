using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TransitPay.API.Data;
using TransitPay.API.Models;

namespace TransitPay.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class TownController : ControllerBase
{
    private readonly TransitPayDbContext _dbContext;

    public TownController(TransitPayDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpGet]
    public async Task<IActionResult> GetTowns()
    {
        var towns = await _dbContext.Towns
            .Where(t => t.DeletedAt == null)
            .Select(t => new { t.TownId, t.TownName, t.IsActive, stationCount = t.Stations.Count(s => s.DeletedAt == null) })
            .ToListAsync();
        return Ok(new { success = true, message = "Towns retrieved successfully.", data = towns });
    }

    [HttpPost]
    public async Task<IActionResult> CreateTown([FromBody] Town town)
    {
        _dbContext.Towns.Add(town);
        await _dbContext.SaveChangesAsync();
        return Ok(new { success = true, message = "Town created successfully.", data = town });
    }
}
