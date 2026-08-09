using Microsoft.EntityFrameworkCore;
using TransitPay.API.Data;

namespace TransitPay.API.Services;

/// <summary>
/// Generates unique human-readable Transaction Reference Numbers (TRN).
/// Format: TRN-YYYYMMDD-XXXXXX (e.g., TRN-20260804-000001).
/// 
/// Uniqueness is guaranteed by an atomic counter table (trn_counters).
/// Each call performs a single atomic INSERT ... ON CONFLICT ... RETURNING,
/// so concurrent payments (different drivers/passengers) always receive
/// distinct sequence numbers — no race condition.
/// 
/// The same TRN is stored on the single Transaction record, so both the
/// driver and the passenger see the identical receipt number for that
/// transaction, and no two transactions ever share a TRN.
/// </summary>
public class TransactionReferenceNumberGenerator
{
    private readonly TransitPayDbContext _dbContext;

    public TransactionReferenceNumberGenerator(TransitPayDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <summary>
    /// Generates the next available TRN for today's date using an atomic
    /// counter upsert. Format: TRN-YYYYMMDD-XXXXXX where XXXXXX is a
    /// six-digit zero-padded sequence.
    /// </summary>
    public async Task<string> GenerateNextAsync()
    {
        var date = DateTime.UtcNow.ToString("yyyyMMdd");
        var prefix = $"TRN-{date}-";

        // Atomic upsert: insert a new counter row for today (UTC), or increment
        // the existing one. RETURNING gives us the unique sequence number.
        // This single statement is serialized by the DB, so concurrent calls
        // always get distinct values. The UTC date aligns with the prefix date.
        var sql = @"
            INSERT INTO trn_counters (counter_date, last_sequence)
            VALUES ((now() AT TIME ZONE 'UTC')::date, 1)
            ON CONFLICT (counter_date)
            DO UPDATE SET last_sequence = trn_counters.last_sequence + 1
            RETURNING last_sequence;";

        var sequence = await _dbContext.Database
            .SqlQueryRaw<int>(sql)
            .SingleAsync();

        return $"{prefix}{sequence:D6}";
    }
}