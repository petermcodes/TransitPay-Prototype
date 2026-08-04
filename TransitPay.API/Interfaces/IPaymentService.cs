using TransitPay.API.DTOs.Payment;

namespace TransitPay.API.Interfaces;

/// <summary>
/// Service for processing transit fare payments via payment sessions.
/// The fare is locked in the session at creation time and charged during driver scan.
/// All operations are wrapped in a database transaction for atomicity.
/// </summary>
public interface IPaymentService
{
    /// <summary>
    /// Processes a QR-based payment by validating the QR code, identifying the card,
    /// retrieving the active payment session, and processing the payment.
    /// </summary>
    /// <param name="qrData">The base64-encoded QR payload.</param>
    /// <param name="signature">The HMAC signature of the QR payload.</param>
    /// <param name="driverId">The authenticated driver's user ID.</param>
    /// <returns>Payment response with receipt data.</returns>
    Task<PaymentResponse> ProcessQRPaymentAsync(string qrData, string signature, int driverId);

    /// <summary>
    /// Processes a conductor-initiated payment where the driver scans the QR code
    /// and selects the destination. The backend calculates the fare based on the
    /// active trip's origin, the selected destination, and the card's passenger type.
    /// </summary>
    /// <param name="qrData">The base64-encoded QR payload.</param>
    /// <param name="signature">The HMAC signature of the QR payload.</param>
    /// <param name="driverId">The authenticated driver's user ID.</param>
    /// <param name="destinationStationId">The destination station selected by the driver.</param>
    /// <returns>Payment response with receipt data.</returns>
    Task<PaymentResponse> ProcessConductorPaymentAsync(string qrData, string signature, int driverId, int destinationStationId);
}
