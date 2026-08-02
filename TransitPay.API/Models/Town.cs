using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TransitPay.API.Models;

[Table("towns")]
public class Town
{
    [Key]
    [Column("town_id")]
    public int TownId { get; set; }

    [Column("town_name")]
    public string TownName { get; set; } = string.Empty;

    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime? UpdatedAt { get; set; }

    [Column("deleted_at")]
    public DateTime? DeletedAt { get; set; }

    public ICollection<Station> Stations { get; set; } = new List<Station>();
}
