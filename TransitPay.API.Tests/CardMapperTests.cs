using TransitPay.API.DTOs.Card;
using TransitPay.API.Enums;
using TransitPay.API.Mappings;
using TransitPay.API.Models;
using Xunit;

namespace TransitPay.API.Tests;

/// <summary>
/// Unit tests for <see cref="TransitPay.API.Mappings.CardMapper"/>: card-masking and
/// DTO mapping shape.
/// </summary>
public class CardMapperTests
{
    private static Card CreateTestCard()
    {
        return new Card
        {
            CardId = 1,
            UserId = 10,
            CardNumber = "4111111111111111",
            Status = CardStatus.ACTIVE,
            PassengerType = PassengerType.Passenger,
            IssueDate = DateTime.UtcNow,
            ExpiryDate = DateTime.UtcNow.AddYears(1),
            CreatedAt = DateTime.UtcNow
        };
    }

    [Fact]
    public void ToDto_MasksCardNumber()
    {
        var card = CreateTestCard();
        var dto = CardMapper.ToDto(card);

        Assert.Equal("•••• 1111", dto.MaskedCardNumber);
        Assert.NotEqual(card.CardNumber, dto.MaskedCardNumber);
    }

    [Fact]
    public void ToDto_MapsAllFields()
    {
        var card = CreateTestCard();
        var dto = CardMapper.ToDto(card);

        Assert.Equal(card.CardId, dto.CardId);
        Assert.Equal(card.Status.ToString(), dto.Status);
        Assert.Equal(card.PassengerType.ToString(), dto.PassengerType);
        Assert.Equal(card.IssueDate, dto.IssueDate);
        Assert.Equal(card.ExpiryDate, dto.ExpiryDate);
    }

    [Fact]
    public void ToDetailsDto_MasksCardNumber()
    {
        var card = CreateTestCard();
        var dto = CardMapper.ToDetailsDto(card);

        Assert.Equal("•••• 1111", dto.MaskedCardNumber);
        Assert.NotEqual(card.CardNumber, dto.MaskedCardNumber);
        Assert.Equal(card.CardId, dto.CardId);
        Assert.Equal(card.UserId, dto.UserId);
        Assert.Equal(card.Status, dto.Status);
        Assert.Equal(card.PassengerType, dto.PassengerType);
    }

    [Fact]
    public void ToCreatedDto_MasksCardNumber()
    {
        var card = CreateTestCard();
        var dto = CardMapper.ToCreatedDto(card);

        Assert.Equal("•••• 1111", dto.MaskedCardNumber);
        Assert.NotEqual(card.CardNumber, dto.MaskedCardNumber);
        Assert.Equal(card.CardId, dto.CardId);
        Assert.Equal(card.UserId, dto.UserId);
        Assert.Equal(card.Status, dto.Status);
        Assert.Equal(card.PassengerType, dto.PassengerType);
    }

    [Fact]
    public void ToDto_DoesNotExposeFullCardNumber()
    {
        var card = CreateTestCard();
        var dto = CardMapper.ToDto(card);

        // The full card number must never appear in the masked DTO
        Assert.DoesNotContain(card.CardNumber, dto.MaskedCardNumber);
    }
}