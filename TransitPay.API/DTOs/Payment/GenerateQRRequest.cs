using System.ComponentModel.DataAnnotations;

namespace TransitPay.API.DTOs.Payment;

public class GenerateQRRequest
{
    [Required(ErrorMessage = "Card ID is required.")]
    public int CardId { get; set; }
}