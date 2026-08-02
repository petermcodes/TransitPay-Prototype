using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TransitPay.API.Data;
using TransitPay.API.Models;

namespace TransitPay.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class StationController : ControllerBase
{
    private readonly TransitPayDbContext _dbContext;

    public StationController(TransitPayDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpGet]
    public async Task<IActionResult> GetStations()
    {
        var stations = await _dbContext.Stations
            .Include(s => s.Town)
            .Where(s => s.DeletedAt == null)
            .Select(s => new { s.StationId, s.StationName, s.IsActive, townName = s.Town!.TownName })
            .ToListAsync();
        return Ok(new { success = true, message = "Stations retrieved successfully.", data = stations });
    }

    [HttpPost]
    public async Task<IActionResult> CreateStation([FromBody] Station station)
    {
        _dbContext.Stations.Add(station);
        await _dbContext.SaveChangesAsync();
        return Ok(new { success = true, message = "Station created successfully.", data = station });
    }
}
