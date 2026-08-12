using TransitPay.API.Services;
using Xunit;

namespace TransitPay.API.Tests;

/// <summary>
/// Integration tests for the GUID-based Transaction Reference Number (TNR) generator.
/// Since the new implementation uses GUID v4 (no database dependency), these tests
/// run without any database connection.
/// </summary>
public class TrnConcurrencyPostgresTests
{
    private readonly TransactionReferenceNumberGenerator _generator = new();

    [Fact]
    public async Task GenerateNextAsync_ConcurrentCalls_AllTNRsAreUnique()
    {
        var generator = new TransactionReferenceNumberGenerator();

        // Run 50 concurrent calls
        const int callCount = 50;
        var tasks = Enumerable.Range(0, callCount)
            .Select(_ => generator.GenerateNextAsync())
            .ToArray();

        var results = await Task.WhenAll(tasks);

        // All TNRs must be unique
        Assert.Equal(callCount, results.Distinct().Count());

        // All TNRs must match the expected format: TNR-XXXXXXXX-XXXXXXXX
        foreach (var tnr in results)
        {
            Assert.Matches(@"^TNR-[A-F0-9]{8}-[A-F0-9]{8}$", tnr);
        }
    }

    [Fact]
    public async Task GenerateNextAsync_SequentialCalls_AreUnique()
    {
        var generator = new TransactionReferenceNumberGenerator();

        var first = await generator.GenerateNextAsync();
        var second = await generator.GenerateNextAsync();

        Assert.NotEqual(first, second);

        // Both must match the TNR format
        Assert.Matches(@"^TNR-[A-F0-9]{8}-[A-F0-9]{8}$", first);
        Assert.Matches(@"^TNR-[A-F0-9]{8}-[A-F0-9]{8}$", second);
    }
}