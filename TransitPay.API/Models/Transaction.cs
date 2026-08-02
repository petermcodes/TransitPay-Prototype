using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TransitPay.API.Models;

[Table("transactions")]
public class Transaction
{
    [Key]
    [Column("transaction_id")]
    public int TransactionId { get; set; }

    [ForeignKey(nameof(Card))]
    [Column("card_id")]
    public int? CardId { get; set; }

    [ForeignKey(nameof(Station))]
    [Column("station_id")]
    public int? StationId { get; set; }

    [Column("amount")]
    public decimal Amount { get; set; }

    [Column("transaction_type")]
    public string TransactionType { get; set; } = string.Empty;

    [Column("transaction_name")]
    public string TransactionName { get; set; } = string.Empty;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime? UpdatedAt { get; set; }

    [Column("deleted_at")]
    public DateTime? DeletedAt { get; set; }

    public Card? Card { get; set; }
    public Station? Station { get; set; }
}
