using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TransitPay.API.Data;
using TransitPay.API.DTOs.Payment;
using TransitPay.API.Enums;
using TransitPay.API.Interfaces;
using TransitPay.API.Utilities;

namespace TransitPay.API.Controllers;

/// <summary>
/// Payment controller for conductor-initiated payments.
/// Canonical flow: TripPlan-based (passenger creates plan, driver scans QR, payment processed).
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PaymentController : ControllerBase
{
    private readonly IPaymentService _paymentService;
    private readonly IQRService _qrService;
    private readonly TransitPayDbContext _dbContext;
    private readonly ILogger<PaymentController> _logger;

    /// <summary>
    /// Creates a new PaymentController.
    /// </summary>
    public PaymentController(
        IPaymentService paymentService,
        IQRService qrService,
        TransitPayDbContext dbContext,
        ILogger<PaymentController> logger)
    {
        _paymentService = paymentService;
        _qrService = qrService;
        _dbContext = dbContext;
        _logger = logger;
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

        // Ownership validation: the card must belong to the authenticated user
        var canAccess = await CanAccessCardAsync(request.CardId);
        if (!canAccess)
        {
            return NotFound(new { success = false, message = "Card not found." });
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
        // Ownership validation: the card must belong to the authenticated user,
        // or the caller must be an Admin or Driver (who can scan/verify QR).
        var canAccess = await CanAccessCardAsync(cardId);
        if (!canAccess)
        {
            return NotFound(new { success = false, message = "Card not found." });
        }

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
        var driverId = User.GetAuthenticatedUserId();
        if (driverId == null)
        {
            return Unauthorized(new { success = false, message = "Driver not authenticated." });
        }

        try
        {
            var result = await _paymentService.ProcessConductorPaymentAsync(
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
            _logger.LogError(ex, "Error processing conductor payment for driver {DriverId}", driverId);
            return StatusCode(500, new { success = false, message = "Error processing payment." });
        }
    }

    /// <summary>
    /// Processes a conductor-initiated physical card payment where the driver enters
    /// the card number and selects the destination. The backend calculates the fare
    /// based on the active trip's current boarding origin, the selected destination,
    /// and the card's passenger type.
    /// </summary>
    [HttpPost("scan-physical")]
    [Authorize(Roles = "Driver,Admin")]
    public async Task<IActionResult> ScanPhysicalCard([FromBody] ScanPhysicalCardRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new { success = false, message = "Validation failed.", errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage) });
        }

        // Extract the authenticated driver's ID from the JWT claims
        var driverId = User.GetAuthenticatedUserId();
        if (driverId == null)
        {
            return Unauthorized(new { success = false, message = "Driver not authenticated." });
        }

        try
        {
            var result = await _paymentService.ProcessConductorPhysicalCardPaymentAsync(
                request.CardNumber,
                driverId.Value);

            if (!result.Success)
            {
                return BadRequest(result);
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing physical card payment for driver {DriverId}", driverId);
            return StatusCode(500, new { success = false, message = "Error processing payment." });
        }
    }

    /// <summary>
    /// Determines whether the authenticated user can access a specific card.
    /// Owners may access their own cards. Admins and Drivers may access any card.
    /// </summary>
    private async Task<bool> CanAccessCardAsync(int cardId)
    {
        var userId = User.GetAuthenticatedUserId();
        if (userId == null)
        {
            return false;
        }

        var isAdmin = User.IsInRole(nameof(RoleName.Admin));
        var isDriver = User.IsInRole(nameof(RoleName.Driver));

        if (isAdmin || isDriver)
        {
            return true;
        }

        var card = await _dbContext.Cards
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.CardId == cardId);

        return card != null && card.UserId == userId.Value;
    }
}

/// <summary>
/// Request DTO for generating a QR code.
/// </summary>
public class GenerateQRRequest
{
    /// <summary>The transit card ID whose QR code is requested.</summary>
    [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "Card ID is required.")]
    public int CardId { get; set; }
}