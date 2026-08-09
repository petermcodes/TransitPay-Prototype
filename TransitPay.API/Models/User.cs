using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TransitPay.API.Models;

[Table("users")]
public class User
{
    [Key]
    [Column("user_id")]
    public int UserId { get; set; }

    [Column("username")]
    public string Username { get; set; } = string.Empty;

    [ForeignKey(nameof(Role))]
    [Column("role_id")]
    public int RoleId { get; set; }

    [Column("first_name")]
    public string FirstName { get; set; } = string.Empty;

    [Column("last_name")]
    public string LastName { get; set; } = string.Empty;

    [Column("mobile_number")]
    public string MobileNumber { get; set; } = string.Empty;

    [Column("password_hash")]
    public string PasswordHash { get; set; } = string.Empty;

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

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime? UpdatedAt { get; set; }

    [Column("deleted_at")]
    public DateTime? DeletedAt { get; set; }

    [Column("plate_number")]
    public string? PlateNumber { get; set; }

    public Role? Role { get; set; }
    public ICollection<Card> Cards { get; set; } = new List<Card>();
}
