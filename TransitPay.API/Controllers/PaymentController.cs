using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TransitPay.API.DTOs.Payment;
using TransitPay.API.Interfaces;

namespace TransitPay.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PaymentController : ControllerBase
{
    private readonly IPaymentService _paymentService;
    private readonly IPaymentSessionService _paymentSessionService;
    private readonly IQRService _qrService;
    private readonly ILogger<PaymentController> _logger;

    public PaymentController(
        IPaymentService paymentService,
        IPaymentSessionService paymentSessionService,
        IQRService qrService,
        ILogger<PaymentController> logger)
    {
        _paymentService = paymentService;
        _paymentSessionService = paymentSessionService;
        _qrService = qrService;
        _logger = logger;
    }

    /// <summary>
    /// Creates or updates a PENDING payment session for a passenger's selected route.
    /// The backend determines and locks the fare from the FareRules table.
    /// </summary>
    [HttpPost("session")]
    public async Task<IActionResult> CreateOrUpdateSession([FromBody] CreatePaymentSessionRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new { success = false, message = "Validation failed.", errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage) });
        }

        // Extract the authenticated user's ID from the JWT claims
        var userId = GetUserIdFromClaims();
        if (userId == null)
        {
            return Unauthorized(new { success = false, message = "User not authenticated." });
        }

        var result = await _paymentSessionService.CreateOrUpdateSessionAsync(
            request.CardId, userId.Value, request.OriginStationId, request.DestinationStationId);

        if (!result.Success)
        {
            return BadRequest(result);
        }
        return Ok(result);
    }

    /// <summary>
    /// Retrieves the active PENDING payment session for a card.
    /// </summary>
    [HttpGet("session/{cardId}")]
    public async Task<IActionResult> GetActiveSession(int cardId)
    {
        var result = await _paymentSessionService.GetActiveSessionAsync(cardId);
        if (result == null)
        {
            return NotFound(new { success = false, message = "No active payment session found." });
        }
        return Ok(result);
    }

    /// <summary>
    /// Generates a permanent QR code for a card, or retrieves the existing one.
    /// The QR permanently identifies the passenger's TransitPay Card.
    /// </summary>
    [HttpPost("qr")]
    public async Task<IActionResult> GenerateQR([FromBody] GenerateQRRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new { success = false, message = "Validation failed." });
        }

        try
        {
            var ticket = await _qrService.GenerateOrRetrieveQRAsync(request.CardId);
            return Ok(new { success = true, data = ticket });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning("QR generation failed: {Message}", ex.Message);
            return BadRequest(new { success = false, message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating QR for card {CardId}", request.CardId);
            return StatusCode(500, new { success = false, message = "Error generating QR code." });
        }
    }

    /// <summary>
    /// Retrieves the current active QR code for a card.
    /// </summary>
    [HttpGet("qr/{cardId}")]
    public async Task<IActionResult> GetQR(int cardId)
    {
        try
        {
            var ticket = await _qrService.GetQRAsync(cardId);
            if (ticket == null)
            {
                return NotFound(new { success = false, message = "No active QR code found for this card." });
            }
            return Ok(new { success = true, data = ticket });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving QR for card {CardId}", cardId);
            return StatusCode(500, new { success = false, message = "Error retrieving QR code." });
        }
    }

    /// <summary>
    /// Regenerates a QR code for a card (admin only).
    /// Revokes the old QR and creates a new one.
    /// </summary>
    [HttpPost("qr/{cardId}/regenerate")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> RegenerateQR(int cardId)
    {
        try
        {
            var ticket = await _qrService.RegenerateQRAsync(cardId);
            return Ok(new { success = true, message = "QR code regenerated successfully.", data = ticket });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning("QR regeneration failed: {Message}", ex.Message);
            return BadRequest(new { success = false, message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error regenerating QR for card {CardId}", cardId);
            return StatusCode(500, new { success = false, message = "Error regenerating QR code." });
        }
    }

    /// <summary>
    /// Scans a passenger's permanent QR code and processes the payment.
    /// The backend retrieves the active Payment Session, validates the card/wallet/route,
    /// deducts the locked fare, records the transaction, and marks the session COMPLETED.
    /// </summary>
    [HttpPost("scan")]
    [Authorize(Roles = "Driver,Admin")]
    public async Task<IActionResult> ScanQR([FromBody] ScanQRRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new { success = false, message = "Validation failed." });
        }

        // Extract the authenticated driver's ID from the JWT claims
        var driverId = GetUserIdFromClaims();
        if (driverId == null)
        {
            return Unauthorized(new { success = false, message = "Driver not authenticated." });
        }

        try
        {
            var result = await _paymentService.ProcessQRPaymentAsync(
                request.QRData,
                request.Signature,
                driverId.Value);

            if (!result.Success)
            {
                return BadRequest(result);
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error scanning QR code");
            return StatusCode(500, new { success = false, message = "Error processing QR code." });
        }
    }

    /// <summary>
    /// Processes a conductor-initiated payment where the driver scans the QR code
    /// and selects the destination. The backend calculates the fare based on the
    /// active trip's origin, the selected destination, and the card's passenger type.
    /// </summary>
    [HttpPost("process-conductor")]
    [Authorize(Roles = "Driver,Admin")]
    public async Task<IActionResult> ProcessConductorPayment([FromBody] ProcessConductorPaymentRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new { success = false, message = "Validation failed.", errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage) });
        }

        // Extract the authenticated driver's ID from the JWT claims
        var driverId = GetUserIdFromClaims();
        if (driverId == null)
        {
            return Unauthorized(new { success = false, message = "Driver not authenticated." });
        }

        try
        {
            var result = await _paymentService.ProcessConductorPaymentAsync(
                request.QRData,
                request.Signature,
                driverId.Value,
                request.DestinationStationId);

            if (!result.Success)
            {
                return BadRequest(result);
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing conductor payment for driver {DriverId}", driverId);
            return StatusCode(500, new { success = false, message = "Error processing payment." });
        }
    }

    /// <summary>
    /// Extracts the authenticated user's ID from the JWT claims.
    /// </summary>
    private int? GetUserIdFromClaims()
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub")
            ?? User.FindFirstValue("userId");

        if (int.TryParse(userIdClaim, out var userId))
        {
            return userId;
        }

        return null;
    }
}

/// <summary>
/// Request DTO for generating a QR code.
/// </summary>
public class GenerateQRRequest
{
    [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "Card ID is required.")]
    public int CardId { get; set; }
}