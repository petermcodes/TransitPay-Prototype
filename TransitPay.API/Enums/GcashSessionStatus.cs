namespace TransitPay.API.Enums;

/// <summary>
/// Lifecycle status of a simulated GCash top-up checkout session.
/// Mirrors the states a real payment-gateway checkout session transitions through:
/// a session is created PENDING, then either COMPLETED (wallet credited) or
/// terminated as FAILED (wrong OTP), CANCELLED (user backed out) or EXPIRED (timeout).
/// </summary>
public enum GcashSessionStatus
{
    /// <summary>The checkout session is open and awaiting payment confirmation.</summary>
    PENDING,

    /// <summary>The payment succeeded and the wallet was credited.</summary>
    COMPLETED,

    /// <summary>The payment failed (e.g., too many incorrect OTP attempts).</summary>
    FAILED,

    /// <summary>The user cancelled the payment before completing it.</summary>
    CANCELLED,

    /// <summary>The session timed out before payment was confirmed.</summary>
    EXPIRED
}
