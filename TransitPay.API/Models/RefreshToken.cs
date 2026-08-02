using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TransitPay.API.Models;

[Table("refresh_tokens")]
public class RefreshToken
{
    [Key]
    [Column("token_id")]
    public int TokenId { get; set; }

    [ForeignKey(nameof(User))]
    [Column("user_id")]
    public int UserId { get; set; }

    [Column("token")]
    public string Token { get; set; } = string.Empty;

    [Column("expires_at")]
    public DateTime ExpiresAt { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("revoked")]
    public bool Revoked { get; set; } = false;

    public User? User { get; set; }
}
