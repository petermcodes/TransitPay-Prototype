using TransitPay.API.DTOs.Payment;

namespace TransitPay.API.Interfaces;

/// <summary>
/// Service for processing transit fare payments via payment sessions.
/// The fare is locked in the session at creation time and charged during driver scan.
/// All operations are wrapped in a database transaction for atomicity.
/// Duplicate processing is prevented via row locking and status transitions.
/// </summary>
public interface IPaymentService
{
    /// <summary>
    /// Processes a conductor-initiated QR payment where the driver scans the QR code.
    /// The destination is read from the passenger's active trip plan.
    /// The backend calculates the fare based on the active trip's current boarding origin,
    /// the trip plan destination, and the card's passenger type.
    /// </summary>
    /// <param name="qrData">The base64-encoded QR payload.</param>
    /// <param name="signature">The HMAC signature of the QR payload.</param>
    /// <param name="driverId">The authenticated driver's user ID.</param>
    /// <returns>Payment response with receipt data.</returns>
    Task<PaymentResponse> ProcessConductorPaymentAsync(
        string qrData, string signature, int driverId, int planId = 0);

    /// <summary>
    /// Processes a conductor-initiated physical card payment where the driver enters the card number.
    /// The destination is read from the passenger's active trip plan.
    /// The backend calculates the fare based on the active trip's current boarding origin,
    /// the trip plan destination, and the card's passenger type.
    /// </summary>
    /// <param name="cardNumber">The passenger's physical card number.</param>
    /// <param name="driverId">The authenticated driver's user ID.</param>
    /// <returns>Payment response with receipt data.</returns>
    Task<PaymentResponse> ProcessConductorPhysicalCardPaymentAsync(
        string cardNumber, int driverId);
}