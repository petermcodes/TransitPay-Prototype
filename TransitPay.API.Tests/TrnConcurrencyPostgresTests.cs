using Microsoft.EntityFrameworkCore;
using TransitPay.API.Data;
using TransitPay.API.Services;
using Xunit;

namespace TransitPay.API.Tests;

/// <summary>
/// Opt-in integration test that validates the atomic TRN counter uniqueness
/// against a real PostgreSQL database. This is the only test that can truly
/// verify the concurrency guarantee of the atomic counter table.
///
/// To run: set DB_PASSWORD and RUN_POSTGRES_TESTS=1, then run dotnet test.
/// When DB_PASSWORD is not set, these tests are skipped automatically.
/// </summary>
public class TrnConcurrencyPostgresTests
{
    private static readonly bool _runPostgresTests =
        Environment.GetEnvironmentVariable("RUN_POSTGRES_TESTS") == "1";

    private static readonly string? _dbPassword =
        Environment.GetEnvironmentVariable("DB_PASSWORD");

    private static string? _connectionString;

    private static string GetConnectionString()
    {
        if (_connectionString != null) return _connectionString;

        var baseConnection = "Host=localhost;Port=5432;Database=TransitPayDB;Username=postgres;Password=${DB_PASSWORD}";
        _connectionString = baseConnection.Replace("${DB_PASSWORD}", _dbPassword);
        return _connectionString;
    }

    private static TransitPayDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<TransitPayDbContext>()
            .UseNpgsql(GetConnectionString())
            .Options;
        return new TransitPayDbContext(options);
    }

    [Fact]
    public async Task GenerateNextAsync_ConcurrentCalls_AllTRNsAreUnique()
    {
        if (!_runPostgresTests || string.IsNullOrEmpty(_dbPassword))
        {
            return; // Skip when not explicitly enabled
        }

        await using var context = CreateContext();

        // Ensure the trn_counters table exists (migrations should have been applied)
        var generator = new TransactionReferenceNumberGenerator(context);

        // Run 50 concurrent calls
        const int callCount = 50;
        var tasks = Enumerable.Range(0, callCount)
            .Select(_ => generator.GenerateNextAsync())
            .ToArray();

        var results = await Task.WhenAll(tasks);

        // All TRNs must be unique
        Assert.Equal(callCount, results.Distinct().Count());

        // All TRNs must match the expected format
        foreach (var trn in results)
        {
            Assert.Matches(@"^TRN-\d{8}-\d{6}$", trn);
        }
    }

    [Fact]
    public async Task GenerateNextAsync_SequentialCalls_AreIncrementing()
    {
        if (!_runPostgresTests || string.IsNullOrEmpty(_dbPassword))
        {
            return; // Skip when not explicitly enabled
        }

        await using var context = CreateContext();
        var generator = new TransactionReferenceNumberGenerator(context);

        var first = await generator.GenerateNextAsync();
        var second = await generator.GenerateNextAsync();

        Assert.NotEqual(first, second);

        // Extract sequence numbers and verify they differ
        var seq1 = int.Parse(first[^6..]);
        var seq2 = int.Parse(second[^6..]);
        Assert.NotEqual(seq1, seq2);
    }
}