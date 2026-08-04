using System.ComponentModel.DataAnnotations;

namespace TransitPay.API.DTOs.Payment;

/// <summary>
/// Request DTO for scanning a passenger's permanent QR code.
/// The origin, destination, and fare come from the active Payment Session — not the driver.
/// </summary>
public class ScanQRRequest
{
    [Required(ErrorMessage = "QR data is required.")]
    public string QRData { get; set; } = string.Empty;

    [Required(ErrorMessage = "Signature is required.")]
    public string Signature { get; set; } = string.Empty;
}