namespace TransitPay.API.Interfaces;

using TransitPay.API.DTOs.Auth;
using TransitPay.API.Models;

public interface IAuthService
{
    /// <summary>
    /// Registers a new Passenger account. The Passenger role is always assigned
    /// server-side regardless of any client-supplied values. Driver and Admin
    /// accounts are never created through this method.
    /// </summary>
    Task<RegisterResponse> RegisterAsync(string username, string firstName, string lastName, string mobileNumber, string password);

    Task<LoginResponse> LoginAsync(string username, string password);
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
