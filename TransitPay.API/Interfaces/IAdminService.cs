using TransitPay.API.DTOs.Admin;
using TransitPay.API.Models;

namespace TransitPay.API.Interfaces;

/// <summary>
/// Service for the Administration Domain.
/// Handles all user, driver, and administrator account management.
/// This service is the single entry point for administrative account operations.
/// Authentication (AuthController/AuthService) is never responsible for
/// creating or managing Driver or Administrator accounts.
/// </summary>
public interface IAdminService
{
    /// <summary>
    /// Creates a new Driver account. The driver is created active immediately
    /// (IsActive = true) — no approval workflow.
    /// </summary>
    Task<User> CreateDriverAsync(CreateDriverRequest request);

    /// <summary>
    /// Creates a new Administrator account.
    /// Only existing Administrators or the initial bootstrap may create additional Admins.
    /// </summary>
    Task<User> CreateAdministratorAsync(CreateAdministratorRequest request);

    /// <summary>
    /// Activates a user account (sets IsActive = true).
    /// </summary>
    Task<User> ActivateUserAsync(int userId);

    /// <summary>
    /// Deactivates a user account (sets IsActive = false).
    /// Deactivated users cannot authenticate. No soft-delete is performed —
    /// historical transactions and audit logs remain intact.
    /// </summary>
    Task<User> DeactivateUserAsync(int userId);

    /// <summary>
    /// Resets a user's password. Applies the password policy, hashes the new password,
    /// clears any account lockout, and resets failed login attempts.
    /// </summary>
    Task<User> ResetUserPasswordAsync(int userId, string newPassword);

    /// <summary>
    /// Unlocks a locked user account. Clears LockoutEnd and resets FailedLoginAttempts.
    /// </summary>
    Task<User> UnlockUserAccountAsync(int userId);

    /// <summary>
    /// Updates a user's personal information (first name, last name, mobile number).
    /// </summary>
    Task<User> UpdateUserAsync(int userId, UpdateUserRequest request);

    /// <summary>
    /// Retrieves all users with pagination.
    /// </summary>
    Task<(List<User> Users, int Total)> GetUsersAsync(int page, int pageSize);

    /// <summary>
    /// Retrieves all drivers with pagination.
    /// </summary>
    Task<(List<User> Drivers, int Total)> GetDriversAsync(int page, int pageSize);
}