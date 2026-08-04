using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TransitPay.API.Data;
using TransitPay.API.Enums;

namespace TransitPay.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class WalletController : ControllerBase
{
    private readonly TransitPayDbContext _dbContext;

    public WalletController(TransitPayDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpGet("{cardId}")]
    public async Task<IActionResult> GetWallet(int cardId)
    {
        var wallet = await _dbContext.Wallets.FirstOrDefaultAsync(w => w.CardId == cardId);
        if (wallet == null)
        {
            return NotFound(new { success = false, message = "Wallet not found." });
        }

        return Ok(new { success = true, message = "Wallet retrieved successfully.", data = wallet });
    }

    [HttpPost("topup")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> TopUp([FromBody] TopUpRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new { success = false, message = "Validation failed.", errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage) });
        }

        var wallet = await _dbContext.Wallets.FirstOrDefaultAsync(w => w.CardId == request.CardId);
        if (wallet == null)
        {
            return NotFound(new { success = false, message = "Wallet not found." });
        }

        wallet.Balance += request.Amount;
        wallet.UpdatedAt = DateTime.UtcNow;
        _dbContext.Transactions.Add(new Models.Transaction
        {
            CardId = request.CardId,
            Amount = request.Amount,
            TransactionType = TransactionType.TOP_UP,
            TransactionName = "Admin top-up",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });
        await _dbContext.SaveChangesAsync();

        return Ok(new { success = true, message = "Wallet topped up successfully.", data = wallet });
    }
}

public class TopUpRequest
{
    [Required(ErrorMessage = "Card ID is required.")]
    public int CardId { get; set; }

    [Range(1, 100000, ErrorMessage = "Amount must be between 1 and 100,000.")]
    public decimal Amount { get; set; }
}
