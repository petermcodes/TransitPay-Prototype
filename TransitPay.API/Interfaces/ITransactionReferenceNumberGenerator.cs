namespace TransitPay.API.Interfaces;

/// <summary>
/// Generates unique human-readable Transaction Reference Numbers (TRN).
/// Format: TNR-XXXXXXXX-XXXXXXXX (e.g., TNR-3F9A2C1E-BD7F4A6C).
/// Uses GUID v4 for guaranteed uniqueness without database counters.
/// </summary>
public interface ITransactionReferenceNumberGenerator
{
    /// <summary>
    /// Generates a new unique Transaction Reference Number (TNR).
    /// Format: TNR-XXXXXXXX-XXXXXXXX (all caps, GUID-based).
    /// Example: TNR-3F9A2C1E-BD7F4A6C
    /// </summary>
    Task<string> GenerateNextAsync();
}