using System.ComponentModel.DataAnnotations;

namespace TransitPay.API.DTOs.Payment;

/// <summary>
/// Request DTO for processing a fare payment.
/// The server determines the fare from the FareRules table based on the route.
/// </summary>
public class PaymentRequest
{
    /// <summary>The passenger's transit card ID.</summary>
    [Required(ErrorMessage = "Card ID is required.")]
    public int CardId { get; set; }

    /// <summary>The boarding terminal ID.</summary>
    [Required(ErrorMessage = "Origin terminal ID is required.")]
    public int OriginTerminalId { get; set; }

    /// <summary>The alighting terminal ID.</summary>
    [Required(ErrorMessage = "Destination terminal ID is required.")]
    public int DestinationTerminalId { get; set; }

    // Note: Amount field REMOVED for security - server always determines fare from FareRules table
}