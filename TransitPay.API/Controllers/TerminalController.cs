using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TransitPay.API.Data;
using TransitPay.API.Models;

namespace TransitPay.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TerminalController : ControllerBase
{
    private readonly TransitPayDbContext _dbContext;

    public TerminalController(TransitPayDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> GetTerminals()
    {
        var terminals = await _dbContext.Terminals
            .Where(t => t.DeletedAt == null && t.IsActive)
            .Select(t => new { t.TerminalId, t.TerminalName, t.IsActive })
            .ToListAsync();
        return Ok(new { success = true, message = "Terminals retrieved successfully.", data = terminals });
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> CreateTerminal([FromBody] CreateTerminalRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new { success = false, message = "Validation failed.", errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage) });
        }

        var terminal = new Terminal
        {
            TerminalName = request.TerminalName,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        _dbContext.Terminals.Add(terminal);
        await _dbContext.SaveChangesAsync();
        return Ok(new { success = true, message = "Terminal created successfully.", data = terminal });
    }
}

public class CreateTerminalRequest
{
    [Required(ErrorMessage = "Terminal name is required.")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "Terminal name must be between 2 and 100 characters.")]
    public string TerminalName { get; set; } = string.Empty;
}