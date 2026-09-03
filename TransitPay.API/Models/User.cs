using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TransitPay.API.Models;

/// <summary>
/// Represents a user account (Passenger, Driver, or Administrator).
/// Passengers register themselves; Driver and Admin accounts are created by Admins.
/// The account can be deactivated (IsActive = false) or soft-deleted by Admins.
/// </summary>
[Table("users")]
public class User
{
    /// <summary>Primary key.</summary>
    [Key]
    [Column("user_id")]
    public int UserId { get; set; }

    /// <summary>The login name. Passengers/admin use a chosen username; drivers use a generated Driver ID (DRV-xxxxxx).</summary>
    [Column("username")]
    public string Username { get; set; } = string.Empty;

    /// <summary>The role assigned to this account (Passenger, Driver, Admin).</summary>
    [ForeignKey(nameof(Role))]
    [Column("role_id")]
    public int RoleId { get; set; }

    /// <summary>The user's first name.</summary>
    [Column("first_name")]
    public string FirstName { get; set; } = string.Empty;

    /// <summary>The user's last name.</summary>
    [Column("last_name")]
    public string LastName { get; set; } = string.Empty;

    /// <summary>The user's mobile number — a unique login identifier and PII-protected value.</summary>
    [Column("mobile_number")]
    public string MobileNumber { get; set; } = string.Empty;

    /// <summary>The ASP.NET Identity password hash (never the plaintext password).</summary>
    [Column("password_hash")]
    public string PasswordHash { get; set; } = string.Empty;

    /// <summary>Whether the account can authenticate. Deactivated accounts are rejected at login.</summary>
    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// The number of consecutive failed login attempts.
    /// Reset to 0 on successful login. When it reaches the configured
    /// MaxFailedAttempts threshold, LockoutEnd is set.
    /// </summary>
    [Column("failed_login_attempts")]
    public int FailedLoginAttempts { get; set; } = 0;

    /// <summary>
    /// When the account lockout expires (UTC). Null if not locked out.
    /// While LockoutEnd > DateTime.UtcNow, authentication is rejected.
    /// </summary>
    [Column("lockout_end")]
    public DateTime? LockoutEnd { get; set; }

    /// <summary>
    /// When the password was last changed (UTC). Used for security auditing
    /// and future password-expiration policies.
    /// </summary>
    [Column("password_changed_at")]
    public DateTime? PasswordChangedAt { get; set; }

    /// <summary>When the account was created (UTC).</summary>
    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>When the account was last updated.</summary>
    [Column("updated_at")]
    public DateTime? UpdatedAt { get; set; }

    /// <summary>Soft-delete timestamp. Null while the record is live.</summary>
    [Column("deleted_at")]
    public DateTime? DeletedAt { get; set; }

    /// <summary>The vehicle plate number assigned to a driver account (drivers only).</summary>
    [Column("plate_number")]
    public string? PlateNumber { get; set; }

    /// <summary>Navigation property to the assigned role.</summary>
    public Role? Role { get; set; }

    /// <summary>Navigation property to the user's transit cards.</summary>
    public ICollection<Card> Cards { get; set; } = new List<Card>();
}
