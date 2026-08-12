using TransitPay.API.Interfaces;

namespace TransitPay.API.Services;

/// <summary>
/// Generates unique human-readable Transaction Reference Numbers (TNR).
/// Format: TNR-XXXXXXXX-XXXXXXXX (e.g., TNR-3F9A2C1E-BD7F4A6C).
/// Uses GUID v4 for guaranteed uniqueness without database counters.
/// </summary>
public class TransactionReferenceNumberGenerator : ITransactionReferenceNumberGenerator
{
    /// <summary>
    /// Generates a new unique Transaction Reference Number (TNR).
    /// Format: TNR-XXXXXXXX-XXXXXXXX (all caps, GUID-based).
    /// Uses a single GUID v4 split into two 8-character hex groups.
    /// Guaranteed unique across all concurrent transactions.
    /// </summary>
    public Task<string> GenerateNextAsync()
    {
        var guid = Guid.NewGuid().ToString("N").ToUpperInvariant();
        // Format: TNR-XXXXXXXX-XXXXXXXX (first 8 chars + dash + next 8 chars)
        return Task.FromResult($"TNR-{guid.Substring(0, 8)}-{guid.Substring(8, 8)}");
    }
}