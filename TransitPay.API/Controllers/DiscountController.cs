using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TransitPay.API.Data;
using TransitPay.API.Enums;
using TransitPay.API.Interfaces;
using TransitPay.API.Models;
using TransitPay.API.Utilities;

namespace TransitPay.API.Controllers;

/// <summary>
/// Discount lifecycle endpoints (Admin CRUD + approval workflow, passenger applications).
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DiscountController : ControllerBase
{
    private readonly IDiscountService _discountService;
    private readonly TransitPayDbContext _dbContext;
    private readonly ILogger<DiscountController> _logger;

    /// <summary>
    /// Creates a new DiscountController.
    /// </summary>
    public DiscountController(IDiscountService discountService, TransitPayDbContext dbContext, ILogger<DiscountController> logger)
    {
        _discountService = discountService;
        _dbContext = dbContext;
        _logger = logger;
    }

    #region Discount Type Management (Admin)

    /// <summary>
    /// Creates a new discount type (Admin only).
    /// </summary>
    [HttpPost("types")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> CreateDiscountType([FromBody] CreateDiscountTypeRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new { success = false, message = "Validation failed.", errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage) });
        }

        try
        {
            var discountType = new DiscountType
            {
                Name = request.Name,
                Description = request.Description,
                DiscountPercentage = request.DiscountPercentage,
                RequiresApproval = request.RequiresApproval
            };

            var created = await _discountService.CreateDiscountTypeAsync(discountType);

            return Ok(new
            {
                success = true,
                message = "Discount type created successfully.",
                data = new
                {
                    created.DiscountTypeId,
                    created.Name,
                    created.Description,
                    created.DiscountPercentage,
                    created.IsActive,
                    created.RequiresApproval,
                    created.CreatedAt
                }
            });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning("Failed to create discount type: {Message}", ex.Message);
            return BadRequest(new { success = false, message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating discount type");
            return StatusCode(500, new { success = false, message = "An error occurred while creating the discount type." });
        }
    }

    /// <summary>
    /// Updates an existing discount type (Admin only).
    /// </summary>
    [HttpPut("types/{discountTypeId}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateDiscountType(int discountTypeId, [FromBody] UpdateDiscountTypeRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new { success = false, message = "Validation failed.", errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage) });
        }

        try
        {
            var discountType = new DiscountType
            {
                Name = request.Name,
                Description = request.Description,
                DiscountPercentage = request.DiscountPercentage,
                RequiresApproval = request.RequiresApproval
            };

            var updated = await _discountService.UpdateDiscountTypeAsync(discountTypeId, discountType);

            return Ok(new
            {
                success = true,
                message = "Discount type updated successfully.",
                data = new
                {
                    updated.DiscountTypeId,
                    updated.Name,
                    updated.Description,
                    updated.DiscountPercentage,
                    updated.IsActive,
                    updated.RequiresApproval,
                    updated.UpdatedAt
                }
            });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning("Failed to update discount type {DiscountTypeId}: {Message}", discountTypeId, ex.Message);
            return BadRequest(new { success = false, message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating discount type {DiscountTypeId}", discountTypeId);
            return StatusCode(500, new { success = false, message = "An error occurred while updating the discount type." });
        }
    }

    /// <summary>
    /// Deletes a discount type (Admin only). Soft delete.
    /// </summary>
    [HttpDelete("types/{discountTypeId}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteDiscountType(int discountTypeId)
    {
        try
        {
            var result = await _discountService.DeleteDiscountTypeAsync(discountTypeId);

            if (!result)
            {
                return NotFound(new { success = false, message = "Discount type not found." });
            }

            return Ok(new { success = true, message = "Discount type deleted successfully." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting discount type {DiscountTypeId}", discountTypeId);
            return StatusCode(500, new { success = false, message = "An error occurred while deleting the discount type." });
        }
    }

    /// <summary>
    /// Activates a discount type (Admin only).
    /// </summary>
    [HttpPost("types/{discountTypeId}/activate")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> ActivateDiscountType(int discountTypeId)
    {
        try
        {
            var result = await _discountService.ActivateDiscountTypeAsync(discountTypeId);

            return Ok(new { success = true, message = "Discount type activated successfully." });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning("Failed to activate discount type {DiscountTypeId}: {Message}", discountTypeId, ex.Message);
            return BadRequest(new { success = false, message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error activating discount type {DiscountTypeId}", discountTypeId);
            return StatusCode(500, new { success = false, message = "An error occurred while activating the discount type." });
        }
    }

    /// <summary>
    /// Deactivates a discount type (Admin only).
    /// </summary>
    [HttpPost("types/{discountTypeId}/deactivate")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeactivateDiscountType(int discountTypeId)
    {
        try
        {
            var result = await _discountService.DeactivateDiscountTypeAsync(discountTypeId);

            return Ok(new { success = true, message = "Discount type deactivated successfully." });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning("Failed to deactivate discount type {DiscountTypeId}: {Message}", discountTypeId, ex.Message);
            return BadRequest(new { success = false, message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deactivating discount type {DiscountTypeId}", discountTypeId);
            return StatusCode(500, new { success = false, message = "An error occurred while deactivating the discount type." });
        }
    }

    /// <summary>
    /// Retrieves all active discount types (available to all authenticated users).
    /// </summary>
    [HttpGet("types")]
    [AllowAnonymous]
    public async Task<IActionResult> GetAllDiscountTypes()
    {
        try
        {
            var discountTypes = await _discountService.GetAllDiscountTypesAsync();

            return Ok(new
            {
                success = true,
                message = "Discount types retrieved successfully.",
                data = discountTypes.Select(dt => new
                {
                    discountTypeId = dt.DiscountTypeId,
                    name = dt.Name,
                    description = dt.Description,
                    discountPercentage = dt.DiscountPercentage,
                    isActive = dt.IsActive,
                    requiresApproval = dt.RequiresApproval,
                    createdAt = dt.CreatedAt,
                    updatedAt = dt.UpdatedAt
                })
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving discount types");
            return StatusCode(500, new { success = false, message = "An error occurred while retrieving discount types." });
        }
    }

    /// <summary>
    /// Retrieves a specific discount type (Admin only).
    /// </summary>
    [HttpGet("types/{discountTypeId}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetDiscountTypeById(int discountTypeId)
    {
        try
        {
            var discountType = await _discountService.GetDiscountTypeByIdAsync(discountTypeId);

            if (discountType == null)
            {
                return NotFound(new { success = false, message = "Discount type not found." });
            }

            return Ok(new
            {
                success = true,
                message = "Discount type retrieved successfully.",
                data = new
                {
                    discountTypeId = discountType.DiscountTypeId,
                    name = discountType.Name,
                    description = discountType.Description,
                    discountPercentage = discountType.DiscountPercentage,
                    isActive = discountType.IsActive,
                    requiresApproval = discountType.RequiresApproval,
                    createdAt = discountType.CreatedAt,
                    updatedAt = discountType.UpdatedAt
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving discount type {DiscountTypeId}", discountTypeId);
            return StatusCode(500, new { success = false, message = "An error occurred while retrieving the discount type." });
        }
    }

    #endregion

    #region Discount Program Management (Admin)

    /// <summary>
    /// Creates a new discount program (Admin only).
    /// </summary>
    [HttpPost("programs")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> CreateDiscountProgram([FromBody] CreateDiscountProgramRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new { success = false, message = "Validation failed.", errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage) });
        }

        try
        {
            var discountProgram = new DiscountProgram
            {
                Name = request.Name,
                Description = request.Description,
                DiscountPercentage = request.DiscountPercentage,
                RequiresApproval = request.RequiresApproval
            };

            var created = await _discountService.CreateDiscountProgramAsync(discountProgram);

            return Ok(new
            {
                success = true,
                message = "Discount program created successfully.",
                data = new
                {
                    created.DiscountProgramId,
                    created.Name,
                    created.Description,
                    created.DiscountPercentage,
                    created.IsActive,
                    created.RequiresApproval,
                    created.CreatedAt
                }
            });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning("Failed to create discount program: {Message}", ex.Message);
            return BadRequest(new { success = false, message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating discount program");
            return StatusCode(500, new { success = false, message = "An error occurred while creating the discount program." });
        }
    }

    /// <summary>
    /// Updates an existing discount program (Admin only).
    /// </summary>
    [HttpPut("programs/{discountProgramId}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateDiscountProgram(int discountProgramId, [FromBody] UpdateDiscountProgramRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new { success = false, message = "Validation failed.", errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage) });
        }

        try
        {
            var discountProgram = new DiscountProgram
            {
                Name = request.Name,
                Description = request.Description,
                DiscountPercentage = request.DiscountPercentage,
                RequiresApproval = request.RequiresApproval
            };

            var updated = await _discountService.UpdateDiscountProgramAsync(discountProgramId, discountProgram);

            return Ok(new
            {
                success = true,
                message = "Discount program updated successfully.",
                data = new
                {
                    updated.DiscountProgramId,
                    updated.Name,
                    updated.Description,
                    updated.DiscountPercentage,
                    updated.IsActive,
                    updated.RequiresApproval,
                    updated.UpdatedAt
                }
            });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning("Failed to update discount program {DiscountProgramId}: {Message}", discountProgramId, ex.Message);
            return BadRequest(new { success = false, message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating discount program {DiscountProgramId}", discountProgramId);
            return StatusCode(500, new { success = false, message = "An error occurred while updating the discount program." });
        }
    }

    /// <summary>
    /// Deletes a discount program (Admin only). Soft delete.
    /// </summary>
    [HttpDelete("programs/{discountProgramId}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteDiscountProgram(int discountProgramId)
    {
        try
        {
            var result = await _discountService.DeleteDiscountProgramAsync(discountProgramId);

            if (!result)
            {
                return NotFound(new { success = false, message = "Discount program not found." });
            }

            return Ok(new { success = true, message = "Discount program deleted successfully." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting discount program {DiscountProgramId}", discountProgramId);
            return StatusCode(500, new { success = false, message = "An error occurred while deleting the discount program." });
        }
    }

    /// <summary>
    /// Activates a discount program (Admin only).
    /// </summary>
    [HttpPost("programs/{discountProgramId}/activate")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> ActivateDiscountProgram(int discountProgramId)
    {
        try
        {
            await _discountService.ActivateDiscountProgramAsync(discountProgramId);
            return Ok(new { success = true, message = "Discount program activated successfully." });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning("Failed to activate discount program {DiscountProgramId}: {Message}", discountProgramId, ex.Message);
            return BadRequest(new { success = false, message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error activating discount program {DiscountProgramId}", discountProgramId);
            return StatusCode(500, new { success = false, message = "An error occurred while activating the discount program." });
        }
    }

    /// <summary>
    /// Deactivates a discount program (Admin only).
    /// </summary>
    [HttpPost("programs/{discountProgramId}/deactivate")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeactivateDiscountProgram(int discountProgramId)
    {
        try
        {
            await _discountService.DeactivateDiscountProgramAsync(discountProgramId);
            return Ok(new { success = true, message = "Discount program deactivated successfully." });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning("Failed to deactivate discount program {DiscountProgramId}: {Message}", discountProgramId, ex.Message);
            return BadRequest(new { success = false, message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deactivating discount program {DiscountProgramId}", discountProgramId);
            return StatusCode(500, new { success = false, message = "An error occurred while deactivating the discount program." });
        }
    }

    /// <summary>
    /// Retrieves all discount programs (Admin only).
    /// </summary>
    [HttpGet("programs")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetAllDiscountPrograms()
    {
        try
        {
            var discountPrograms = await _discountService.GetAllDiscountProgramsAsync();

            return Ok(new
            {
                success = true,
                message = "Discount programs retrieved successfully.",
                data = discountPrograms.Select(dp => new
                {
                    dp.DiscountProgramId,
                    dp.Name,
                    dp.Description,
                    dp.DiscountPercentage,
                    dp.IsActive,
                    dp.RequiresApproval,
                    dp.CreatedAt,
                    dp.UpdatedAt
                })
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving discount programs");
            return StatusCode(500, new { success = false, message = "An error occurred while retrieving discount programs." });
        }
    }

    /// <summary>
    /// Retrieves a specific discount program (Admin only).
    /// </summary>
    [HttpGet("programs/{discountProgramId}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetDiscountProgramById(int discountProgramId)
    {
        try
        {
            var discountProgram = await _discountService.GetDiscountProgramByIdAsync(discountProgramId);

            if (discountProgram == null)
            {
                return NotFound(new { success = false, message = "Discount program not found." });
            }

            return Ok(new
            {
                success = true,
                message = "Discount program retrieved successfully.",
                data = new
                {
                    discountProgram.DiscountProgramId,
                    discountProgram.Name,
                    discountProgram.Description,
                    discountProgram.DiscountPercentage,
                    discountProgram.IsActive,
                    discountProgram.RequiresApproval,
                    discountProgram.CreatedAt,
                    discountProgram.UpdatedAt
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving discount program {DiscountProgramId}", discountProgramId);
            return StatusCode(500, new { success = false, message = "An error occurred while retrieving the discount program." });
        }
    }

    #endregion

    #region Active Discount Retrieval (Passenger)

    /// <summary>
    /// Retrieves the active discount for a specific card (Passenger).
    /// Queries the PassengerDiscounts table directly for the materialized active discount.
    /// </summary>
    [HttpGet("active/{cardId}")]
    [Authorize]
    public async Task<IActionResult> GetActiveDiscount(int cardId)
    {
        // Ownership validation: the card must belong to the authenticated user
        var cardAccess = await CanAccessCardAsync(cardId);
        if (!cardAccess)
        {
            return NotFound(new { success = false, message = "Card not found." });
        }

        try
        {
            var activeDiscount = await _discountService.GetActiveDiscountForCardAsync(cardId);

            if (activeDiscount == null)
            {
                return Ok(new { success = true, message = "No active discount found.", data = (object?)null });
            }

            return Ok(new
            {
                success = true,
                message = "Active discount retrieved successfully.",
                data = new
                {
                    activeDiscount.PassengerDiscountId,
                    activeDiscount.CardId,
                    activeDiscount.DiscountProgramId,
                    discountTypeName = activeDiscount.DiscountProgram?.Name,
                    discountPercentage = activeDiscount.DiscountPercentage,
                    activeDiscount.Status,
                    activeDiscount.ApprovedBy,
                    activeDiscount.ApprovedAt,
                    activeDiscount.CreatedAt
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving active discount for card {CardId}", cardId);
            return StatusCode(500, new { success = false, message = "An error occurred while retrieving active discount." });
        }
    }

    #endregion

    #region Discount Application Management (Passenger)

    /// <summary>
    /// Applies for a discount (Passenger).
    /// </summary>
    [HttpPost("apply")]
    [Authorize]
    public async Task<IActionResult> ApplyForDiscount([FromBody] ApplyForDiscountRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new { success = false, message = "Validation failed.", errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage) });
        }

        // Ownership validation: the card must belong to the authenticated user
        var cardAccess = await CanAccessCardAsync(request.CardId);
        if (!cardAccess)
        {
            return NotFound(new { success = false, message = "Card not found." });
        }

        // Get the authenticated user ID
        var userId = User.GetAuthenticatedUserId();
        if (userId == null)
        {
            return Unauthorized(new { success = false, message = "User not authenticated." });
        }

        try
        {
            var application = await _discountService.ApplyForDiscountAsync(
                request.CardId,
                request.DiscountTypeId,
                userId.Value,
                request.DiscountDocument);

            return Ok(new
            {
                success = true,
                message = "Discount application submitted successfully.",
                data = new
                {
                    application.DiscountApplicationId,
                    application.CardId,
                    application.UserId,
                    application.DiscountTypeId,
                    discountTypeName = application.DiscountType?.Name,
                    application.Status,
                    application.DiscountDocument,
                    application.CreatedAt
                }
            });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning("Failed to apply for discount: {Message}", ex.Message);
            return BadRequest(new { success = false, message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error applying for discount");
            return StatusCode(500, new { success = false, message = "An error occurred while applying for the discount." });
        }
    }

    /// <summary>
    /// Retrieves discount applications for a specific card (Passenger).
    /// </summary>
    [HttpGet("applications/card/{cardId}")]
    [Authorize]
    public async Task<IActionResult> GetApplicationsByCard(int cardId)
    {
        // Ownership validation: the card must belong to the authenticated user
        var cardAccess = await CanAccessCardAsync(cardId);
        if (!cardAccess)
        {
            return NotFound(new { success = false, message = "Card not found." });
        }

        try
        {
            var applications = await _discountService.GetApplicationsByCardAsync(cardId);

            return Ok(new
            {
                success = true,
                message = "Discount applications retrieved successfully.",
                data = applications.Select(a => new
                {
                    a.DiscountApplicationId,
                    a.CardId,
                    a.DiscountTypeId,
                    discountTypeName = a.DiscountType?.Name,
                    discountPercentage = a.DiscountType != null ? (decimal?)a.DiscountType.DiscountPercentage : null,
                    a.Status,
                    a.ApprovedBy,
                    a.ApprovedAt,
                    a.RejectedAt,
                    a.RejectionReason,
                    a.CreatedAt
                })
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving discount applications for card {CardId}", cardId);
            return StatusCode(500, new { success = false, message = "An error occurred while retrieving discount applications." });
        }
    }

    #endregion

    #region Discount Approval Workflow (Admin)

    /// <summary>
    /// Approves a discount application (Admin only).
    /// </summary>
    [HttpPost("applications/{applicationId}/approve")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> ApproveDiscountApplication(int applicationId)
    {
        try
        {
            var adminId = User.GetAuthenticatedUserId();
            if (adminId == null)
            {
                return Unauthorized(new { success = false, message = "Admin not authenticated." });
            }

            var application = await _discountService.ApproveDiscountApplicationAsync(applicationId, adminId.Value);

            return Ok(new
            {
                success = true,
                message = "Discount application approved successfully.",
                data = new
                {
                    application.DiscountApplicationId,
                    application.CardId,
                    application.DiscountTypeId,
                    discountTypeName = application.DiscountType?.Name,
                    application.Status,
                    application.ApprovedBy,
                    application.ApprovedAt
                }
            });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning("Failed to approve discount application {ApplicationId}: {Message}", applicationId, ex.Message);
            return BadRequest(new { success = false, message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error approving discount application {ApplicationId}", applicationId);
            return StatusCode(500, new { success = false, message = "An error occurred while approving the discount application." });
        }
    }

    /// <summary>
    /// Rejects a discount application (Admin only).
    /// </summary>
    [HttpPost("applications/{applicationId}/reject")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> RejectDiscountApplication(int applicationId, [FromBody] RejectDiscountApplicationRequest request)
    {
        try
        {
            var adminId = User.GetAuthenticatedUserId();
            if (adminId == null)
            {
                return Unauthorized(new { success = false, message = "Admin not authenticated." });
            }

            var application = await _discountService.RejectDiscountApplicationAsync(applicationId, adminId.Value, request.RejectionReason);

            return Ok(new
            {
                success = true,
                message = "Discount application rejected successfully.",
                data = new
                {
                    application.DiscountApplicationId,
                    application.CardId,
                    application.DiscountTypeId,
                    discountTypeName = application.DiscountType?.Name,
                    application.Status,
                    application.RejectedAt,
                    application.RejectionReason
                }
            });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning("Failed to reject discount application {ApplicationId}: {Message}", applicationId, ex.Message);
            return BadRequest(new { success = false, message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error rejecting discount application {ApplicationId}", applicationId);
            return StatusCode(500, new { success = false, message = "An error occurred while rejecting the discount application." });
        }
    }

    /// <summary>
    /// Retrieves all pending discount applications (Admin only).
    /// </summary>
    [HttpGet("applications/pending")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetPendingApplications()
    {
        try
        {
            var applications = await _discountService.GetPendingApplicationsAsync();

            return Ok(new
            {
                success = true,
                message = "Pending discount applications retrieved successfully.",
                data = applications.Select(a => new
                {
                    a.DiscountApplicationId,
                    a.CardId,
                    passengerName = a.User != null 
                        ? $"{a.User.FirstName} {a.User.LastName}" 
                        : a.Card != null && a.Card.User != null 
                            ? $"{a.Card.User.FirstName} {a.Card.User.LastName}" 
                            : "Unknown",
                    a.DiscountTypeId,
                    discountTypeName = a.DiscountType?.Name,
                    discountPercentage = a.DiscountType?.DiscountPercentage,
                    a.Status,
                    a.DiscountDocument,
                    a.CreatedAt
                })
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving pending discount applications");
            return StatusCode(500, new { success = false, message = "An error occurred while retrieving pending applications." });
        }
    }

    /// <summary>
    /// Retrieves all discount applications (Admin only).
    /// </summary>
    [HttpGet("applications")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetAllApplications()
    {
        try
        {
            var applications = await _discountService.GetAllApplicationsAsync();

            return Ok(new
            {
                success = true,
                message = "All discount applications retrieved successfully.",
                data = applications.Select(a => new
                {
                    a.DiscountApplicationId,
                    a.CardId,
                    passengerName = a.User != null 
                        ? $"{a.User.FirstName} {a.User.LastName}" 
                        : a.Card != null && a.Card.User != null 
                            ? $"{a.Card.User.FirstName} {a.Card.User.LastName}" 
                            : "Unknown",
                    a.DiscountTypeId,
                    discountTypeName = a.DiscountType?.Name,
                    discountPercentage = a.DiscountType?.DiscountPercentage,
                    a.Status,
                    a.ApprovedBy,
                    a.ApprovedAt,
                    a.RejectedAt,
                    a.RejectionReason,
                    a.CreatedAt
                })
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving all discount applications");
            return StatusCode(500, new { success = false, message = "An error occurred while retrieving applications." });
        }
    }

    /// <summary>
    /// Downloads the discount application document (Admin only).
    /// </summary>
    [HttpGet("applications/{applicationId}/document")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetApplicationDocument(int applicationId)
    {
        try
        {
            var application = await _dbContext.DiscountApplications
                .Include(a => a.Card)
                .FirstOrDefaultAsync(a => a.DiscountApplicationId == applicationId);

            if (application == null)
            {
                return NotFound(new { success = false, message = "Application not found." });
            }

            if (string.IsNullOrEmpty(application.DiscountDocument))
            {
                return NotFound(new { success = false, message = "No document uploaded for this application." });
            }

            // Check if it's an image (base64 data URL)
            if (application.DiscountDocument.StartsWith("data:image"))
            {
                // Extract the base64 data and content type
                var parts = application.DiscountDocument.Split(',');
                if (parts.Length != 2)
                {
                    return BadRequest(new { success = false, message = "Invalid document format." });
                }

                var contentType = parts[0].Split(':')[1].Split(';')[0];
                var base64Data = parts[1];

                // Convert base64 to byte array
                var bytes = Convert.FromBase64String(base64Data);

                return File(bytes, contentType, $"document_{applicationId}.{contentType.Split('/')[1]}");
            }
            else
            {
                // Treat as text file
                var bytes = System.Text.Encoding.UTF8.GetBytes(application.DiscountDocument);
                return File(bytes, "text/plain", $"document_{applicationId}.txt");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving document for application {ApplicationId}", applicationId);
            return StatusCode(500, new { success = false, message = "An error occurred while retrieving the document." });
        }
    }

    #endregion

    /// <summary>
    /// Determines whether the authenticated user can access a specific card.
    /// Owners may access their own cards. Admins may access any card.
    /// </summary>
    private async Task<bool> CanAccessCardAsync(int cardId)
    {
        var userId = User.GetAuthenticatedUserId();
        if (userId == null)
        {
            return false;
        }

        var isAdmin = User.IsInRole(nameof(RoleName.Admin));

        if (isAdmin)
        {
            return true;
        }

        var card = await _dbContext.Cards
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.CardId == cardId);

        return card != null && card.UserId == userId.Value;
    }
}

#region Request DTOs

/// <summary>
/// Request DTO for creating a discount type.
/// </summary>
public class CreateDiscountTypeRequest
{
    /// <summary>The discount name (e.g., "Student", "Senior").</summary>
    [Required(ErrorMessage = "Name is required.")]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    /// <summary>A description of eligibility/terms.</summary>
    [MaxLength(500)]
    public string? Description { get; set; }

    /// <summary>The discount percentage (0–100).</summary>
    [Required(ErrorMessage = "Discount percentage is required.")]
    [Range(0, 100, ErrorMessage = "Discount percentage must be between 0 and 100.")]
    public decimal DiscountPercentage { get; set; }

    /// <summary>Whether applications for this type require admin approval.</summary>
    public bool RequiresApproval { get; set; } = true;
}

/// <summary>
/// Request DTO for updating a discount type.
/// </summary>
public class UpdateDiscountTypeRequest
{
    /// <summary>The discount name (e.g., "Student", "Senior").</summary>
    [Required(ErrorMessage = "Name is required.")]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    /// <summary>A description of eligibility/terms.</summary>
    [MaxLength(500)]
    public string? Description { get; set; }

    /// <summary>The discount percentage (0–100).</summary>
    [Required(ErrorMessage = "Discount percentage is required.")]
    [Range(0, 100, ErrorMessage = "Discount percentage must be between 0 and 100.")]
    public decimal DiscountPercentage { get; set; }

    /// <summary>Whether applications for this type require admin approval.</summary>
    public bool RequiresApproval { get; set; } = true;
}

/// <summary>
/// Request DTO for applying for a discount.
/// </summary>
public class ApplyForDiscountRequest
{
    /// <summary>The card being applied for the discount.</summary>
    [Required(ErrorMessage = "Card ID is required.")]
    public int CardId { get; set; }

    /// <summary>The discount type being applied for.</summary>
    [Required(ErrorMessage = "Discount type ID is required.")]
    public int DiscountTypeId { get; set; }

    /// <summary>Optional supporting document identifier uploaded by the passenger.</summary>
    [MaxLength(500)]
    public string? DiscountDocument { get; set; }
}

/// <summary>
/// Request DTO for rejecting a discount application.
/// </summary>
public class RejectDiscountApplicationRequest
{
    /// <summary>Optional reason for the rejection (shown to the passenger).</summary>
    [MaxLength(500)]
    public string? RejectionReason { get; set; }
}

/// <summary>
/// Request DTO for creating a discount program.
/// </summary>
public class CreateDiscountProgramRequest
{
    /// <summary>The program name (e.g., "Student", "Senior").</summary>
    [Required(ErrorMessage = "Name is required.")]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    /// <summary>A description of eligibility/terms.</summary>
    [MaxLength(500)]
    public string? Description { get; set; }

    /// <summary>The discount percentage (0–100).</summary>
    [Required(ErrorMessage = "Discount percentage is required.")]
    [Range(0, 100, ErrorMessage = "Discount percentage must be between 0 and 100.")]
    public decimal DiscountPercentage { get; set; }

    /// <summary>Whether applications for this program require admin approval.</summary>
    public bool RequiresApproval { get; set; } = true;
}

/// <summary>
/// Request DTO for updating a discount program.
/// </summary>
public class UpdateDiscountProgramRequest
{
    /// <summary>The program name (e.g., "Student", "Senior").</summary>
    [Required(ErrorMessage = "Name is required.")]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    /// <summary>A description of eligibility/terms.</summary>
    [MaxLength(500)]
    public string? Description { get; set; }

    /// <summary>The discount percentage (0–100).</summary>
    [Required(ErrorMessage = "Discount percentage is required.")]
    [Range(0, 100, ErrorMessage = "Discount percentage must be between 0 and 100.")]
    public decimal DiscountPercentage { get; set; }

    /// <summary>Whether applications for this program require admin approval.</summary>
    public bool RequiresApproval { get; set; } = true;
}

#endregion