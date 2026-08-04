using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using TransitPay.API.Enums;

namespace TransitPay.API.Models;

[Table("roles")]
public class Role
{
    [Key]
    [Column("role_id")]
    public int RoleId { get; set; }

    [Column("role_name")]
    public RoleName RoleName { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime? UpdatedAt { get; set; }

    public ICollection<User> Users { get; set; } = new List<User>();
}