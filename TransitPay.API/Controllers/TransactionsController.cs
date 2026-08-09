using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TransitPay.API.Data;
using TransitPay.API.Enums;
using TransitPay.API.Utilities;

namespace TransitPay.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class TransactionsController : ControllerBase
{
    private readonly TransitPayDbContext _dbContext;

    public TransactionsController(TransitPayDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpGet("{cardId}")]
    public async Task<IActionResult> GetTransactions(int cardId, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var userId = User.GetAuthenticatedUserId();
        if (userId == null)
        {
            return Unauthorized(new { success = false, message = "User not authenticated." });
        }

        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        // Ownership validation: the card must belong to the authenticated user,
        // or the caller must be an Admin or Driver (who can view transactions for operational purposes).
        var isAdmin = User.IsInRole(nameof(RoleName.Admin));
        var isDriver = User.IsInRole(nameof(RoleName.Driver));

        if (!isAdmin && !isDriver)
        {
            var card = await _dbContext.Cards
                .FirstOrDefaultAsync(c => c.CardId == cardId);

            if (card == null || card.UserId != userId.Value)
            {
                return NotFound(new { success = false, message = "Transactions not found." });
            }
        }

        var query = _dbContext.Transactions
            .Where(t => t.CardId == cardId && t.DeletedAt == null);

        var total = await query.CountAsync();
        var transactions = await query
            .Include(t => t.Driver)
            .Include(t => t.Card)
            .OrderByDescending(t => t.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        // Project to a DTO to avoid serializing navigation properties (which can cause
        // circular reference errors when a transaction has Trip/Driver/FareRule set).
        var data = transactions.Select(t => new
        {
            t.TransactionId,
            t.CardId,
            t.Amount,
            t.TransactionType,
            t.TransactionName,
            t.Status,
            t.TransactionReferenceNumber,
            t.OriginTerminalId,
            t.OriginTerminalName,
            t.TerminalId,
            t.DestinationTerminalName,
            t.FinalFare,
            t.RemainingBalance,
            t.PaymentMode,
            DriverName = t.Driver != null ? $"{t.Driver.FirstName} {t.Driver.LastName}".Trim() : null,
            MaskedCardNumber = t.Card != null ? CardFormatter.MaskCardNumber(t.Card.CardNumber) : null,
            t.CreatedAt
        });

        return Ok(new
        {
            success = true,
            message = "Transactions retrieved successfully.",
            data,
            pagination = new { page, pageSize, total, totalPages = (int)Math.Ceiling(total / (double)pageSize) }
        });
    }

    /// <summary>
    /// Retrieves all transactions processed by the authenticated driver.
    /// Returns paginated results with passenger and fare details.
    /// </summary>
    [HttpGet("driver")]
    [Authorize(Roles = "Driver,Admin")]
    public async Task<IActionResult> GetTransactionsByDriver([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var driverId = User.GetAuthenticatedUserId();
        if (driverId == null)
        {
            return Unauthorized(new { success = false, message = "Driver not authenticated." });
        }

        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = _dbContext.Transactions
            .Where(t => t.DriverId == driverId.Value && 
                        t.TransactionType == TransactionType.PAYMENT && 
                        t.DeletedAt == null);

        var total = await query.CountAsync();
        var transactions = await query
            .Include(t => t.Card)
                .ThenInclude(c => c!.User)
            .OrderByDescending(t => t.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        // Project to a DTO to avoid serializing navigation properties
        var data = transactions.Select(t => new
        {
            t.TransactionId,
            t.CardId,
            t.Amount,
            t.TransactionType,
            t.TransactionName,
            t.Status,
            t.TransactionReferenceNumber,
            t.OriginTerminalId,
            t.OriginTerminalName,
            t.TerminalId,
            t.DestinationTerminalName,
            t.FinalFare,
            t.RemainingBalance,
            t.PaymentMode,
            PassengerName = t.Card != null && t.Card.User != null 
                ? $"{t.Card.User.FirstName} {t.Card.User.LastName}".Trim() 
                : "Unknown Passenger",
            MaskedCardNumber = t.Card != null ? CardFormatter.MaskCardNumber(t.Card.CardNumber) : null,
            t.CreatedAt
        });

        return Ok(new
        {
            success = true,
            message = "Driver transactions retrieved successfully.",
            data,
            pagination = new { page, pageSize, total, totalPages = (int)Math.Ceiling(total / (double)pageSize) }
        });
    }
}