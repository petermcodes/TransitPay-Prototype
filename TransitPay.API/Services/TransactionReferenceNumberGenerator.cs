using Microsoft.EntityFrameworkCore;
using TransitPay.API.Data;

namespace TransitPay.API.Services;

/// <summary>
/// Generates unique human-readable Transaction Reference Numbers (TRN).
/// Format: TRN-YYYYMMDD-XXXXXX (e.g., TRN-20260804-000001).
/// Generated inside the same DB transaction as the wallet deduction so failed
/// attempts don't create gaps.
/// </summary>
public class TransactionReferenceNumberGenerator
{
    private readonly TransitPayDbContext _dbContext;

    public TransactionReferenceNumberGenerator(TransitPayDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <summary>
    /// Generates the next available TRN for today's date.
    /// Format: TRN-YYYYMMDD-XXXXXX where XXXXXX is a six-digit zero-padded sequence.
    /// </summary>
    public async Task<string> GenerateNextAsync()
    {
        var date = DateTime.UtcNow.ToString("yyyyMMdd");
        var prefix = $"TRN-{date}-";

        // Find the highest existing sequence for today
        var lastTrn = await _dbContext.Transactions
            .Where(t => t.TransactionReferenceNumber != null &&
                        t.TransactionReferenceNumber.StartsWith(prefix))
            .OrderByDescending(t => t.TransactionReferenceNumber)
            .Select(t => t.TransactionReferenceNumber)
            .FirstOrDefaultAsync();

        int nextSequence = 1;
        if (!string.IsNullOrEmpty(lastTrn))
        {
            var lastPart = lastTrn[(lastTrn.Length - 6)..];
            if (int.TryParse(lastPart, out var lastSeq))
            {
                nextSequence = lastSeq + 1;
            }
        }

        return $"{prefix}{nextSequence:D6}";
    }
}