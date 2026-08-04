using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using TransitPay.API.Enums;

namespace TransitPay.API.Models;

[Table("wallets")]
public class Wallet
{
    [Key]
    [Column("wallet_id")]
    public int WalletId { get; set; }

    [ForeignKey(nameof(Card))]
    [Column("card_id")]
    public int CardId { get; set; }

    [Column("balance")]
    public decimal Balance { get; set; } = 0;

    [Column("status")]
    public CardStatus Status { get; set; } = CardStatus.ACTIVE;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime? UpdatedAt { get; set; }

    [Column("deleted_at")]
    public DateTime? DeletedAt { get; set; }

    public Card? Card { get; set; }
}
