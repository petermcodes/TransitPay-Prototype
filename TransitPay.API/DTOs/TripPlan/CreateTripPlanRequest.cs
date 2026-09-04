using System.ComponentModel.DataAnnotations;

namespace TransitPay.API.DTOs.TripPlan;

/// <summary>
/// Request DTO for creating a trip plan.
/// The server resolves the fare from the fare matrix and stores it on the plan.
/// </summary>
public class CreateTripPlanRequest
{
    /// <summary>The planned boarding terminal ID.</summary>
    [Required(ErrorMessage = "Origin terminal ID is required.")]
    [Range(1, int.MaxValue, ErrorMessage = "Invalid origin terminal ID.")]
    public int OriginTerminalId { get; set; }

    /// <summary>The planned alighting terminal ID.</summary>
    [Required(ErrorMessage = "Destination terminal ID is required.")]
    [Range(1, int.MaxValue, ErrorMessage = "Invalid destination terminal ID.")]
    public int DestinationTerminalId { get; set; }
}