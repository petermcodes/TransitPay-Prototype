using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TransitPay.API.Models.History;

/// <summary>
/// Audit trail of driver DELETE operations. When an admin deletes a driver account,
/// a JSON snapshot of the deleted record is stored here for forensics.
/// Newer records are always read-only append-only — never updated or deleted.
/// </summary>
[Table("driver_delete_history")]
public class DriverDeleteHistory
{
    /// <summary>Primary key.</summary>
    [Key]
    [Column("history_id")]
    public int HistoryId { get; set; }

    /// <summary>The ID of the original (users) record that was deleted.</summary>
    [Column("original_record_id")]
    public int OriginalRecordId { get; set; }

    /// <summary>The operation performed ("DELETE").</summary>
    [Column("operation")]
    [MaxLength(20)]
    public string Operation { get; set; } = "DELETE";

    /// <summary>The admin user who performed the operation.</summary>
    [Column("performed_by_user_id")]
    public int PerformedByUserId { get; set; }

    /// <summary>When the operation was performed (UTC).</summary>
    [Column("performed_at")]
    public DateTime PerformedAt { get; set; } = DateTime.UtcNow;

    /// <summary>JSON snapshot of the deleted record.</summary>
    [Column("original_data")]
    public string OriginalData { get; set; } = "{}";

    /// <summary>Optional note explaining why the deletion was made.</summary>
    [Column("reason")]
    [MaxLength(500)]
    public string? Reason { get; set; }
}