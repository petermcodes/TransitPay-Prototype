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
    public int CardId { get; set; }

    /// <summary>
    /// The passenger's name (if permitted by business rules).
    /// </summary>
    public string? PassengerName { get; set; }

    /// <summary>
    /// The card number, masked for display (e.g., "•••• 4821").
    /// </summary>
    public string? MaskedCardNumber { get; set; }

    public int OriginTerminalId { get; set; }
    public int DestinationTerminalId { get; set; }
    public string? OriginTerminalName { get; set; }
    public string? DestinationTerminalName { get; set; }

    /// <summary>
    /// The locked fare charged for this payment (from the Payment Session).
    /// For the conductor flow this equals the regular fare before discount.
    /// </summary>
    public decimal LockedFare { get; set; }

    /// <summary>
    /// The regular fare amount before any discount was applied.
    /// </summary>
    public decimal RegularFare { get; set; }

    /// <summary>
    /// The discount percentage applied (e.g., 50.00 for 50% off).
    /// Zero or null when no discount was applied.
    /// </summary>
    public decimal? DiscountPercentage { get; set; }

    /// <summary>
    /// The discount amount deducted from the regular fare.
    /// Zero or null when no discount was applied.
    /// </summary>
    public decimal? DiscountAmount { get; set; }

    /// <summary>
    /// The final fare amount actually charged after discount.
    /// </summary>
    public decimal FinalFare { get; set; }

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