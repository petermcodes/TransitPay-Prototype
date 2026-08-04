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
    public async Task<IActionResult> CreateTown([FromBody] TownCreateRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new { success = false, message = "Validation failed.", errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage) });
        }

        var town = new Town
        {
            TownName = request.TownName,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        _dbContext.Towns.Add(town);
        await _dbContext.SaveChangesAsync();
        return Ok(new { success = true, message = "Town created successfully.", data = town });
    }
}

public class TownCreateRequest
{
    [Required(ErrorMessage = "Town name is required.")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "Town name must be between 2 and 100 characters.")]
    public string TownName { get; set; } = string.Empty;
}