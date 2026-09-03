using System.ComponentModel.DataAnnotations;

namespace TransitPay.API.DTOs.Payment;

/// <summary>
/// Request DTO for generating or retrieving a card's permanent QR code.
/// </summary>
public class GenerateQRRequest
{
    /// <summary>The transit card ID whose QR code is requested.</summary>
    [Required(ErrorMessage = "Card ID is required.")]
    public int CardId { get; set; }
}