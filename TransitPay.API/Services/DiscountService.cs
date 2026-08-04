using Microsoft.EntityFrameworkCore;
using TransitPay.API.Data;
using TransitPay.API.Interfaces;
using TransitPay.API.Models;

namespace TransitPay.API.Services;

/// <summary>
/// Service for managing discount types and discount applications.
/// Handles the complete discount lifecycle from creation to application.
/// </summary>
public class DiscountService : IDiscountService
{
    private readonly TransitPayDbContext _dbContext;
    private readonly ILogger<DiscountService> _logger;

    public DiscountService(TransitPayDbContext dbContext, ILogger<DiscountService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    #region Discount Type Management (Admin)

    /// <inheritdoc />
    public async Task<DiscountType> CreateDiscountTypeAsync(DiscountType discountType)
    {
        _logger.LogInformation("Creating discount type: {Name}", discountType.Name);

        // Validate discount percentage
        if (discountType.DiscountPercentage < 0 || discountType.DiscountPercentage > 100)
        {
            throw new InvalidOperationException("Discount percentage must be between 0 and 100.");
        }

        discountType.CreatedAt = DateTime.UtcNow;
        discountType.IsActive = true;

        _dbContext.DiscountTypes.Add(discountType);
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("Discount type created successfully. DiscountTypeId: {DiscountTypeId}", discountType.DiscountTypeId);

        return discountType;
    }

    /// <inheritdoc />
    public async Task<DiscountType> UpdateDiscountTypeAsync(int discountTypeId, DiscountType discountType)
    {
        _logger.LogInformation("Updating discount type {DiscountTypeId}", discountTypeId);

        var existingType = await _dbContext.DiscountTypes.FindAsync(discountTypeId);
        if (existingType == null)
        {
            throw new InvalidOperationException("Discount type not found.");
        }

        // Validate discount percentage
        if (discountType.DiscountPercentage < 0 || discountType.DiscountPercentage > 100)
        {
            throw new InvalidOperationException("Discount percentage must be between 0 and 100.");
        }

        // Update properties
        existingType.Name = discountType.Name;
        existingType.Description = discountType.Description;
        existingType.DiscountPercentage = discountType.DiscountPercentage;
        existingType.RequiresApproval = discountType.RequiresApproval;
        existingType.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("Discount type {DiscountTypeId} updated successfully", discountTypeId);

        return existingType;
    }

    /// <inheritdoc />
    public async Task<bool> DeleteDiscountTypeAsync(int discountTypeId)
    {
        _logger.LogInformation("Deleting discount type {DiscountTypeId}", discountTypeId);

        var discountType = await _dbContext.DiscountTypes.FindAsync(discountTypeId);
        if (discountType == null)
        {
            return false;
        }

        // Soft delete
        discountType.DeletedAt = DateTime.UtcNow;
        discountType.IsActive = false;

        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("Discount type {DiscountTypeId} deleted successfully", discountTypeId);

        return true;
    }

    /// <inheritdoc />
    public async Task<bool> ActivateDiscountTypeAsync(int discountTypeId)
    {
        _logger.LogInformation("Activating discount type {DiscountTypeId}", discountTypeId);

        var discountType = await _dbContext.DiscountTypes.FindAsync(discountTypeId);
        if (discountType == null)
        {
            throw new InvalidOperationException("Discount type not found.");
        }

        if (discountType.DeletedAt != null)
        {
            throw new InvalidOperationException("Cannot activate a deleted discount type.");
        }

        discountType.IsActive = true;
        discountType.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("Discount type {DiscountTypeId} activated successfully", discountTypeId);

        return true;
    }

    /// <inheritdoc />
    public async Task<bool> DeactivateDiscountTypeAsync(int discountTypeId)
    {
        _logger.LogInformation("Deactivating discount type {DiscountTypeId}", discountTypeId);

        var discountType = await _dbContext.DiscountTypes.FindAsync(discountTypeId);
        if (discountType == null)
        {
            throw new InvalidOperationException("Discount type not found.");
        }

        discountType.IsActive = false;
        discountType.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("Discount type {DiscountTypeId} deactivated successfully", discountTypeId);

        return true;
    }

    /// <inheritdoc />
    public async Task<IEnumerable<DiscountType>> GetAllDiscountTypesAsync()
    {
        _logger.LogInformation("Retrieving all discount types");

        var discountTypes = await _dbContext.DiscountTypes
            .Where(dt => dt.DeletedAt == null)
            .OrderBy(dt => dt.Name)
            .ToListAsync();

        return discountTypes;
    }

    /// <inheritdoc />
    public async Task<DiscountType?> GetDiscountTypeByIdAsync(int discountTypeId)
    {
        _logger.LogInformation("Retrieving discount type {DiscountTypeId}", discountTypeId);

        var discountType = await _dbContext.DiscountTypes
            .FirstOrDefaultAsync(dt => dt.DiscountTypeId == discountTypeId && dt.DeletedAt == null);

        return discountType;
    }

    #endregion

    #region Discount Application Management (Passenger)

    /// <inheritdoc />
    public async Task<DiscountApplication> ApplyForDiscountAsync(int cardId, int discountTypeId, string? discountDocument = null)
    {
        _logger.LogInformation("Processing discount application for card {CardId} to discount type {DiscountTypeId}", cardId, discountTypeId);

        // Validate card exists
        var card = await _dbContext.Cards.FindAsync(cardId);
        if (card == null)
        {
            throw new InvalidOperationException("Card not found.");
        }

        // Validate discount type exists and is active
        var discountType = await _dbContext.DiscountTypes
            .FirstOrDefaultAsync(dt => dt.DiscountTypeId == discountTypeId && dt.IsActive && dt.DeletedAt == null);

        if (discountType == null)
        {
            throw new InvalidOperationException("Discount type not found or is not active.");
        }

        // Check if card already has an active discount
        var existingActiveDiscount = await _dbContext.DiscountApplications
            .FirstOrDefaultAsync(da => da.CardId == cardId &&
                                       da.Status == DiscountApplicationStatus.Approved &&
                                       da.DiscountType.DiscountTypeId == discountTypeId &&
                                       da.DiscountType.DeletedAt == null);

        if (existingActiveDiscount != null)
        {
            throw new InvalidOperationException("This card already has an active discount of this type.");
        }

        // Check for pending application
        var pendingApplication = await _dbContext.DiscountApplications
            .FirstOrDefaultAsync(da => da.CardId == cardId &&
                                       da.DiscountTypeId == discountTypeId &&
                                       da.Status == DiscountApplicationStatus.Pending);

        if (pendingApplication != null)
        {
            throw new InvalidOperationException("A pending application for this discount type already exists.");
        }

        // Create application
        var application = new DiscountApplication
        {
            CardId = cardId,
            DiscountTypeId = discountTypeId,
            Status = discountType.RequiresApproval ? DiscountApplicationStatus.Pending : DiscountApplicationStatus.Approved,
            DiscountDocument = discountDocument,
            CreatedAt = DateTime.UtcNow
        };

        // If no approval required, auto-approve
        if (!discountType.RequiresApproval)
        {
            application.ApprovedAt = DateTime.UtcNow;
            application.ApprovedBy = null; // System approval
        }

        _dbContext.DiscountApplications.Add(application);
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("Discount application {ApplicationId} created for card {CardId}", application.DiscountApplicationId, cardId);

        return application;
    }

    /// <inheritdoc />
    public async Task<IEnumerable<DiscountApplication>> GetApplicationsByCardAsync(int cardId)
    {
        _logger.LogInformation("Retrieving discount applications for card {CardId}", cardId);

        var applications = await _dbContext.DiscountApplications
            .Include(da => da.DiscountType)
            .Where(da => da.CardId == cardId && da.DiscountType.DeletedAt == null)
            .OrderByDescending(da => da.CreatedAt)
            .ToListAsync();

        return applications;
    }

    #endregion

    #region Discount Approval Workflow (Admin)

    /// <inheritdoc />
    public async Task<DiscountApplication> ApproveDiscountApplicationAsync(int applicationId, int adminId)
    {
        _logger.LogInformation("Approving discount application {ApplicationId} by admin {AdminId}", applicationId, adminId);

        var application = await _dbContext.DiscountApplications
            .Include(da => da.DiscountType)
            .FirstOrDefaultAsync(da => da.DiscountApplicationId == applicationId);

        if (application == null)
        {
            throw new InvalidOperationException("Discount application not found.");
        }

        if (application.Status != DiscountApplicationStatus.Pending)
        {
            throw new InvalidOperationException($"Cannot approve application with status '{application.Status}'. Only pending applications can be approved.");
        }

        if (application.DiscountType.DeletedAt != null)
        {
            throw new InvalidOperationException("Cannot approve application for a deleted discount type.");
        }

        // Approve the application
        application.Status = DiscountApplicationStatus.Approved;
        application.ApprovedBy = adminId;
        application.ApprovedAt = DateTime.UtcNow;
        application.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("Discount application {ApplicationId} approved successfully", applicationId);

        return application;
    }

    /// <inheritdoc />
    public async Task<DiscountApplication> RejectDiscountApplicationAsync(int applicationId, int adminId, string? rejectionReason = null)
    {
        _logger.LogInformation("Rejecting discount application {ApplicationId} by admin {AdminId}", applicationId, adminId);

        var application = await _dbContext.DiscountApplications
            .Include(da => da.DiscountType)
            .FirstOrDefaultAsync(da => da.DiscountApplicationId == applicationId);

        if (application == null)
        {
            throw new InvalidOperationException("Discount application not found.");
        }

        if (application.Status != DiscountApplicationStatus.Pending)
        {
            throw new InvalidOperationException($"Cannot reject application with status '{application.Status}'. Only pending applications can be rejected.");
        }

        // Reject the application
        application.Status = DiscountApplicationStatus.Rejected;
        application.RejectedAt = DateTime.UtcNow;
        application.RejectionReason = rejectionReason;
        application.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("Discount application {ApplicationId} rejected successfully", applicationId);

        return application;
    }

    /// <inheritdoc />
    public async Task<IEnumerable<DiscountApplication>> GetPendingApplicationsAsync()
    {
        _logger.LogInformation("Retrieving pending discount applications");

        var pendingApplications = await _dbContext.DiscountApplications
            .Include(da => da.Card)
            .Include(da => da.DiscountType)
            .Where(da => da.Status == DiscountApplicationStatus.Pending && da.DiscountType.DeletedAt == null)
            .OrderBy(da => da.CreatedAt)
            .ToListAsync();

        return pendingApplications;
    }

    /// <inheritdoc />
    public async Task<IEnumerable<DiscountApplication>> GetAllApplicationsAsync()
    {
        _logger.LogInformation("Retrieving all discount applications");

        var applications = await _dbContext.DiscountApplications
            .Include(da => da.Card)
            .Include(da => da.DiscountType)
            .Where(da => da.DiscountType.DeletedAt == null)
            .OrderByDescending(da => da.CreatedAt)
            .ToListAsync();

        return applications;
    }

    #endregion

    #region Discount Retrieval (Payment Service)

    /// <inheritdoc />
    public async Task<DiscountApplication?> GetActiveDiscountForCardAsync(int cardId)
    {
        _logger.LogInformation("Retrieving active discount for card {CardId}", cardId);

        var activeDiscount = await _dbContext.DiscountApplications
            .Include(da => da.DiscountType)
            .FirstOrDefaultAsync(da => da.CardId == cardId &&
                                       da.Status == DiscountApplicationStatus.Approved &&
                                       da.DiscountType.IsActive &&
                                       da.DiscountType.DeletedAt == null);

        return activeDiscount;
    }

    /// <inheritdoc />
    public async Task<decimal> CalculateDiscountedFareAsync(decimal regularFare, int? discountTypeId)
    {
        _logger.LogInformation("Calculating discounted fare. RegularFare: {RegularFare}, DiscountTypeId: {DiscountTypeId}", regularFare, discountTypeId);

        // If no discount type, return regular fare
        if (!discountTypeId.HasValue)
        {
            return regularFare;
        }

        // Get discount type
        var discountType = await _dbContext.DiscountTypes
            .FirstOrDefaultAsync(dt => dt.DiscountTypeId == discountTypeId.Value && dt.IsActive && dt.DeletedAt == null);

        if (discountType == null)
        {
            _logger.LogWarning("Discount type {DiscountTypeId} not found or inactive", discountTypeId);
            return regularFare;
        }

        // Calculate discount
        var discountAmount = regularFare * (discountType.DiscountPercentage / 100);
        var finalFare = regularFare - discountAmount;

        _logger.LogInformation("Discount applied. RegularFare: {RegularFare}, DiscountPercentage: {DiscountPercentage}%, DiscountAmount: {DiscountAmount}, FinalFare: {FinalFare}",
            regularFare, discountType.DiscountPercentage, discountAmount, finalFare);

        return finalFare;
    }

    #endregion
}