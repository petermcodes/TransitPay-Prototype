using Microsoft.EntityFrameworkCore;
using TransitPay.API.Data;
using TransitPay.API.Interfaces;
using TransitPay.API.Models;

namespace TransitPay.API.Services;

/// <summary>
/// Service for managing discount types, discount programs, and discount applications.
/// Handles the complete discount lifecycle from creation to application.
/// Approving an application materializes a PassengerDiscount with a snapshotted percentage.
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

    #region Discount Program Management (Admin)

    /// <inheritdoc />
    public async Task<DiscountProgram> CreateDiscountProgramAsync(DiscountProgram discountProgram)
    {
        _logger.LogInformation("Creating discount program: {Name}", discountProgram.Name);

        // Validate discount percentage
        if (discountProgram.DiscountPercentage < 0 || discountProgram.DiscountPercentage > 100)
        {
            throw new InvalidOperationException("Discount percentage must be between 0 and 100.");
        }

        discountProgram.CreatedAt = DateTime.UtcNow;
        discountProgram.IsActive = true;

        _dbContext.DiscountPrograms.Add(discountProgram);
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("Discount program created successfully. DiscountProgramId: {DiscountProgramId}", discountProgram.DiscountProgramId);

        return discountProgram;
    }

    /// <inheritdoc />
    public async Task<DiscountProgram> UpdateDiscountProgramAsync(int discountProgramId, DiscountProgram discountProgram)
    {
        _logger.LogInformation("Updating discount program {DiscountProgramId}", discountProgramId);

        var existingProgram = await _dbContext.DiscountPrograms.FindAsync(discountProgramId);
        if (existingProgram == null)
        {
            throw new InvalidOperationException("Discount program not found.");
        }

        // Validate discount percentage
        if (discountProgram.DiscountPercentage < 0 || discountProgram.DiscountPercentage > 100)
        {
            throw new InvalidOperationException("Discount percentage must be between 0 and 100.");
        }

        // Update properties.
        // NOTE: Existing PassengerDiscount rows preserve their own snapshotted percentage,
        // so changing the program percentage only affects future approvals.
        existingProgram.Name = discountProgram.Name;
        existingProgram.Description = discountProgram.Description;
        existingProgram.DiscountPercentage = discountProgram.DiscountPercentage;
        existingProgram.RequiresApproval = discountProgram.RequiresApproval;
        existingProgram.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("Discount program {DiscountProgramId} updated successfully", discountProgramId);

        return existingProgram;
    }

    /// <inheritdoc />
    public async Task<bool> DeleteDiscountProgramAsync(int discountProgramId)
    {
        _logger.LogInformation("Deleting discount program {DiscountProgramId}", discountProgramId);

        var discountProgram = await _dbContext.DiscountPrograms.FindAsync(discountProgramId);
        if (discountProgram == null)
        {
            return false;
        }

        // Soft delete
        discountProgram.DeletedAt = DateTime.UtcNow;
        discountProgram.IsActive = false;

        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("Discount program {DiscountProgramId} deleted successfully", discountProgramId);

        return true;
    }

    /// <inheritdoc />
    public async Task<bool> ActivateDiscountProgramAsync(int discountProgramId)
    {
        _logger.LogInformation("Activating discount program {DiscountProgramId}", discountProgramId);

        var discountProgram = await _dbContext.DiscountPrograms.FindAsync(discountProgramId);
        if (discountProgram == null)
        {
            throw new InvalidOperationException("Discount program not found.");
        }

        if (discountProgram.DeletedAt != null)
        {
            throw new InvalidOperationException("Cannot activate a deleted discount program.");
        }

        discountProgram.IsActive = true;
        discountProgram.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("Discount program {DiscountProgramId} activated successfully", discountProgramId);

        return true;
    }

    /// <inheritdoc />
    public async Task<bool> DeactivateDiscountProgramAsync(int discountProgramId)
    {
        _logger.LogInformation("Deactivating discount program {DiscountProgramId}", discountProgramId);

        var discountProgram = await _dbContext.DiscountPrograms.FindAsync(discountProgramId);
        if (discountProgram == null)
        {
            throw new InvalidOperationException("Discount program not found.");
        }

        discountProgram.IsActive = false;
        discountProgram.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("Discount program {DiscountProgramId} deactivated successfully", discountProgramId);

        return true;
    }

    /// <inheritdoc />
    public async Task<IEnumerable<DiscountProgram>> GetAllDiscountProgramsAsync()
    {
        _logger.LogInformation("Retrieving all discount programs");

        var discountPrograms = await _dbContext.DiscountPrograms
            .Where(dp => dp.DeletedAt == null)
            .OrderBy(dp => dp.Name)
            .ToListAsync();

        return discountPrograms;
    }

    /// <inheritdoc />
    public async Task<DiscountProgram?> GetDiscountProgramByIdAsync(int discountProgramId)
    {
        _logger.LogInformation("Retrieving discount program {DiscountProgramId}", discountProgramId);

        var discountProgram = await _dbContext.DiscountPrograms
            .FirstOrDefaultAsync(dp => dp.DiscountProgramId == discountProgramId && dp.DeletedAt == null);

        return discountProgram;
    }

    #endregion

    #region Discount Application Management (Passenger)

    /// <inheritdoc />
    public async Task<DiscountApplication> ApplyForDiscountAsync(int cardId, int discountTypeId, int userId, string? discountDocument = null)
    {
        _logger.LogInformation("Processing discount application for card {CardId} to discount type {DiscountTypeId} by user {UserId}", cardId, discountTypeId, userId);

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
            UserId = userId,
            DiscountTypeId = discountTypeId,
            Status = discountType.RequiresApproval ? DiscountApplicationStatus.Pending : DiscountApplicationStatus.Approved,
            DiscountDocument = discountDocument,
            CreatedAt = DateTime.UtcNow
        };

        // If no approval required, auto-approve and materialize the passenger discount
        if (!discountType.RequiresApproval)
        {
            application.ApprovedAt = DateTime.UtcNow;
            application.ApprovedBy = null; // System approval

            _dbContext.DiscountApplications.Add(application);
            await _dbContext.SaveChangesAsync();

            await MaterializePassengerDiscountAsync(application, null, discountType.DiscountPercentage, discountType.Name);
        }
        else
        {
            _dbContext.DiscountApplications.Add(application);
            await _dbContext.SaveChangesAsync();
        }

        _logger.LogInformation("Discount application {ApplicationId} created for card {CardId} by user {UserId}", application.DiscountApplicationId, cardId, userId);

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
            .Include(da => da.DiscountProgram)
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

        // Resolve the discount percentage snapshot source:
        // Prefer the linked DiscountProgram; fall back to the DiscountType.
        decimal snapshotPercentage;
        if (application.DiscountProgram?.DiscountPercentage is decimal programPercentage)
        {
            snapshotPercentage = programPercentage;
        }
        else
        {
            snapshotPercentage = application.DiscountType.DiscountPercentage;
        }

        // Approve the application
        application.Status = DiscountApplicationStatus.Approved;
        application.ApprovedBy = adminId;
        application.ApprovedAt = DateTime.UtcNow;
        application.UpdatedAt = DateTime.UtcNow;

        // Revoke any existing active passenger discount for the card (one active per card)
        var existingActive = await _dbContext.PassengerDiscounts
            .Where(pd => pd.CardId == application.CardId && pd.Status == PassengerDiscountStatus.Active)
            .ToListAsync();
        foreach (var active in existingActive)
        {
            active.Status = PassengerDiscountStatus.Revoked;
            active.UpdatedAt = DateTime.UtcNow;
        }

        await _dbContext.SaveChangesAsync();

        // Materialize the approved PassengerDiscount with the snapshotted percentage
        await MaterializePassengerDiscountAsync(application, adminId, snapshotPercentage, application.DiscountType.Name);

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
            .Include(da => da.User)
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
            .Include(da => da.User)
            .Include(da => da.DiscountType)
            .Where(da => da.DiscountType.DeletedAt == null)
            .OrderByDescending(da => da.CreatedAt)
            .ToListAsync();

        return applications;
    }

    #endregion

    #region Discount Retrieval (Payment Service)

    /// <inheritdoc />
    public async Task<PassengerDiscount?> GetActiveDiscountForCardAsync(int cardId)
    {
        _logger.LogInformation("Retrieving active discount for card {CardId}", cardId);

        var activeDiscount = await _dbContext.PassengerDiscounts
            .Include(pd => pd.DiscountProgram)
            .FirstOrDefaultAsync(pd => pd.CardId == cardId &&
                                       pd.Status == PassengerDiscountStatus.Active &&
                                       pd.DeletedAt == null);

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

    /// <summary>
    /// Materializes a PassengerDiscount from an approved application.
    /// The discount percentage is snapshotted so future program changes do not affect this passenger.
    /// </summary>
    private async Task<PassengerDiscount> MaterializePassengerDiscountAsync(
        DiscountApplication application, int? approvedBy, decimal snapshotPercentage, string programName)
    {
        // Ensure a discount program exists for this application. If none is linked,
        // resolve one by name or create a new one.
        int? discountProgramId = application.DiscountProgramId;

        if (discountProgramId == null)
        {
            var existingProgram = await _dbContext.DiscountPrograms
                .FirstOrDefaultAsync(dp => dp.Name == programName);
            if (existingProgram != null)
            {
                discountProgramId = existingProgram.DiscountProgramId;
            }
            else
            {
                var newProgram = new DiscountProgram
                {
                    Name = programName,
                    Description = $"Auto-created from discount type '{programName}'.",
                    DiscountPercentage = snapshotPercentage,
                    DiscountTypeId = application.DiscountTypeId,
                    IsActive = true,
                    RequiresApproval = true,
                    CreatedAt = DateTime.UtcNow
                };
                _dbContext.DiscountPrograms.Add(newProgram);
                await _dbContext.SaveChangesAsync();
                discountProgramId = newProgram.DiscountProgramId;
            }
        }

        var passengerDiscount = new PassengerDiscount
        {
            CardId = application.CardId,
            DiscountProgramId = discountProgramId,
            DiscountPercentage = snapshotPercentage,
            Status = PassengerDiscountStatus.Active,
            ApprovedBy = approvedBy,
            ApprovedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.PassengerDiscounts.Add(passengerDiscount);
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("Passenger discount {PassengerDiscountId} materialized for card {CardId} at {Percentage}%",
            passengerDiscount.PassengerDiscountId, application.CardId, snapshotPercentage);

        return passengerDiscount;
    }
}
