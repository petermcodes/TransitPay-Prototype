namespace TransitPay.API.Enums;

/// <summary>
/// Lifecycle status of a payment session.
/// PENDING → SCANNING → PROCESSING → COMPLETED
/// Alternative endings: FAILED, EXPIRED, CANCELLED
/// </summary>
public enum PaymentSessionStatus
{
    /// <summary>
    /// Passenger has selected a route and is waiting for the driver to scan the QR.
    /// </summary>
    PENDING,

    /// <summary>
    /// Driver's scan request has been received; backend is validating the QR and locating the active session.
    /// </summary>
    SCANNING,

    /// <summary>
    /// Backend is performing wallet validation, fare validation, deduction, transaction creation, and DB updates.
    /// </summary>
    PROCESSING,

    /// <summary>
    /// Payment finished successfully.
    /// </summary>
    COMPLETED,

    /// <summary>
    /// Processing failed due to validation errors, insufficient balance, inactive card, etc.
    /// </summary>
    FAILED,

    /// <summary>
    /// Session exceeded its allowed lifetime before payment.
    /// </summary>
    EXPIRED,

    /// <summary>
    /// Passenger cancelled the trip or the session was explicitly terminated.
    /// </summary>
    CANCELLED
}