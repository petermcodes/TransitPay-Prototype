using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TransitPay.API.Models;

/// <summary>
/// Represents a bus terminal or station. Terminals are the endpoints of fare rules,
/// trip routes, and trip plans, and are referenced by conductor payments as the
/// boarding (origin) and alighting (destination) points.
/// </summary>
[Table("terminals")]
public class Terminal
{
    /// <summary>Primary key.</summary>
    [Key]
    [Column("terminal_id")]
    public int TerminalId { get; set; }

    /// <summary>The display name of the terminal (e.g., "Central Terminal").</summary>
    [Column("terminal_name")]
    public string TerminalName { get; set; } = string.Empty;

    /// <summary>Whether the terminal is currently active and usable in routes/payments.</summary>
    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    /// <summary>When the terminal was created (UTC).</summary>
    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>When the terminal was last updated.</summary>
    [Column("updated_at")]
    public DateTime? UpdatedAt { get; set; }

    /// <summary>Soft-delete timestamp. Null while the record is live.</summary>
    [Column("deleted_at")]
    public DateTime? DeletedAt { get; set; }
}
