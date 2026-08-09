using System.Security.Cryptography;
using System.Text;

namespace TransitPay.API.Utilities;

/// <summary>
/// Provides PII-minimized hashing for audit logs.
/// Returns a stable SHA-256 hex digest of the input so events can be
/// correlated without storing sensitive plaintext values.
/// </summary>
public static class PiiHasher
{
    public static string Sha256Hex(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return string.Empty;
        }

        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}