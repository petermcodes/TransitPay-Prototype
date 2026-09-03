using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TransitPay.API.Models.History;

/// <summary>
/// Audit trail of terminal EDIT operations. When an admin edits a terminal,
/// a JSON snapshot of the original record is stored here for forensics and rollback.
/// Newer records are always read-only append-only — never updated or deleted.
/// </summary>
[Table("terminal_edit_history")]
public class TerminalEditHistory
{
    /// <summary>Primary key.</summary>
    [Key]
    [Column("history_id")]
    public int HistoryId { get; set; }

    /// <summary>The ID of the original (terminals) record that was edited.</summary>
    [Column("original_record_id")]
    public int OriginalRecordId { get; set; }

    /// <summary>The operation performed ("EDIT").</summary>
    [Column("operation")]
    [MaxLength(20)]
    public string Operation { get; set; } = "EDIT";

    /// <summary>The admin user who performed the operation.</summary>
    [Column("performed_by_user_id")]
    public int PerformedByUserId { get; set; }

    /// <summary>When the operation was performed (UTC).</summary>
    [Column("performed_at")]
    public DateTime PerformedAt { get; set; } = DateTime.UtcNow;

    /// <summary>JSON snapshot of the original record before the edit.</summary>
    [Column("original_data")]
    public string OriginalData { get; set; } = "{}";

    /// <summary>Optional note explaining why the edit was made.</summary>
    [Column("reason")]
    [MaxLength(500)]
    public string? Reason { get; set; }
}