using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TransitPay.API.Models.History;

[Table("driver_edit_history")]
public class DriverEditHistory
{
    [Key]
    [Column("history_id")]
    public int HistoryId { get; set; }

    [Column("original_record_id")]
    public int OriginalRecordId { get; set; }

    [Column("operation")]
    [MaxLength(20)]
    public string Operation { get; set; } = "EDIT";

    [Column("performed_by_user_id")]
    public int PerformedByUserId { get; set; }

    [Column("performed_at")]
    public DateTime PerformedAt { get; set; } = DateTime.UtcNow;

    [Column("original_data")]
    public string OriginalData { get; set; } = "{}";

    [Column("reason")]
    [MaxLength(500)]
    public string? Reason { get; set; }
}