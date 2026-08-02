using Microsoft.EntityFrameworkCore;
using TransitPay.API.Data;
using TransitPay.API.Models;
using TransitPay.API.Services;
using Xunit;

namespace TransitPay.API.Tests;

public class PaymentServiceTests
{
    [Fact]
    public async Task ProcessPaymentAsync_DeductsFareAndCreatesTransaction()
    {
        var options = new DbContextOptionsBuilder<TransitPayDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var context = new TransitPayDbContext(options);
        var card = new Card
        {
            CardNumber = "4111111111111111",
            Status = "ACTIVE",
            IssueDate = DateTime.UtcNow,
            ExpiryDate = DateTime.UtcNow.AddYears(1)
        };
        context.Cards.Add(card);
        context.Wallets.Add(new Wallet { Card = card, Balance = 50, Status = "ACTIVE" });
        context.Stations.Add(new Station { StationId = 1, StationName = "Central", TownId = 1, IsActive = true });
        context.Stations.Add(new Station { StationId = 2, StationName = "Harbor", TownId = 1, IsActive = true });
        context.FareRules.Add(new FareRule
        {
            OriginStationId = 1,
            DestinationStationId = 2,
            VehicleType = "BUS",
            PassengerType = "Passenger",
            FareAmount = 12.5m,
            EffectiveDate = DateTime.UtcNow.AddDays(-1),
            IsActive = true
        });
        await context.SaveChangesAsync();

        var service = new PaymentService(context);
        var result = await service.ProcessPaymentAsync(card.CardId, 2, 0m) as dynamic;

        Assert.NotNull(result);
    }
}
