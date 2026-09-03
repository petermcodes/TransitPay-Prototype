using TransitPay.API.Models;

namespace TransitPay.API.Interfaces;

/// <summary>
/// Service for managing discount types, discount programs, and discount applications.
/// Handles the complete discount lifecycle from creation to application:
///   - Discount Types and Programs are configured by Admins.
///   - Passengers apply for a discount against a card.
///   - Approving an application materializes a PassengerDiscount with a snapshotted
///     percentage (the discount rate is frozen at approval time).
///   - The payment flow consumes the active PassengerDiscount via <see cref="GetActiveDiscountForCardAsync"/>.
/// </summary>
public interface IDiscountService
{
    // ── Discount Type Management (Admin) ──────────────────────────────────────

    /// <summary>Creates a new discount type. The percentage must be between 0 and 100.</summary>
    Task<DiscountType> CreateDiscountTypeAsync(DiscountType discountType);

    /// <summary>Updates an existing discount type's name, description, percentage, and approval requirement.</summary>
    Task<DiscountType> UpdateDiscountTypeAsync(int discountTypeId, DiscountType discountType);

    /// <summary>Soft-deletes a discount type (sets DeletedAt and IsActive = false).</summary>
    Task<bool> DeleteDiscountTypeAsync(int discountTypeId);

    /// <summary>Re-activates a discount type. Cannot activate a soft-deleted type.</summary>
    Task<bool> ActivateDiscountTypeAsync(int discountTypeId);

    /// <summary>Deactivates a discount type without deleting it.</summary>
    Task<bool> DeactivateDiscountTypeAsync(int discountTypeId);

    /// <summary>Retrieves all non-deleted discount types, ordered by name.</summary>
    Task<IEnumerable<DiscountType>> GetAllDiscountTypesAsync();

    /// <summary>Retrieves a single non-deleted discount type, or <c>null</c>.</summary>
    Task<DiscountType?> GetDiscountTypeByIdAsync(int discountTypeId);

    // ── Discount Program Management (Admin) ───────────────────────────────────

    /// <summary>Creates a new discount program. The percentage must be between 0 and 100.</summary>
    Task<DiscountProgram> CreateDiscountProgramAsync(DiscountProgram discountProgram);

    /// <summary>
    /// Updates an existing discount program. Existing PassengerDiscount rows keep their
    /// snapshotted percentage — changing the program only affects future approvals.
    /// </summary>
    Task<DiscountProgram> UpdateDiscountProgramAsync(int discountProgramId, DiscountProgram discountProgram);

    /// <summary>Soft-deletes a discount program.</summary>
    Task<bool> DeleteDiscountProgramAsync(int discountProgramId);

    /// <summary>Re-activates a discount program. Cannot activate a soft-deleted program.</summary>
    Task<bool> ActivateDiscountProgramAsync(int discountProgramId);

    /// <summary>Deactivates a discount program without deleting it.</summary>
    Task<bool> DeactivateDiscountProgramAsync(int discountProgramId);

    /// <summary>Retrieves all non-deleted discount programs, ordered by name.</summary>
    Task<IEnumerable<DiscountProgram>> GetAllDiscountProgramsAsync();

    /// <summary>Retrieves a single non-deleted discount program, or <c>null</c>.</summary>
    Task<DiscountProgram?> GetDiscountProgramByIdAsync(int discountProgramId);

    // ── Discount Application Management (Passenger) ──────────────────────────

    /// <summary>
    /// Creates a discount application for a card. When the discount type does not require
    /// approval, the application is auto-approved and the PassengerDiscount is materialized
    /// immediately (system approval). Otherwise it stays Pending for an admin to review.
    /// </summary>
    Task<DiscountApplication> ApplyForDiscountAsync(int cardId, int discountTypeId, int userId, string? discountDocument = null);

    /// <summary>Retrieves all applications for a card, newest first.</summary>
    Task<IEnumerable<DiscountApplication>> GetApplicationsByCardAsync(int cardId);

    // ── Discount Approval Workflow (Admin) ────────────────────────────────────

    /// <summary>
    /// Approves a pending application, materializes (or re-materializes) the PassengerDiscount
    /// with a snapshotted percentage, and revokes any prior active discount for the card
    /// (one active discount per card).
    /// </summary>
    Task<DiscountApplication> ApproveDiscountApplicationAsync(int applicationId, int adminId);

    /// <summary>Rejects a pending application with an optional reason.</summary>
    Task<DiscountApplication> RejectDiscountApplicationAsync(int applicationId, int adminId, string? rejectionReason = null);

    /// <summary>Retrieves all Pending applications for the admin review queue.</summary>
    Task<IEnumerable<DiscountApplication>> GetPendingApplicationsAsync();

    /// <summary>Retrieves all applications (every status) for admin management.</summary>
    Task<IEnumerable<DiscountApplication>> GetAllApplicationsAsync();

    // ── Discount Retrieval (Payment Service) ──────────────────────────────────

    /// <summary>
    /// Retrieves the current ACTIVE, non-expired PassengerDiscount for a card.
    /// This is the single source of truth the payment flow reads to apply a discount.
    /// </summary>
    Task<PassengerDiscount?> GetActiveDiscountForCardAsync(int cardId);

    /// <summary>
    /// Applies a discount type's percentage to a regular fare and returns the final fare.
    /// Returns the regular fare unchanged when no discount type is supplied or the type
    /// is not found/inactive.
    /// </summary>
    Task<decimal> CalculateDiscountedFareAsync(decimal regularFare, int? discountTypeId);
}
