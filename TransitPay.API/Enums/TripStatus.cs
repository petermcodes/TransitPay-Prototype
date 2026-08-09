namespace TransitPay.API.Enums;

/// <summary>
/// Lifecycle status of a trip.
/// Normal flow: Pending → Active → Completed
/// Alternative ending: Cancelled (from Pending or Active)
/// </summary>
public enum TripStatus
{
    /// <summary>
    /// Trip has been created but the bus has not yet departed.
    /// </summary>
    Pending = 0,

    /// <summary>
    /// Trip is currently in progress. Only one ACTIVE trip may exist per conductor/driver.
    /// Payments are only accepted while the trip is in this state.
    /// </summary>
    Active = 1,

    /// <summary>
    /// Trip has arrived at the final destination.
    /// No further payments are accepted.
    /// </summary>
    Completed = 2,

    /// <summary>
    /// Trip was cancelled before completion.
    /// Cannot be resumed.
    /// </summary>
    Cancelled = 3
}