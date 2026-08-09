namespace TransitPay.API.Utilities;

/// <summary>
/// Centralized card number formatting utilities.
/// Single source of truth for card number masking across Cards, Wallets, Payments,
/// Admin Dashboard, Reports, and future APIs.
/// </summary>
public static class CardFormatter
{
    /// <summary>
    /// Masks a card number, preserving only the last four digits.
    /// </summary>
    /// <param name="cardNumber">The full card number to mask.</param>
    /// <returns>
    /// The masked card number (e.g., "•••• 4821"), or the input unchanged
    /// when it is null, empty, whitespace, or shorter than four digits.
    /// </returns>
    public static string? MaskCardNumber(string? cardNumber)
    {
        if (string.IsNullOrWhiteSpace(cardNumber) || cardNumber.Length < 4)
        {
            return cardNumber;
        }

        return $"•••• {cardNumber[^4..]}";
    }
}