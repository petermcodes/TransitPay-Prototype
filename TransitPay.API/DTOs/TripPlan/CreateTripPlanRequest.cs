using System.ComponentModel.DataAnnotations;

namespace TransitPay.API.DTOs.TripPlan;

public class CreateTripPlanRequest
{
    [Required(ErrorMessage = "Origin terminal ID is required.")]
    [Range(1, int.MaxValue, ErrorMessage = "Invalid origin terminal ID.")]
    public int OriginTerminalId { get; set; }

    [Required(ErrorMessage = "Destination terminal ID is required.")]
    [Range(1, int.MaxValue, ErrorMessage = "Invalid destination terminal ID.")]
    public int DestinationTerminalId { get; set; }
}