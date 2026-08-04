using System.ComponentModel.DataAnnotations;

namespace TransitPay.API.DTOs.Payment;

/// <summary>
/// Request DTO for conductor-initiated payment processing.
/// The driver scans the passenger's QR code and selects the destination.
/// </summary>
public class ProcessConductorPaymentRequest
{
    /// <summary>
    /// The base64-encoded QR payload from the passenger's QR code.
    /// </summary>
    [Required(ErrorMessage = "QR data is required.")]
    public string QRData { get; set; } = string.Empty;

    /// <summary>
    /// The HMAC signature of the QR payload for validation.
    /// </summary>
    [Required(ErrorMessage = "QR signature is required.")]
    public string Signature { get; set; } = string.Empty;

    /// <summary>
    /// The destination station ID selected by the conductor/driver.
    /// Backend will calculate the fare based on trip origin and this destination.
    /// </summary>
    [Required(ErrorMessage = "Destination station ID is required.")]
    [Range(1, int.MaxValue, ErrorMessage = "Invalid destination station ID.")]
    public int DestinationStationId { get; set; }
}