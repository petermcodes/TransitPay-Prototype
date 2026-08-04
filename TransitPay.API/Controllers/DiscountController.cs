using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TransitPay.API.Interfaces;
using TransitPay.API.Models;

namespace TransitPay.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DiscountController : ControllerBase
{
    private readonly IDiscountService _discountService;
    private readonly ILogger<DiscountController> _logger;

    public DiscountController(IDiscountService discountService, ILogger<DiscountController> logger)
    {
        _discountService = discountService;
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
    /// Retrieves all discount types (Admin only).
    /// </summary>
    [HttpGet("types")]
    [Authorize(Roles = "Admin")]
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
                    dt.DiscountTypeId,
                    dt.Name,
                    dt.Description,
                    dt.DiscountPercentage,
                    dt.IsActive,
                    dt.RequiresApproval,
                    dt.CreatedAt,
                    dt.UpdatedAt
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
                    discountType.DiscountTypeId,
                    discountType.Name,
                    discountType.Description,
                    discountType.DiscountPercentage,
                    discountType.IsActive,
                    discountType.RequiresApproval,
                    discountType.CreatedAt,
                    discountType.UpdatedAt
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

        try
        {
            var application = await _discountService.ApplyForDiscountAsync(
                request.CardId,
                request.DiscountTypeId,
                request.DiscountDocument);

            return Ok(new
            {
                success = true,
                message = "Discount application submitted successfully.",
                data = new
                {
                    application.DiscountApplicationId,
                    application.CardId,
                    application.DiscountTypeId,
                    DiscountTypeName = application.DiscountType?.Name,
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
                    DiscountTypeName = a.DiscountType?.Name,
                    DiscountPercentage = a.DiscountType != null ? (decimal?)a.DiscountType.DiscountPercentage : null,
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
            var adminId = GetUserIdFromClaims();
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
                    DiscountTypeName = application.DiscountType?.Name,
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
            var adminId = GetUserIdFromClaims();
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
                    DiscountTypeName = application.DiscountType?.Name,
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
                    CardNumber = a.Card?.CardNumber,
                    a.DiscountTypeId,
                    DiscountTypeName = a.DiscountType?.Name,
                    DiscountPercentage = a.DiscountType?.DiscountPercentage,
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
                    CardNumber = a.Card?.CardNumber,
                    a.DiscountTypeId,
                    DiscountTypeName = a.DiscountType?.Name,
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

    #endregion

    #region Helper Methods

    /// <summary>
    /// Extracts the authenticated user's ID from the JWT claims.
    /// </summary>
    private int? GetUserIdFromClaims()
    {
        var userIdClaim = User.FindFirstValue(System.Security.Claims.ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub")
            ?? User.FindFirstValue("userId");

        if (int.TryParse(userIdClaim, out var userId))
        {
            return userId;
        }

        return null;
    }

    #endregion
}

#region Request DTOs

/// <summary>
/// Request DTO for creating a discount type.
/// </summary>
public class CreateDiscountTypeRequest
{
    [Required(ErrorMessage = "Name is required.")]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Description { get; set; }

    [Required(ErrorMessage = "Discount percentage is required.")]
    [Range(0, 100, ErrorMessage = "Discount percentage must be between 0 and 100.")]
    public decimal DiscountPercentage { get; set; }

    public bool RequiresApproval { get; set; } = true;
}

/// <summary>
/// Request DTO for updating a discount type.
/// </summary>
public class UpdateDiscountTypeRequest
{
    [Required(ErrorMessage = "Name is required.")]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Description { get; set; }

    [Required(ErrorMessage = "Discount percentage is required.")]
    [Range(0, 100, ErrorMessage = "Discount percentage must be between 0 and 100.")]
    public decimal DiscountPercentage { get; set; }

    public bool RequiresApproval { get; set; } = true;
}

/// <summary>
/// Request DTO for applying for a discount.
/// </summary>
public class ApplyForDiscountRequest
{
    [Required(ErrorMessage = "Card ID is required.")]
    public int CardId { get; set; }

    [Required(ErrorMessage = "Discount type ID is required.")]
    public int DiscountTypeId { get; set; }

    [MaxLength(500)]
    public string? DiscountDocument { get; set; }
}

/// <summary>
/// Request DTO for rejecting a discount application.
/// </summary>
public class RejectDiscountApplicationRequest
{
    [MaxLength(500)]
    public string? RejectionReason { get; set; }
}

#endregion