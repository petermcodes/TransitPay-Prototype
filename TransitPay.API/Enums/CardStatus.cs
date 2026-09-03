namespace TransitPay.API.Enums;

/// <summary>
/// Lifecycle status of a transit card and its associated wallet.
/// A card must be ACTIVE to generate/validate a QR code and to process payments or top-ups.
/// </summary>
public enum CardStatus
{
    /// <summary>The card is active and can be used for payments and top-ups.</summary>
    ACTIVE,

    /// <summary>The card is inactive and cannot be used.</summary>
    INACTIVE,

    /// <summary>The card is temporarily suspended (e.g., reported lost/compromised).</summary>
    SUSPENDED,

    /// <summary>The card has passed its expiry date.</summary>
    EXPIRED
}