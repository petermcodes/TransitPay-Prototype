using Microsoft.IdentityModel.Tokens;

namespace TransitPay.API.Interfaces;

/// <summary>
/// Centralized provider for cryptographic signing keys.
/// Both JWT token signing and QR code HMAC signing use this single source
/// to ensure key consistency across the application.
/// </summary>
public interface ISecurityKeyProvider
{
    /// <summary>
    /// Returns the raw signing key bytes used for HMAC operations (QR codes).
    /// </summary>
    byte[] GetSigningKeyBytes();

    /// <summary>
    /// Returns a SymmetricSecurityKey for JWT token signing/validation.
    /// </summary>
    SymmetricSecurityKey GetSymmetricSecurityKey();
}