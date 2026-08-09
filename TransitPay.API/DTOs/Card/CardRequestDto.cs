using System.ComponentModel.DataAnnotations;

namespace TransitPay.API.DTOs.Card;

/// <summary>
/// Request DTO for creating a Transit Card via <c>POST /api/cards</c>.
/// Uses DataAnnotations validation, consistent with the existing project
/// standard (FluentValidation is not used in this solution).
/// </summary>
public class CardRequestDto
{
    /// <summary>
    /// Gets or sets the 16-digit card number.
    /// </summary>
    [Required(ErrorMessage = "Card number is required.")]
    [RegularExpression(@"^\d{16}$", ErrorMessage = "Card number must be 16 digits.")]
    public string CardNumber { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the user ID the card belongs to, if any.
    /// </summary>
    public int? UserId { get; set; }
}