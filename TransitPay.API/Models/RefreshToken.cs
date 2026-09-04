using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TransitPay.API.Models;

/// <summary>
/// Represents a long-lived refresh token issued alongside the short-lived JWT.
/// Refresh tokens are persisted in the database so they can be revoked (logout),
/// rotated (each refresh issues a new token), and reuse-detected via the
/// <see cref="ReplacedByTokenId"/> family chain (theft mitigation).
/// </summary>
[Table("refresh_tokens")]
public class RefreshToken
{
    /// <summary>Primary key.</summary>
    [Key]
    [Column("token_id")]
    public int TokenId { get; set; }

    /// <summary>The user this token was issued for.</summary>
    [ForeignKey(nameof(User))]
    [Column("user_id")]
    public int UserId { get; set; }

    /// <summary>The token value (64 cryptographically random bytes, Base64-encoded).</summary>
    [Column("token")]
    public string Token { get; set; } = string.Empty;

    /// <summary>When the token expires (7 days after issuance).</summary>
    [Column("expires_at")]
    public DateTime ExpiresAt { get; set; }

    /// <summary>When the token was issued (UTC).</summary>
    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Whether the token has been revoked (logout or rotation).</summary>
    [Column("revoked")]
    public bool Revoked { get; set; } = false;

    /// <summary>
    /// The ID of the replacement refresh token created during rotation.
    /// When a refresh token is used successfully, it is revoked and this
    /// field points to the new token. Used for reuse detection.
    /// </summary>
    [Column("replaced_by_token_id")]
    public int? ReplacedByTokenId { get; set; }

    /// <summary>Navigation property to the owning user.</summary>
    public User? User { get; set; }
}
