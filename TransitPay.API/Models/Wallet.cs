using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using TransitPay.API.Enums;

namespace TransitPay.API.Models;

/// <summary>
/// Represents the stored-value e-wallet bound to a transit card.
/// Fares are deducted from the wallet balance during conductor payments and the
/// balance is topped up through the wallet endpoints. Safe concurrent balance updates
/// are guaranteed by the <see cref="RowVersion"/> optimistic concurrency token and the
/// non-negative balance database check constraint.
/// </summary>
[Table("wallets")]
public class Wallet
{
    /// <summary>Primary key.</summary>
    [Key]
    [Column("wallet_id")]
    public int WalletId { get; set; }

    /// <summary>The transit card this wallet is bound to (one wallet per card).</summary>
    [ForeignKey(nameof(Card))]
    [Column("card_id")]
    public int CardId { get; set; }

    /// <summary>The current wallet balance in peso. Never negative (DB check constraint).</summary>
    [Column("balance")]
    public decimal Balance { get; set; } = 0;

    /// <summary>Wallet state. Payments require an active wallet.</summary>
    [Column("status")]
    public CardStatus Status { get; set; } = CardStatus.ACTIVE;

    /// <summary>When the wallet was created (UTC).</summary>
    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>When the wallet was last updated.</summary>
    [Column("updated_at")]
    public DateTime? UpdatedAt { get; set; }

    /// <summary>Soft-delete timestamp. Null while the record is live.</summary>
    [Column("deleted_at")]
    public DateTime? DeletedAt { get; set; }

    /// <summary>EF Core optimistic concurrency token for safe concurrent balance updates.</summary>
    [ConcurrencyCheck]
    [Column("row_version")]
    public byte[]? RowVersion { get; set; }

    /// <summary>Navigation property to the bound card.</summary>
    public Card? Card { get; set; }
}
