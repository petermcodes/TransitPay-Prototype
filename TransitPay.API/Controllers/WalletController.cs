using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TransitPay.API.Data;
using TransitPay.API.Enums;
using TransitPay.API.Interfaces;
using TransitPay.API.Services;
using TransitPay.API.Utilities;

namespace TransitPay.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class WalletController : ControllerBase
{
    private readonly TransitPayDbContext _dbContext;
    private readonly ITransactionReferenceNumberGenerator _trnGenerator;

    public WalletController(TransitPayDbContext dbContext, ITransactionReferenceNumberGenerator trnGenerator)
    {
        _dbContext = dbContext;
        _trnGenerator = trnGenerator;
    }

    [HttpGet("{cardId}")]
    public async Task<IActionResult> GetWallet(int cardId)
    {
        var userId = User.GetAuthenticatedUserId();
        if (userId == null)
        {
            return Unauthorized(new { success = false, message = "User not authenticated." });
        }

        // Ownership validation: the card must belong to the authenticated user,
        // or the user must be an Admin (who can view any wallet).
        var isAdmin = User.IsInRole(nameof(Enums.RoleName.Admin));

        var wallet = await _dbContext.Wallets
            .Include(w => w.Card)
            .FirstOrDefaultAsync(w => w.CardId == cardId);

        if (wallet == null)
        {
            return NotFound(new { success = false, message = "Wallet not found." });
        }

        // Non-admin users may only access wallets of cards they own
        if (!isAdmin && (wallet.Card == null || wallet.Card.UserId != userId.Value))
        {
            return NotFound(new { success = false, message = "Wallet not found." });
        }

        return Ok(new { success = true, message = "Wallet retrieved successfully.", data = new {
            wallet.WalletId,
            wallet.CardId,
            wallet.Balance,
            wallet.Status,
            wallet.CreatedAt,
            wallet.UpdatedAt
        }});
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

        // Generate a unique Transaction Reference Number (TNR) for the top-up
        var tnr = await _trnGenerator.GenerateNextAsync();

        // Create a transaction record for audit trail with the generated TNR
        _dbContext.Transactions.Add(new Models.Transaction
        {
            CardId = request.CardId,
            Amount = request.Amount,
            TransactionType = TransactionType.TOP_UP,
            TransactionName = "Admin top-up",
            RemainingBalance = wallet.Balance,
            PaymentMode = request.PaymentMode ?? "Admin",
            RegularFare = 0,
            FinalFare = 0,
            TransactionReferenceNumber = tnr,
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

    /// <summary>
    /// The payment mode used for this top-up (e.g., "GCash", "PayMaya", "Bank Transfer", "Admin").
    /// Defaults to "Admin" when not provided.
    /// </summary>
    public string? PaymentMode { get; set; }
}