namespace TransitPay.API.Configuration;

/// <summary>
/// Configuration settings for digital (online) wallet top-up payments.
/// Bound from the "Payments" section of appsettings.json.
/// The current implementation is a GCash <c>simulation</c> ("sandbox"): the flow
/// mirrors a real gateway checkout (intent → authentication → confirm → credit)
/// but no real money moves. When a real payment service provider (e.g., PayMongo,
/// Xendit) is integrated later, only <see cref="TransitPay.API.Services.GcashTopUpService"/>
/// needs replacing — controllers, DTOs and the frontend flow stay unchanged.
/// </summary>
public class PaymentSettings
{
    /// <summary>
    /// The GCash gateway settings (sandbox limits and session lifetime).
    /// </summary>
    public GcashSettings Gcash { get; set; } = new();
}

/// <summary>
/// Settings for the simulated GCash top-up gateway.
/// </summary>
public class GcashSettings
{
    /// <summary>Smallest amount that can be topped up in one transaction (peso).</summary>
    public decimal MinAmount { get; set; } = 1;

    /// <summary>Largest amount that can be topped up in one transaction (peso).</summary>
    public decimal MaxAmount { get; set; } = 10000;

    /// <summary>How long a checkout session stays open before it expires (minutes).</summary>
    public int SessionExpiryMinutes { get; set; } = 15;
}
