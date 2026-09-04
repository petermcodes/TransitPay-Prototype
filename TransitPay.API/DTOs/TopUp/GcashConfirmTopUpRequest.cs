using System.ComponentModel.DataAnnotations;

namespace TransitPay.API.DTOs.TopUp;

/// <summary>
/// Request DTO for confirming a simulated GCash top-up with the checkout OTP.
/// </summary>
public class GcashConfirmTopUpRequest
{
    /// <summary>The checkout session (payment intent) being confirmed.</summary>
    [Required(ErrorMessage = "Session ID is required.")]
    public Guid SessionId { get; set; }

    /// <summary>The (simulated) GCash authentication code entered by the user.</summary>
    [Required(ErrorMessage = "OTP code is required.")]
    public string Otp { get; set; } = string.Empty;
}
