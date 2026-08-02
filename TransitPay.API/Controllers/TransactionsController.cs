using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TransitPay.API.Data;

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
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = _dbContext.Transactions
            .Where(t => t.CardId == cardId && t.DeletedAt == null);

        var total = await query.CountAsync();
        var transactions = await query
            .OrderByDescending(t => t.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return Ok(new
        {
            success = true,
            message = "Transactions retrieved successfully.",
            data = transactions,
            pagination = new { page, pageSize, total, totalPages = (int)Math.Ceiling(total / (double)pageSize) }
        });
    }
}