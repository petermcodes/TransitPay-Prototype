using Microsoft.EntityFrameworkCore;
using TransitPay.API.Data;
using TransitPay.API.Interfaces;
using TransitPay.API.Models;

namespace TransitPay.API.Services;

public class PaymentService : IPaymentService
{
    private readonly TransitPayDbContext _dbContext;

    public PaymentService(TransitPayDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<object> ProcessPaymentAsync(int cardId, int stationId, decimal amount)
    {
        var card = await _dbContext.Cards.FindAsync(cardId);
        if (card == null)
        {
            return new { success = false, message = "Card not found." };
        }

        if (card.Status != "ACTIVE")
        {
            return new { success = false, message = "Card is not active." };
        }

        var wallet = await _dbContext.Wallets
            .Where(w => w.CardId == cardId)
            .FirstOrDefaultAsync();
        if (wallet == null)
        {
            wallet = new Wallet
            {
                CardId = cardId,
                Balance = 0m,
                Status = "ACTIVE",
                CreatedAt = DateTime.UtcNow
            };
            _dbContext.Wallets.Add(wallet);
            await _dbContext.SaveChangesAsync();
        }

        var selectedStation = await _dbContext.Stations.FindAsync(stationId);
        var fareRule = await _dbContext.FareRules
            .Where(fr => fr.IsActive && fr.DestinationStationId == stationId)
            .OrderByDescending(fr => fr.EffectiveDate)
            .FirstOrDefaultAsync();

        var fareAmount = amount > 0 ? amount : fareRule?.FareAmount ?? 0m;
        if (fareAmount <= 0)
        {
            return new { success = false, message = "No fare rule found for this journey." };
        }

        if (wallet.Balance < fareAmount)
        {
            return new { success = false, message = "Insufficient balance." };
        }

        wallet.Balance -= fareAmount;
        wallet.UpdatedAt = DateTime.UtcNow;
        _dbContext.Transactions.Add(new Models.Transaction
        {
            CardId = cardId,
            StationId = stationId,
            Amount = fareAmount,
            TransactionType = "PAYMENT",
            TransactionName = selectedStation != null ? $"Fare payment to {selectedStation.StationName}" : "Fare payment",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });
        await _dbContext.SaveChangesAsync();

        return new { success = true, message = "Payment completed successfully.", data = new { cardId, stationId, amount = fareAmount, balance = wallet.Balance, stationName = selectedStation?.StationName } };
    }
}
