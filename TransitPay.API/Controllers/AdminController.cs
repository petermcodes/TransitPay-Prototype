using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TransitPay.API.Data;
using TransitPay.API.Enums;
using TransitPay.API.Models;

namespace TransitPay.API.Controllers;

[ApiController]
[Route("api/admin")]
[Authorize(Roles = "Admin")]
public class AdminController : ControllerBase
{
    private readonly TransitPayDbContext _dbContext;

    public AdminController(TransitPayDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpGet("users")]
    public async Task<IActionResult> GetUsers([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = _dbContext.Users
            .Include(u => u.Role)
            .Where(u => u.DeletedAt == null);

        var total = await query.CountAsync();
        var users = await query
            .OrderByDescending(u => u.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(u => new { u.UserId, u.Username, u.FirstName, u.LastName, u.MobileNumber, u.IsActive, roleName = u.Role!.RoleName })
            .ToListAsync();

        return Ok(new
        {
            success = true,
            message = "Users retrieved successfully.",
            data = users,
            pagination = new { page, pageSize, total, totalPages = (int)Math.Ceiling(total / (double)pageSize) }
        });
    }

    [HttpGet("drivers")]
    public async Task<IActionResult> GetDrivers([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var driverRole = await _dbContext.Roles.FirstOrDefaultAsync(r => r.RoleName == RoleName.Driver);
        if (driverRole == null)
        {
            return Ok(new { success = true, message = "No drivers found.", data = new List<object>(), pagination = new { page, pageSize, total = 0, totalPages = 0 } });
        }

        var query = _dbContext.Users
            .Where(u => u.RoleId == driverRole.RoleId && u.DeletedAt == null);

        var total = await query.CountAsync();
        var drivers = await query
            .OrderByDescending(u => u.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(u => new { u.UserId, u.Username, u.FirstName, u.LastName, u.MobileNumber, u.IsActive })
            .ToListAsync();

        return Ok(new
        {
            success = true,
            message = "Drivers retrieved successfully.",
            data = drivers,
            pagination = new { page, pageSize, total, totalPages = (int)Math.Ceiling(total / (double)pageSize) }
        });
    }

    [HttpGet("stations")]
    public async Task<IActionResult> GetStations()
    {
        var stations = await _dbContext.Stations
            .Include(s => s.Town)
            .Where(s => s.DeletedAt == null)
            .Select(s => new { s.StationId, s.StationName, s.IsActive, townName = s.Town!.TownName })
            .ToListAsync();
        return Ok(new { success = true, message = "Stations retrieved successfully.", data = stations });
    }

    [HttpPost("stations")]
    public async Task<IActionResult> CreateStation([FromBody] CreateStationRequest request)
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

    [HttpGet("towns")]
    public async Task<IActionResult> GetTowns()
    {
        var towns = await _dbContext.Towns
            .Where(t => t.DeletedAt == null)
            .Select(t => new { t.TownId, t.TownName, t.IsActive, stationCount = t.Stations.Count(s => s.DeletedAt == null) })
            .ToListAsync();
        return Ok(new { success = true, message = "Towns retrieved successfully.", data = towns });
    }

    [HttpPost("towns")]
    public async Task<IActionResult> CreateTown([FromBody] CreateTownRequest request)
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

    [HttpGet("fare-rules")]
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

    [HttpPost("fare-rules")]
    public async Task<IActionResult> CreateFareRule([FromBody] CreateFareRuleRequest request)
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

    [HttpGet("transactions")]
    public async Task<IActionResult> GetTransactions([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = _dbContext.Transactions
            .Include(t => t.Card)
            .Where(t => t.DeletedAt == null);

        var total = await query.CountAsync();
        var transactions = await query
            .OrderByDescending(t => t.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(t => new
            {
                t.TransactionId,
                cardNumber = t.Card!.CardNumber,
                t.Amount,
                t.TransactionType,
                t.TransactionName,
                t.TransactionReferenceNumber,
                t.ReferenceNumber,
                t.CreatedAt
            })
            .ToListAsync();

        return Ok(new
        {
            success = true,
            message = "Transactions retrieved successfully.",
            data = transactions,
            pagination = new { page, pageSize, total, totalPages = (int)Math.Ceiling(total / (double)pageSize) }
        });
    }

    [HttpGet("reports/summary")]
    public async Task<IActionResult> GetReportSummary()
    {
        var totalUsers = await _dbContext.Users.CountAsync(u => u.DeletedAt == null);
        var totalDrivers = await _dbContext.Users.CountAsync(u => u.DeletedAt == null && u.Role!.RoleName == RoleName.Driver);
        var totalStations = await _dbContext.Stations.CountAsync(s => s.DeletedAt == null);
        var totalTowns = await _dbContext.Towns.CountAsync(t => t.DeletedAt == null);
        var totalTransactions = await _dbContext.Transactions.CountAsync(t => t.DeletedAt == null);
        var totalRevenue = await _dbContext.Transactions
            .Where(t => t.DeletedAt == null && t.TransactionType == TransactionType.PAYMENT)
            .SumAsync(t => (decimal?)t.Amount) ?? 0m;

        return Ok(new
        {
            success = true,
            message = "Report summary retrieved successfully.",
            data = new
            {
                totalUsers,
                totalDrivers,
                totalStations,
                totalTowns,
                totalTransactions,
                totalRevenue
            }
        });
    }
}

public class CreateStationRequest
{
    [Required(ErrorMessage = "Town ID is required.")]
    public int TownId { get; set; }

    [Required(ErrorMessage = "Station name is required.")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "Station name must be between 2 and 100 characters.")]
    public string StationName { get; set; } = string.Empty;
}

public class CreateTownRequest
{
    [Required(ErrorMessage = "Town name is required.")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "Town name must be between 2 and 100 characters.")]
    public string TownName { get; set; } = string.Empty;
}

public class CreateFareRuleRequest
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