namespace TransitPay.API.DTOs.Payment;

/// <summary>
/// Response DTO for QR code generation/retrieval endpoints.
/// Contains the signed QR payload that the frontend encodes into a QR image.
/// </summary>
public class QRTicketResponse
{
    /// <summary>
    /// Base64-encoded JSON payload containing the QR token and card ID.
    /// This is what gets encoded into the QR code image.
    /// </summary>
    public string Data { get; set; } = string.Empty;

    /// <summary>
    /// HMAC-SHA256 signature of the data payload.
    /// Used by the scan endpoint to verify the QR hasn't been tampered with.
    /// </summary>
    public string Signature { get; set; } = string.Empty;

    /// <summary>
    /// The card ID associated with this QR code.
    /// </summary>
    public int CardId { get; set; }

    /// <summary>
    /// The masked card number for display (e.g., "•••• 4821").
    /// The full card number is never included in the QR payload or response.
    /// </summary>
    public string? MaskedCardNumber { get; set; }
}