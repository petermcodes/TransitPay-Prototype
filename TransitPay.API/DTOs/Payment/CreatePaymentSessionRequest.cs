using System.ComponentModel.DataAnnotations;

namespace TransitPay.API.DTOs.Payment;

/// <summary>
/// Request DTO for creating or updating a pending payment session.
/// The server determines the locked fare from the FareRules table.
/// </summary>
public class CreatePaymentSessionRequest
{
    [Required(ErrorMessage = "Card ID is required.")]
    public int CardId { get; set; }

    [Required(ErrorMessage = "Origin station ID is required.")]
    public int OriginStationId { get; set; }

    [Required(ErrorMessage = "Destination station ID is required.")]
    public int DestinationStationId { get; set; }
}