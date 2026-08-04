namespace TransitPay.API.DTOs.Payment;

/// <summary>
/// Response DTO for payment operations.
/// Contains the payment receipt with fare details and updated balance.
/// </summary>
public class PaymentResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public PaymentData? Data { get; set; }
}

/// <summary>
/// Payment receipt data returned to the driver after a successful payment.
/// </summary>
public class PaymentData
{
    public Guid PaymentSessionId { get; set; }
    public int CardId { get; set; }

    /// <summary>
    /// The passenger's name (if permitted by business rules).
    /// </summary>
    public string? PassengerName { get; set; }

    /// <summary>
    /// The card number, masked for display (e.g., "•••• 4821").
    /// </summary>
    public string? MaskedCardNumber { get; set; }

    public int OriginStationId { get; set; }
    public int DestinationStationId { get; set; }
    public string? OriginStationName { get; set; }
    public string? DestinationStationName { get; set; }

    /// <summary>
    /// The locked fare charged for this payment (from the Payment Session).
    /// </summary>
    public decimal LockedFare { get; set; }

    /// <summary>
    /// The remaining wallet balance after payment.
    /// </summary>
    public decimal RemainingBalance { get; set; }

    /// <summary>
    /// Unique human-readable Transaction Reference Number (TRN).
    /// Format: TRN-YYYYMMDD-XXXXXX.
    /// </summary>
    public string? TransactionReferenceNumber { get; set; }

    /// <summary>
    /// The timestamp when the payment was completed.
    /// </summary>
    public DateTime PaymentTimestamp { get; set; }

    /// <summary>
    /// The driver (User) who processed this payment.
    /// </summary>
    public int? DriverId { get; set; }

    /// <summary>
    /// The fare rule ID that was applied for this payment.
    /// Links to the FareRule used for audit/historical purposes.
    /// </summary>
    public int? FareId { get; set; }

    /// <summary>
    /// Human-readable transaction name (e.g., "Fare payment: Central → Airport").
    /// </summary>
    public string? TransactionName { get; set; }
}