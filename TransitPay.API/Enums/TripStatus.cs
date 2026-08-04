namespace TransitPay.API.Enums;

/// <summary>
/// Lifecycle status of a trip.
/// Pending → Active → Completed
/// Alternative endings: Cancelled
/// </summary>
public enum TripStatus
{
    /// <summary>
    /// Trip has been created but the bus has not yet departed.
    /// </summary>
    Pending = 0,

    /// <summary>
    /// Trip is currently in progress. Only one ACTIVE trip may exist per conductor/driver.
    /// </summary>
    Active = 1,

    /// <summary>
    /// Trip has arrived at the final destination.
    /// </summary>
    Completed = 2,

    /// <summary>
    /// Trip was cancelled before completion.
    /// </summary>
    Cancelled = 3
}