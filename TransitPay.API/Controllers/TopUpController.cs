using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TransitPay.API.DTOs.TopUp;
using TransitPay.API.Interfaces;
using TransitPay.API.Utilities;

namespace TransitPay.API.Controllers;

/// <summary>
/// Passenger self-service wallet top-up via the simulated GCash gateway ("sandbox").
/// Lifecycle: initiate (creates a PENDING TOP_UP transaction + checkout session)
/// → confirm (validates the simulated OTP, credits the wallet) → receipt.
/// Cancel and status endpoints support backing out and polling. Every operation is
/// scoped to the authenticated user's own card. No real money moves while the
/// simulation is in place — swap GcashTopUpService for a real PSP implementation to go live.
/// </summary>
[ApiController]
[Route("api/topup/gcash")]
[Authorize]
public class TopUpController : ControllerBase
{
    private readonly IGcashTopUpService _gcashTopUpService;
    private readonly ILogger<TopUpController> _logger;

    /// <summary>
    /// Creates a new TopUpController.
    /// </summary>
    public TopUpController(IGcashTopUpService gcashTopUpService, ILogger<TopUpController> logger)
    {
        _gcashTopUpService = gcashTopUpService;
        _logger = logger;
    }

    /// <summary>
    /// Starts a simulated GCash top-up: creates a PENDING transaction and a checkout
    /// session (payment intent) for the authenticated passenger's card.
    /// </summary>
    [HttpPost("initiate")]
    public async Task<IActionResult> Initiate([FromBody] GcashInitiateTopUpRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new { success = false, message = "Validation failed.", errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage) });
        }

        var userId = User.GetAuthenticatedUserId();
        if (userId == null)
        {
            return Unauthorized(new { success = false, message = "User not authenticated." });
        }

        try
        {
            var session = await _gcashTopUpService.InitiateAsync(request.CardId, request.Amount, userId.Value);
            return Ok(new { success = true, message = "GCash top-up session created.", data = session });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initiating GCash top-up for card {CardId}", request.CardId);
            return StatusCode(500, new { success = false, message = "Error creating GCash payment session." });
        }
    }

    /// <summary>
    /// Confirms the simulated GCash payment with the checkout OTP. Wrong OTPs allow
    /// retries (3 attempts max); a correct OTP credits the wallet atomically and
    /// completes the transaction.
    /// </summary>
    [HttpPost("confirm")]
    public async Task<IActionResult> Confirm([FromBody] GcashConfirmTopUpRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new { success = false, message = "Validation failed.", errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage) });
        }

        var userId = User.GetAuthenticatedUserId();
        if (userId == null)
        {
            return Unauthorized(new { success = false, message = "User not authenticated." });
        }

        try
        {
            var result = await _gcashTopUpService.ConfirmAsync(request.SessionId, request.Otp, userId.Value);
            return Ok(new { success = result.Success, message = result.Message, data = result });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error confirming GCash top-up session {SessionId}", request.SessionId);
            return StatusCode(500, new { success = false, message = "Error confirming GCash payment." });
        }
    }

    /// <summary>
    /// Cancels a pending simulated GCash payment (transaction → CANCELLED, no balance change).
    /// </summary>
    [HttpPost("cancel")]
    public async Task<IActionResult> Cancel([FromBody] GcashCancelTopUpRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new { success = false, message = "Validation failed.", errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage) });
        }

        var userId = User.GetAuthenticatedUserId();
        if (userId == null)
        {
            return Unauthorized(new { success = false, message = "User not authenticated." });
        }

        try
        {
            var session = await _gcashTopUpService.CancelAsync(request.SessionId, userId.Value);
            return Ok(new { success = true, message = "GCash top-up session cancelled.", data = session });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling GCash top-up session {SessionId}", request.SessionId);
            return StatusCode(500, new { success = false, message = "Error cancelling GCash payment." });
        }
    }

    /// <summary>
    /// Returns the current state of a checkout session (used for polling/refresh).
    /// Stale pending sessions are lazily expired when polled.
    /// </summary>
    [HttpGet("status/{sessionId:guid}")]
    public async Task<IActionResult> GetStatus(Guid sessionId)
    {
        var userId = User.GetAuthenticatedUserId();
        if (userId == null)
        {
            return Unauthorized(new { success = false, message = "User not authenticated." });
        }

        try
        {
            var session = await _gcashTopUpService.GetStatusAsync(sessionId, userId.Value);
            if (session == null)
            {
                return NotFound(new { success = false, message = "Top-up session not found." });
            }

            return Ok(new { success = true, message = "Session status retrieved.", data = session });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving GCash top-up session status {SessionId}", sessionId);
            return StatusCode(500, new { success = false, message = "Error retrieving payment status." });
        }
    }
}
