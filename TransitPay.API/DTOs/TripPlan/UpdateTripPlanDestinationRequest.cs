using System.ComponentModel.DataAnnotations;

namespace TransitPay.API.DTOs.TripPlan;

public class UpdateTripPlanDestinationRequest
{
    [Required(ErrorMessage = "New destination terminal ID is required.")]
    [Range(1, int.MaxValue, ErrorMessage = "Invalid destination terminal ID.")]
    public int NewDestinationTerminalId { get; set; }
}