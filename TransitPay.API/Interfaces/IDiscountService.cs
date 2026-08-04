using TransitPay.API.Models;

namespace TransitPay.API.Interfaces;

/// <summary>
/// Service for managing discount types and discount applications.
/// Handles the complete discount lifecycle from creation to application.
/// </summary>
public interface IDiscountService
{
    // Discount Type Management (Admin)
    Task<DiscountType> CreateDiscountTypeAsync(DiscountType discountType);
    Task<DiscountType> UpdateDiscountTypeAsync(int discountTypeId, DiscountType discountType);
    Task<bool> DeleteDiscountTypeAsync(int discountTypeId);
    Task<bool> ActivateDiscountTypeAsync(int discountTypeId);
    Task<bool> DeactivateDiscountTypeAsync(int discountTypeId);
    Task<IEnumerable<DiscountType>> GetAllDiscountTypesAsync();
    Task<DiscountType?> GetDiscountTypeByIdAsync(int discountTypeId);

    // Discount Application Management (Passenger)
    Task<DiscountApplication> ApplyForDiscountAsync(int cardId, int discountTypeId, string? discountDocument = null);
    Task<IEnumerable<DiscountApplication>> GetApplicationsByCardAsync(int cardId);

    // Discount Approval Workflow (Admin)
    Task<DiscountApplication> ApproveDiscountApplicationAsync(int applicationId, int adminId);
    Task<DiscountApplication> RejectDiscountApplicationAsync(int applicationId, int adminId, string? rejectionReason = null);
    Task<IEnumerable<DiscountApplication>> GetPendingApplicationsAsync();
    Task<IEnumerable<DiscountApplication>> GetAllApplicationsAsync();

    // Discount Retrieval (Payment Service)
    Task<DiscountApplication?> GetActiveDiscountForCardAsync(int cardId);
    Task<decimal> CalculateDiscountedFareAsync(decimal regularFare, int? discountTypeId);
}