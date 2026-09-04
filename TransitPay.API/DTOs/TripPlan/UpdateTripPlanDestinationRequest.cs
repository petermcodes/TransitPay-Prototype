using System.ComponentModel.DataAnnotations;

namespace TransitPay.API.DTOs.TripPlan;

/// <summary>
/// Request DTO for changing the destination of an active trip plan.
/// The server re-calculates and re-snapshots the fare for the new route.
/// </summary>
public class UpdateTripPlanDestinationRequest
{
    /// <summary>The new alighting terminal ID.</summary>
    [Required(ErrorMessage = "New destination terminal ID is required.")]
    [Range(1, int.MaxValue, ErrorMessage = "Invalid destination terminal ID.")]
    public int NewDestinationTerminalId { get; set; }
}