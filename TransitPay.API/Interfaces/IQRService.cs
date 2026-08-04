using TransitPay.API.DTOs.Payment;

namespace TransitPay.API.Interfaces;

/// <summary>
/// Centralized service for QR code generation, retrieval, regeneration, and validation.
/// QR codes are permanently associated with transit cards and digitally signed
/// to prevent tampering.
/// </summary>
public interface IQRService
{
    /// <summary>
    /// Generates a new QR code for the card if none exists, or retrieves the existing active QR.
    /// </summary>
    Task<QRTicketResponse> GenerateOrRetrieveQRAsync(int cardId);

    /// <summary>
    /// Retrieves the current active QR code for the card.
    /// Returns null if no active QR exists.
    /// </summary>
    Task<QRTicketResponse?> GetQRAsync(int cardId);

    /// <summary>
    /// Revokes the existing QR and generates a new one.
    /// Used for security purposes (e.g., compromised QR).
    /// </summary>
    Task<QRTicketResponse> RegenerateQRAsync(int cardId);

    /// <summary>
    /// Validates a QR code's signature and looks up the associated card.
    /// Returns the card ID if valid, throws if invalid/expired/inactive.
    /// </summary>
    Task<int> ValidateQRAsync(string qrData, string signature);
}