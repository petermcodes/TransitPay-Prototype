using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TransitPay.API.Data;
using TransitPay.API.Models;

namespace TransitPay.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CardsController : ControllerBase
{
    private readonly TransitPayDbContext _dbContext;

    public CardsController(TransitPayDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> CreateCard([FromBody] CreateCardRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new { success = false, message = "Validation failed.", errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage) });
        }

        var existingCard = await _dbContext.Cards.FirstOrDefaultAsync(c => c.CardNumber == request.CardNumber);
        if (existingCard != null)
        {
            return BadRequest(new { success = false, message = "Card number already exists." });
        }

        var card = new Card
        {
            CardNumber = request.CardNumber,
            UserId = request.UserId,
            Status = "ACTIVE",
            IssueDate = DateTime.UtcNow,
            ExpiryDate = DateTime.UtcNow.AddYears(1),
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.Cards.Add(card);
        await _dbContext.SaveChangesAsync();

        _dbContext.Wallets.Add(new Wallet { CardId = card.CardId, Balance = 0, Status = "ACTIVE", CreatedAt = DateTime.UtcNow });
        await _dbContext.SaveChangesAsync();

        return Ok(new { success = true, message = "Card created successfully.", data = card });
    }

    [HttpGet("{cardNumber}")]
    public async Task<IActionResult> GetCard(string cardNumber)
    {
        var card = await _dbContext.Cards.FirstOrDefaultAsync(c => c.CardNumber == cardNumber);
        if (card == null)
        {
            return NotFound(new { success = false, message = "Card not found." });
        }

        return Ok(new { success = true, message = "Card retrieved successfully.", data = card });
    }

    [HttpGet("validate/{cardNumber}")]
    [Authorize(Roles = "Driver,Admin")]
    public async Task<IActionResult> ValidateCard(string cardNumber)
    {
        var card = await _dbContext.Cards
            .Include(c => c.Wallet)
            .FirstOrDefaultAsync(c => c.CardNumber == cardNumber);

        if (card == null)
        {
            return NotFound(new { success = false, message = "Card not found." });
        }

        if (card.Status != "ACTIVE")
        {
            return BadRequest(new { success = false, message = $"Card cannot be used. Status: {card.Status}" });
        }

        return Ok(new
        {
            success = true,
            message = "Card is valid.",
            data = new
            {
                card.CardId,
                card.CardNumber,
                card.Status,
                balance = card.Wallet?.Balance ?? 0m
            }
        });
    }
}

public class CreateCardRequest
{
    [Required(ErrorMessage = "Card number is required.")]
    [RegularExpression(@"^\d{16}$", ErrorMessage = "Card number must be 16 digits.")]
    public string CardNumber { get; set; } = string.Empty;

    public int? UserId { get; set; }
}