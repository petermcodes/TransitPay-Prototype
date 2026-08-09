using System.ComponentModel.DataAnnotations;

namespace TransitPay.API.DTOs.Payment;

/// <summary>
/// Request DTO for scanning a physical TransitPay card.
/// The driver enters the card number. The destination is read from the passenger's active trip plan.
/// </summary>
public class ScanPhysicalCardRequest
{
    /// <summary>
    /// The passenger's physical card number.
    /// </summary>
    [Required(ErrorMessage = "Card number is required.")]
    public string CardNumber { get; set; } = string.Empty;
}