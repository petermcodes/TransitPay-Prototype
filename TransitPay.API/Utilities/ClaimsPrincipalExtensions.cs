using System.Security.Claims;

namespace TransitPay.API.Utilities;

/// <summary>
/// Extension methods for resolving authenticated identity from JWT claims.
/// This is the single authoritative source for extracting the authenticated user's ID.
/// All ownership validation across the application must use this extension method
/// rather than trusting client-supplied identifiers.
/// </summary>
public static class ClaimsPrincipalExtensions
{
    /// <summary>
    /// Extracts the authenticated user's ID from the JWT <see cref="ClaimTypes.NameIdentifier"/> claim.
    /// This is the single authoritative claim used for identity resolution and ownership validation.
    /// </summary>
    /// <param name="user">The authenticated <see cref="ClaimsPrincipal"/>.</param>
    /// <returns>The authenticated user's ID, or <c>null</c> if the claim is missing or invalid.</returns>
    public static int? GetAuthenticatedUserId(this ClaimsPrincipal user)
    {
        var userIdClaim = user.FindFirstValue(ClaimTypes.NameIdentifier);

        if (int.TryParse(userIdClaim, out var userId))
        {
            return userId;
        }

        return null;
    }
}