using TransitPay.API.Enums;

namespace TransitPay.API.DTOs.Payment;

/// <summary>
/// Response DTO for payment session operations.
/// Contains the locked fare and route details for passenger review.
/// </summary>
public class PaymentSessionResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public PaymentSessionData? Data { get; set; }
}

/// <summary>
/// Payment session data returned to the passenger after selecting a route.
/// </summary>
public class PaymentSessionData
{
    public Guid PaymentSessionId { get; set; }
    public int CardId { get; set; }
    public int UserId { get; set; }
    public int OriginStationId { get; set; }
    public int DestinationStationId { get; set; }
    public string? OriginStationName { get; set; }
    public string? DestinationStationName { get; set; }

    /// <summary>
    /// The locked fare for this session. This is what the passenger agreed to pay.
    /// </summary>
    public decimal LockedFare { get; set; }

    public PaymentSessionStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
}