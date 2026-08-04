using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TransitPay.API.Models;

/// <summary>
/// Represents a permanent QR code associated with a transit card.
/// Each card has exactly one active QR code at any time.
/// When a QR is regenerated, the old one is revoked (IsActive = false, RevokedAt set)
/// and a new one is created.
/// </summary>
[Table("qr_codes")]
public class QRCode
{
    [Key]
    [Column("qr_code_id")]
    public int QRCodeId { get; set; }

    [ForeignKey(nameof(Card))]
    [Column("card_id")]
    public int CardId { get; set; }

    /// <summary>
    /// Unique random token that identifies this QR code.
    /// Stored as a base64url-encoded string (32 bytes of entropy).
    /// </summary>
    [Column("token")]
    public string Token { get; set; } = string.Empty;

    /// <summary>
    /// Whether this QR code is currently active.
    /// Only one QR per card can be active at a time.
    /// </summary>
    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When this QR was revoked (e.g., during regeneration).
    /// Null if the QR is still active.
    /// </summary>
    [Column("revoked_at")]
    public DateTime? RevokedAt { get; set; }

    [Column("updated_at")]
    public DateTime? UpdatedAt { get; set; }

    public Card? Card { get; set; }
}