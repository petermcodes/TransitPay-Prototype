using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TransitPay.API.Models;

[Table("cards")]
public class Card
{
    [Key]
    [Column("card_id")]
    public int CardId { get; set; }

    [ForeignKey(nameof(User))]
    [Column("user_id")]
    public int? UserId { get; set; }

    [Column("card_number")]
    public string CardNumber { get; set; } = string.Empty;

    [Column("issue_date")]
    public DateTime IssueDate { get; set; } = DateTime.UtcNow;

    [Column("expiry_date")]
    public DateTime? ExpiryDate { get; set; }

    [Column("status")]
    public string Status { get; set; } = "ACTIVE";

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime? UpdatedAt { get; set; }

    [Column("deleted_at")]
    public DateTime? DeletedAt { get; set; }

    public User? User { get; set; }
    public Wallet? Wallet { get; set; }
    public ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
}
