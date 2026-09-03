namespace TransitPay.API.Interfaces;

using TransitPay.API.DTOs.Auth;
using TransitPay.API.Models;

/// <summary>
/// Handles the passenger authentication lifecycle: registration, login, token refresh,
/// and logout. Role assignment is always done server-side (registration is Passenger-only);
/// Driver/Admin accounts are managed by <see cref="IAdminService"/>.
/// </summary>
public interface IAuthService
{
    /// <summary>
    /// Registers a new Passenger account. The Passenger role is always assigned
    /// server-side regardless of any client-supplied values. Driver and Admin
    /// accounts are never created through this method.
    /// </summary>
    Task<RegisterResponse> RegisterAsync(string username, string firstName, string lastName, string mobileNumber, string password);

    /// <summary>
    /// Authenticates a user by username, mobile number, or Driver ID (e.g., DRV-000010).
    /// Enforces the account lockout policy (failed attempts → temporary lockout) and always
    /// returns a generic "Invalid credentials." message to prevent account enumeration.
    /// On success, issues a JWT access token and a refresh token and writes a login audit event.
    /// </summary>
    /// <param name="username">The username, mobile number, or Driver ID.</param>
    /// <param name="password">The plaintext password to verify.</param>
    /// <returns>A <see cref="LoginResponse"/> carrying the tokens and user profile on success.</returns>
    Task<LoginResponse> LoginAsync(string username, string password);

    /// <summary>
    /// Rotates the user's refresh token and issues a fresh JWT access token.
    /// The current refresh token is revoked, a new one is linked via ReplacedByTokenId,
    /// and reuse of an already-rotated token revokes the entire token family (theft mitigation).
    /// </summary>
    /// <param name="userId">The authenticated user's ID.</param>
    /// <param name="refreshToken">The current refresh token to rotate.</param>
    /// <returns>A <see cref="RefreshTokenResponse"/> with fresh tokens, or a failure response when invalid.</returns>
    Task<RefreshTokenResponse> RefreshTokenAsync(int userId, string refreshToken);

    /// <summary>
    /// Securely logs out a user by revoking all of their active refresh tokens.
    /// The JWT access token expires naturally (Stateless JWT).
    /// </summary>
    Task<bool> LogoutAsync(int userId);

    /// <summary>
    /// Retrieves a user by their ID, including their role information.
    /// Used for token validation and user info retrieval.
    /// </summary>
    Task<User?> GetUserByIdAsync(int userId);
}
