using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TransitPay.API.Models;

/// <summary>
/// Represents a configurable discount program that can be applied to fares.
/// Examples: Student (50%), Senior (40%), DISABLED (100%), Promotional (20%).
/// A discount program groups discount applications under a common program definition.
/// </summary>
[Table("discount_programs")]
public class DiscountProgram
{
    [Key]
    [Column("discount_program_id")]
    public int DiscountProgramId { get; set; }

    /// <summary>
    /// Name of the discount program (e.g., "Student", "Senior", "DISABLED", "Promotional").
    /// </summary>
    [Required]
    [Column("name")]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Detailed description of the discount program eligibility and terms.
    /// </summary>
    [Column("description")]
    [MaxLength(500)]
    public string? Description { get; set; }

    /// <summary>
    /// Discount percentage (e.g., 50.00 for 50% off).
    /// Must be between 0 and 100.
    /// </summary>
    [Required]
    [Column("discount_percentage")]
    [Range(0, 100)]
    public decimal DiscountPercentage { get; set; }

    /// <summary>
    /// Whether this discount program is currently active and can be applied.
    /// </summary>
    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Whether this discount program requires admin approval for applications.
    /// </summary>
    [Column("requires_approval")]
    public bool RequiresApproval { get; set; } = true;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime? UpdatedAt { get; set; }

    [Column("deleted_at")]
    public DateTime? DeletedAt { get; set; }

    /// <summary>
    /// The discount type this program is based on (if any).
    /// Links the program to the original discount type definition.
    /// </summary>
    [ForeignKey(nameof(DiscountType))]
    [Column("discount_type_id")]
    public int? DiscountTypeId { get; set; }

    /// <summary>
    /// Navigation property for discount applications using this program.
    /// </summary>
    public ICollection<DiscountApplication> DiscountApplications { get; set; } = new List<DiscountApplication>();

    /// <summary>
    /// Navigation property to the discount type this program is based on.
    /// </summary>
    public DiscountType? DiscountType { get; set; }
}
