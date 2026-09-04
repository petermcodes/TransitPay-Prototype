using System.ComponentModel.DataAnnotations;

namespace TransitPay.API.DTOs.TopUp;

/// <summary>
/// Request DTO for starting a simulated GCash top-up. The backend creates a
/// PENDING transaction and a checkout session; the amount is validated server-side.
/// </summary>
public class GcashInitiateTopUpRequest
{
    /// <summary>The card whose wallet will be credited.</summary>
    [Required(ErrorMessage = "Card ID is required.")]
    public int CardId { get; set; }

    /// <summary>The amount to top up (peso). Validated against the configured GCash limits.</summary>
    [Range(1, 100000, ErrorMessage = "Amount must be at least 1.")]
    public decimal Amount { get; set; }
}
