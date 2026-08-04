using System.ComponentModel.DataAnnotations;
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
    public async Task<IActionResult> CreateStation([FromBody] StationCreateRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new { success = false, message = "Validation failed.", errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage) });
        }

        var station = new Station
        {
            TownId = request.TownId,
            StationName = request.StationName,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        _dbContext.Stations.Add(station);
        await _dbContext.SaveChangesAsync();
        return Ok(new { success = true, message = "Station created successfully.", data = station });
    }
}

public class StationCreateRequest
{
    [Required(ErrorMessage = "Town ID is required.")]
    public int TownId { get; set; }

    [Required(ErrorMessage = "Station name is required.")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "Station name must be between 2 and 100 characters.")]
    public string StationName { get; set; } = string.Empty;
}
