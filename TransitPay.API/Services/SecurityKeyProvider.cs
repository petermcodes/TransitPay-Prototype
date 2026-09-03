using System.Text;
using Microsoft.IdentityModel.Tokens;
using TransitPay.API.Interfaces;

namespace TransitPay.API.Services;

/// <summary>
/// Centralized security key provider that reads the JWT key from the
/// JWT_KEY environment variable. Throws at startup if the key is missing.
/// Both JWT signing and QR HMAC signing use this single source.
/// </summary>
public class SecurityKeyProvider : ISecurityKeyProvider
{
    private readonly byte[] _keyBytes;
    private readonly SymmetricSecurityKey _symmetricSecurityKey;

    /// <summary>
    /// Creates a new SecurityKeyProvider. Reads the JWT_KEY environment variable and
    /// fails fast at startup when it is missing.
    /// </summary>
    public SecurityKeyProvider(IConfiguration configuration, ILogger<SecurityKeyProvider> logger)
    {
        // Read the JWT key ONLY from the JWT_KEY environment variable.
        // No hardcoded fallbacks — fail fast if missing.
        var rawKey = Environment.GetEnvironmentVariable("JWT_KEY");
        if (string.IsNullOrWhiteSpace(rawKey))
        {
            logger.LogError("JWT_KEY environment variable is not set. Application cannot start without a valid signing key.");
            throw new InvalidOperationException(
                "JWT_KEY environment variable is not set. " +
                "Set it before starting the application (e.g., set JWT_KEY=your-secret-key-at-least-32-chars).");
        }

        if (rawKey.Length < 32)
        {
            logger.LogWarning("JWT_KEY is shorter than 32 characters. Consider using a longer key for production.");
        }

        _keyBytes = Encoding.UTF8.GetBytes(rawKey);
        _symmetricSecurityKey = new SymmetricSecurityKey(_keyBytes);

        logger.LogInformation("Security key provider initialized from JWT_KEY environment variable.");
    }

    /// <inheritdoc />
    public byte[] GetSigningKeyBytes()
    {
        return _keyBytes;
    }

    /// <inheritdoc />
    public SymmetricSecurityKey GetSymmetricSecurityKey()
    {
        return _symmetricSecurityKey;
    }
}