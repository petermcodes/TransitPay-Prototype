namespace TransitPay.API.Enums;

/// <summary>
/// The passenger category attached to a card. Used for fare rule lookup so the
/// correct fare (and discount eligibility) applies for each passenger type.
/// </summary>
public enum PassengerType
{
    /// <summary>A regular (full-fare) passenger.</summary>
    Passenger,

    /// <summary>A student passenger (discount-eligible).</summary>
    Student,

    /// <summary>A senior citizen passenger (discount-eligible).</summary>
    Senior,

    /// <summary>A person with disability (discount-eligible).</summary>
    DISABLED
}