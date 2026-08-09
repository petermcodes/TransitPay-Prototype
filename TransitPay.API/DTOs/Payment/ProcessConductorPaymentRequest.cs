using System.ComponentModel.DataAnnotations;

namespace TransitPay.API.DTOs.Payment;

/// <summary>
/// Request DTO for conductor-initiated payment processing.
/// The driver scans the passenger's QR code. The destination is read from the passenger's active trip plan.
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
}