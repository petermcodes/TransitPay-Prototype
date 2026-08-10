namespace TransitPay.API.Interfaces;

/// <summary>
/// Generates unique human-readable Transaction Reference Numbers (TRN).
/// Format: TRN-YYYYMMDD-XXXXXX (e.g., TRN-20260804-000001).
/// </summary>
public interface ITransactionReferenceNumberGenerator
{
    /// <summary>
    /// Generates the next available TRN for today's date.
    /// </summary>
    Task<string> GenerateNextAsync();
}