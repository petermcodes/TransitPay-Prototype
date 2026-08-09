using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TransitPay.API.DTOs.Card;
using TransitPay.API.DTOs.Common;
using TransitPay.API.Enums;
using TransitPay.API.Exceptions;
using TransitPay.API.Interfaces;
using TransitPay.API.Utilities;

namespace TransitPay.API.Controllers;

/// <summary>
/// Manages Transit Card lookup, creation, and validation.
/// Legacy endpoints (POST, GET /{cardNumber}, GET /validate/{cardNumber}) have been
/// migrated to use ICardService — no direct DbContext access remains.
/// New endpoints (GET /me, GET /user/{userId}) follow Clean Architecture.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CardsController : ControllerBase
{
    private readonly ICardService _cardService;

    public CardsController(ICardService cardService)
    {
        _cardService = cardService;
    }

    /// <summary>
    /// Creates a new Transit Card and its associated Wallet (Admin only).
    /// </summary>
    /// <param name="request">The card creation request.</param>
    /// <returns>The created card with its details.</returns>
    /// <response code="200">Card created successfully.</response>
    /// <response code="400">Validation failed or card number already exists.</response>
    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ApiResponse<CardCreatedDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<CardCreatedDto>>> CreateCard([FromBody] CardRequestDto request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new { success = false, message = "Validation failed.", errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage) });
        }

        try
        {
            var card = await _cardService.CreateCardAsync(request, HttpContext.RequestAborted);
            return Ok(ApiResponse<CardCreatedDto>.SuccessResponse(card, "Card created successfully."));
        }
        catch (DuplicateCardException)
        {
            return BadRequest(ApiResponse<CardCreatedDto>.ErrorResponse("Card number already exists."));
        }
    }

    /// <summary>
    /// Retrieves a card by its full card number.
    /// </summary>
    /// <param name="cardNumber">The 16-digit card number.</param>
    /// <returns>The card details.</returns>
    /// <response code="200">Card retrieved successfully.</response>
    /// <response code="404">Card not found.</response>
    [HttpGet("{cardNumber}")]
    [Authorize(Roles = "Driver,Admin")]
    [ProducesResponseType(typeof(ApiResponse<CardDetailsDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<CardDetailsDto>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<CardDetailsDto>>> GetCard(string cardNumber)
    {
        var card = await _cardService.GetCardByNumberAsync(cardNumber, HttpContext.RequestAborted);
        if (card == null)
        {
            return NotFound(ApiResponse<CardDetailsDto>.ErrorResponse("Card not found."));
        }

        // The service already returns a masked DTO, so return it directly.
        return Ok(ApiResponse<CardDetailsDto>.SuccessResponse(card, "Card retrieved successfully."));
    }

    /// <summary>
    /// Validates a card by its full card number and returns its status and wallet balance
    /// (Driver and Admin only).
    /// </summary>
    /// <param name="cardNumber">The 16-digit card number.</param>
    /// <returns>The card validation result with balance.</returns>
    /// <response code="200">Card is valid.</response>
    /// <response code="400">Card cannot be used (inactive status).</response>
    /// <response code="404">Card not found.</response>
    [HttpGet("validate/{cardNumber}")]
    [Authorize(Roles = "Driver,Admin")]
    [ProducesResponseType(typeof(ApiResponse<CardValidationDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<CardValidationDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<CardValidationDto>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<CardValidationDto>>> ValidateCard(string cardNumber)
    {
        var card = await _cardService.ValidateCardAsync(cardNumber, HttpContext.RequestAborted);
        if (card == null)
        {
            return NotFound(ApiResponse<CardValidationDto>.ErrorResponse("Card not found."));
        }

        if (card.Status != CardStatus.ACTIVE)
        {
            return BadRequest(ApiResponse<CardValidationDto>.ErrorResponse($"Card cannot be used. Status: {card.Status}"));
        }

        return Ok(ApiResponse<CardValidationDto>.SuccessResponse(card, "Card is valid."));
    }

    /// <summary>
    /// Retrieves the authenticated user's Transit Card.
    /// Does not accept a userId parameter — it only returns the caller's own card.
    /// </summary>
    /// <returns>The authenticated user's card (masked).</returns>
    /// <response code="200">Card retrieved successfully.</response>
    /// <response code="401">User not authenticated.</response>
    /// <response code="404">No Transit Card found for this user.</response>
    [HttpGet("me")]
    [ProducesResponseType(typeof(ApiResponse<CardDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<CardDto>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<CardDto>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<CardDto>>> GetMyCard()
    {
        var userId = User.GetAuthenticatedUserId();
        if (userId == null)
        {
            return Unauthorized(ApiResponse<CardDto>.ErrorResponse("User not authenticated."));
        }

        var card = await _cardService.GetCardByUserIdAsync(userId.Value, HttpContext.RequestAborted);
        if (card == null)
        {
            return NotFound(ApiResponse<CardDto>.ErrorResponse("No Transit Card found for this user."));
        }

        return Ok(ApiResponse<CardDto>.SuccessResponse(card, "Card retrieved successfully."));
    }

    /// <summary>
    /// Retrieves the Transit Card for a specific user (Admin only).
    /// </summary>
    /// <param name="userId">The user ID whose card to retrieve.</param>
    /// <returns>The specified user's card (masked).</returns>
    /// <response code="200">Card retrieved successfully.</response>
    /// <response code="401">User not authenticated.</response>
    /// <response code="403">User is not an admin.</response>
    /// <response code="404">No Transit Card found for the specified user.</response>
    [HttpGet("user/{userId}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ApiResponse<CardDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<CardDto>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<CardDto>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<CardDto>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<CardDto>>> GetCardByUser(int userId)
    {
        var card = await _cardService.GetCardByUserIdAsync(userId, HttpContext.RequestAborted);
        if (card == null)
        {
            return NotFound(ApiResponse<CardDto>.ErrorResponse("No Transit Card found for this user."));
        }

        return Ok(ApiResponse<CardDto>.SuccessResponse(card, "Card retrieved successfully."));
    }

}

/// <summary>
/// Legacy request DTO for creating a card. Kept for backward compatibility
/// with the POST /api/cards endpoint. The service now uses <see cref="CardRequestDto"/>.
/// </summary>
public class CreateCardRequest
{
    [Required(ErrorMessage = "Card number is required.")]
    [RegularExpression(@"^\d{16}$", ErrorMessage = "Card number must be 16 digits.")]
    public string CardNumber { get; set; } = string.Empty;

    public int? UserId { get; set; }
}