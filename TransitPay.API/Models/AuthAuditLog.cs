using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TransitPay.API.Models;

/// <summary>
/// PII-minimized audit record for authentication events.
/// Does NOT store passwords, tokens, or mobile numbers in plain text.
/// A SHA-256 hash of the username (mobile) is stored for correlation.
/// </summary>
[Table("auth_audit_logs")]
public class AuthAuditLog
{
    [Key]
    [Column("audit_id")]
    public long AuditId { get; set; }

    /// <summary>
    /// The user ID involved in the event (nullable — not available for failed registrations).
    /// </summary>
    [Column("user_id")]
    public int? UserId { get; set; }

    /// <summary>
    /// The event type: "register", "login_success", "login_failed", "login_locked",
    /// "refresh_success", "refresh_failed", "logout".
    /// </summary>
    [Column("event_type")]
    public string EventType { get; set; } = string.Empty;

    /// <summary>
    /// SHA-256 hash of the mobile number/username.
    /// Used to correlate events without exposing PII.
    /// </summary>
    [Column("actor_hash")]
    public string? ActorHash { get; set; }

    /// <summary>
    /// The client IP address where the request originated.
    /// </summary>
    [Column("ip_address")]
    public string? IpAddress { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}