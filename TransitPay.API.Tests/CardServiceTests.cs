using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using TransitPay.API.Data;
using TransitPay.API.DTOs.Card;
using TransitPay.API.Enums;
using TransitPay.API.Exceptions;
using TransitPay.API.Models;
using TransitPay.API.Services;
using Xunit;

namespace TransitPay.API.Tests;

public class CardServiceTests
{
    private static TransitPayDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<TransitPayDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        return new TransitPayDbContext(options);
    }

    private static CardService CreateService(TransitPayDbContext context)
    {
        return new CardService(context, NullLogger<CardService>.Instance);
    }

    [Fact]
    public async Task GetCardByUserIdAsync_ReturnsCard_WhenUserHasCard()
    {
        var context = CreateContext();
        context.Cards.Add(new Card
        {
            CardNumber = "4111111111111111",
            UserId = 10,
            Status = CardStatus.ACTIVE,
            PassengerType = PassengerType.Passenger,
            IssueDate = DateTime.UtcNow,
            ExpiryDate = DateTime.UtcNow.AddYears(1)
        });
        await context.SaveChangesAsync();

        var service = CreateService(context);
        var result = await service.GetCardByUserIdAsync(10);

        Assert.NotNull(result);
        Assert.Equal("•••• 1111", result!.MaskedCardNumber);
        Assert.Equal("ACTIVE", result.Status);
    }

    [Fact]
    public async Task GetCardByUserIdAsync_ReturnsNull_WhenNoCard()
    {
        var context = CreateContext();
        var service = CreateService(context);

        var result = await service.GetCardByUserIdAsync(999);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetCardByUserIdAsync_MasksCardNumber()
    {
        var context = CreateContext();
        context.Cards.Add(new Card
        {
            CardNumber = "4111111111114821",
            UserId = 5,
            Status = CardStatus.ACTIVE,
            PassengerType = PassengerType.Passenger,
            IssueDate = DateTime.UtcNow
        });
        await context.SaveChangesAsync();

        var service = CreateService(context);
        var result = await service.GetCardByUserIdAsync(5);

        Assert.NotNull(result);
        Assert.Equal("•••• 4821", result!.MaskedCardNumber);
        Assert.DoesNotContain("4111111111114821", result.MaskedCardNumber);
    }

    [Fact]
    public async Task GetCardByUserIdAsync_ExcludesSoftDeletedCards()
    {
        var context = CreateContext();
        context.Cards.Add(new Card
        {
            CardNumber = "4111111111111111",
            UserId = 10,
            Status = CardStatus.ACTIVE,
            PassengerType = PassengerType.Passenger,
            IssueDate = DateTime.UtcNow,
            DeletedAt = DateTime.UtcNow
        });
        await context.SaveChangesAsync();

        var service = CreateService(context);
        var result = await service.GetCardByUserIdAsync(10);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetCardByNumberAsync_ReturnsCard_WhenExists()
    {
        var context = CreateContext();
        context.Cards.Add(new Card
        {
            CardNumber = "4111111111111111",
            UserId = 10,
            Status = CardStatus.ACTIVE,
            PassengerType = PassengerType.Passenger,
            IssueDate = DateTime.UtcNow
        });
        await context.SaveChangesAsync();

        var service = CreateService(context);
        var result = await service.GetCardByNumberAsync("4111111111111111");

        Assert.NotNull(result);
        // The API contract exposes only the masked card number in DTOs
        Assert.Equal("•••• 1111", result!.MaskedCardNumber);
    }

    [Fact]
    public async Task GetCardByNumberAsync_ReturnsNull_WhenNotExists()
    {
        var context = CreateContext();
        var service = CreateService(context);

        var result = await service.GetCardByNumberAsync("9999999999999999");

        Assert.Null(result);
    }

    [Fact]
    public async Task CreateCardAsync_CreatesCardAndWallet()
    {
        var context = CreateContext();
        var service = CreateService(context);

        var result = await service.CreateCardAsync(new CardRequestDto
        {
            CardNumber = "4111111111111111",
            UserId = 10
        });

        Assert.NotNull(result);
        // Creation response presents masked card number only
        Assert.Equal("•••• 1111", result.MaskedCardNumber);
        Assert.Equal(CardStatus.ACTIVE, result.Status);

        // Verify wallet was created
        var wallet = await context.Wallets.FirstOrDefaultAsync();
        Assert.NotNull(wallet);
        Assert.Equal(0m, wallet!.Balance);
    }

    [Fact]
    public async Task CreateCardAsync_ThrowsDuplicateCardException_WhenDuplicate()
    {
        var context = CreateContext();
        context.Cards.Add(new Card
        {
            CardNumber = "4111111111111111",
            Status = CardStatus.ACTIVE,
            PassengerType = PassengerType.Passenger,
            IssueDate = DateTime.UtcNow
        });
        await context.SaveChangesAsync();

        var service = CreateService(context);

        await Assert.ThrowsAsync<DuplicateCardException>(() =>
            service.CreateCardAsync(new CardRequestDto
            {
                CardNumber = "4111111111111111",
                UserId = 10
            }));
    }

    [Fact]
    public async Task ValidateCardAsync_ReturnsCardWithWalletBalance()
    {
        var context = CreateContext();
        var card = new Card
        {
            CardNumber = "4111111111111111",
            UserId = 10,
            Status = CardStatus.ACTIVE,
            PassengerType = PassengerType.Passenger,
            IssueDate = DateTime.UtcNow
        };
        context.Cards.Add(card);
        context.Wallets.Add(new Wallet { Card = card, Balance = 50m, Status = CardStatus.ACTIVE });
        await context.SaveChangesAsync();

        var service = CreateService(context);
        var result = await service.ValidateCardAsync("4111111111111111");

        Assert.NotNull(result);
        Assert.Equal(50m, result!.Balance);
        Assert.Equal(CardStatus.ACTIVE, result.Status);
    }

    [Fact]
    public async Task ValidateCardAsync_ReturnsNull_WhenNotExists()
    {
        var context = CreateContext();
        var service = CreateService(context);

        var result = await service.ValidateCardAsync("9999999999999999");

        Assert.Null(result);
    }

    [Fact]
    public async Task ValidateCardAsync_ReturnsZeroBalance_WhenNoWallet()
    {
        var context = CreateContext();
        context.Cards.Add(new Card
        {
            CardNumber = "4111111111111111",
            UserId = 10,
            Status = CardStatus.ACTIVE,
            PassengerType = PassengerType.Passenger,
            IssueDate = DateTime.UtcNow
        });
        await context.SaveChangesAsync();

        var service = CreateService(context);
        var result = await service.ValidateCardAsync("4111111111111111");

        Assert.NotNull(result);
        Assert.Equal(0m, result!.Balance);
    }
}