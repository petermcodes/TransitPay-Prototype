namespace TransitPay.API.DTOs.TripPlan;

public class TripPlanResponse
{
    public int PlanId { get; set; }
    public int CardId { get; set; }
    public int OriginTerminalId { get; set; }
    public string OriginTerminalName { get; set; } = string.Empty;
    public int DestinationTerminalId { get; set; }
    public string DestinationTerminalName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public DateTime? UsedAt { get; set; }
    public decimal NormalFare { get; set; }
    public decimal? DiscountAmount { get; set; }
    public decimal? DiscountPercentage { get; set; }
    public decimal FinalFarePrice { get; set; }
}
