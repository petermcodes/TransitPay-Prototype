namespace TransitPay.API.DTOs.TripPlan;

/// <summary>
/// Response DTO representing a passenger's trip plan as returned to the client.
/// Mirrors the snapshotted fare breakdown stored on the plan.
/// </summary>
public class TripPlanResponse
{
    /// <summary>The plan's unique ID.</summary>
    public int PlanId { get; set; }

    /// <summary>The transit card the plan is bound to.</summary>
    public int CardId { get; set; }

    /// <summary>The planned boarding terminal ID.</summary>
    public int OriginTerminalId { get; set; }

    /// <summary>The boarding terminal's display name.</summary>
    public string OriginTerminalName { get; set; } = string.Empty;

    /// <summary>The planned alighting terminal ID.</summary>
    public int DestinationTerminalId { get; set; }

    /// <summary>The alighting terminal's display name.</summary>
    public string DestinationTerminalName { get; set; } = string.Empty;

    /// <summary>Plan state: "Active", "Cancelled", or "Used".</summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>When the plan was created.</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>When the plan expires (24h after creation/update).</summary>
    public DateTime? ExpiresAt { get; set; }

    /// <summary>When the plan was consumed by a payment, if paid.</summary>
    public DateTime? UsedAt { get; set; }

    /// <summary>The base fare locked in for the route.</summary>
    public decimal NormalFare { get; set; }

    /// <summary>The discount amount locked in, or null when no discount applies.</summary>
    public decimal? DiscountAmount { get; set; }

    /// <summary>The snapshotted discount percentage, or null.</summary>
    public decimal? DiscountPercentage { get; set; }

    /// <summary>The final fare the passenger will be charged.</summary>
    public decimal FinalFarePrice { get; set; }
}
