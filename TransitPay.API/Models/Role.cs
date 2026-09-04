using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using TransitPay.API.Enums;

namespace TransitPay.API.Models;

/// <summary>
/// Represents an authorization role (Passenger, Driver, Admin).
/// Roles are seeded into the database and assigned strictly server-side — client-supplied
/// role information is never trusted.
/// </summary>
[Table("roles")]
public class Role
{
    /// <summary>Primary key.</summary>
    [Key]
    [Column("role_id")]
    public int RoleId { get; set; }

    /// <summary>The role name (Passenger, Driver, Admin).</summary>
    [Column("role_name")]
    public RoleName RoleName { get; set; }

    /// <summary>When the role was seeded/created (UTC).</summary>
    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>When the role was last updated.</summary>
    [Column("updated_at")]
    public DateTime? UpdatedAt { get; set; }

    /// <summary>Navigation property to all users assigned this role.</summary>
    public ICollection<User> Users { get; set; } = new List<User>();
}
