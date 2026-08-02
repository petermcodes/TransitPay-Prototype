using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TransitPay.API.Models;

[Table("stations")]
public class Station
{
    [Key]
    [Column("station_id")]
    public int StationId { get; set; }

    [ForeignKey(nameof(Town))]
    [Column("town_id")]
    public int TownId { get; set; }

    [Column("station_name")]
    public string StationName { get; set; } = string.Empty;

    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime? UpdatedAt { get; set; }

    [Column("deleted_at")]
    public DateTime? DeletedAt { get; set; }

    public Town? Town { get; set; }
    public ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
}
