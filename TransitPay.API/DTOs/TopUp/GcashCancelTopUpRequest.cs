using System.ComponentModel.DataAnnotations;

namespace TransitPay.API.DTOs.TopUp;

/// <summary>
/// Request DTO for cancelling a simulated GCash top-up checkout session.
/// </summary>
public class GcashCancelTopUpRequest
{
    /// <summary>The checkout session (payment intent) to cancel.</summary>
    [Required(ErrorMessage = "Session ID is required.")]
    public Guid SessionId { get; set; }
}
