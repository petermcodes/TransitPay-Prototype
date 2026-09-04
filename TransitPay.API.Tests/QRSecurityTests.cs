using System.Text;
using Microsoft.Extensions.Logging.Abstractions;
using TransitPay.API.Data;
using TransitPay.API.Services;
using TransitPay.API.Models;
using TransitPay.API.Interfaces;
using TransitPay.API.Utilities;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace TransitPay.API.Tests;

/// <summary>
/// Security tests for the QR ticket system: signature verification, tamper rejection,
/// and payload structure constraints.
/// </summary>
public class QRSecurityTests
{
    private static TransitPayDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<TransitPayDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new TransitPayDbContext(options);
    }

    private class TestKeyProvider : ISecurityKeyProvider
    {
        private readonly byte[] _key = Encoding.UTF8.GetBytes("01234567012345670123456701234567");
        public byte[] GetSigningKeyBytes() => _key;
        public Microsoft.IdentityModel.Tokens.SymmetricSecurityKey GetSymmetricSecurityKey() => new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(_key);
    }

    [Fact]
    public async Task GenerateOrRetrieveQR_DoesNotEmbedFullCardNumber_InPayload()
    {
        var context = CreateContext();
        var card = new Card
        {
            CardNumber = "4111111111111111",
            Status = Enums.CardStatus.ACTIVE,
            PassengerType = Enums.PassengerType.Passenger,
            IssueDate = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };
        context.Cards.Add(card);
        await context.SaveChangesAsync();

        var qrService = new QRService(context, new TestKeyProvider(), NullLogger<QRService>.Instance);

        var ticket = await qrService.GenerateOrRetrieveQRAsync(card.CardId);

        Assert.NotNull(ticket);
        // Ensure response exposes only masked card number
        Assert.Equal(CardFormatter.MaskCardNumber(card.CardNumber), ticket.MaskedCardNumber);

        // Decode payload (Base64Url-encoded) and ensure full card number is not present
        var data = ticket.Data.Replace('-', '+').Replace('_', '/');
        switch (data.Length % 4)
        {
            case 2: data += "=="; break;
            case 3: data += "="; break;
        }
        var json = Encoding.UTF8.GetString(Convert.FromBase64String(data));
        Assert.DoesNotContain(card.CardNumber, json);
    }

    [Fact]
    public async Task GenerateOrRetrieveQR_PayloadIsMinimal_ForReliableScanning()
    {
        var context = CreateContext();
        var card = new Card
        {
            CardNumber = "4111111111111111",
            Status = Enums.CardStatus.ACTIVE,
            PassengerType = Enums.PassengerType.Passenger,
            IssueDate = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };
        context.Cards.Add(card);
        await context.SaveChangesAsync();

        var qrService = new QRService(context, new TestKeyProvider(), NullLogger<QRService>.Instance);

        var ticket = await qrService.GenerateOrRetrieveQRAsync(card.CardId);

        Assert.NotNull(ticket);

        // Decode payload (Base64Url-encoded)
        var data = ticket.Data.Replace('-', '+').Replace('_', '/');
        switch (data.Length % 4)
        {
            case 2: data += "=="; break;
            case 3: data += "="; break;
        }
        var json = Encoding.UTF8.GetString(Convert.FromBase64String(data));

        // The payload must be minimal (only CardId and Token) to keep the QR code
        // small enough for reliable camera scanning. No QRVersion or CreatedAt.
        Assert.DoesNotContain("QRVersion", json);
        Assert.DoesNotContain("CreatedAt", json);
        Assert.Contains("CardId", json);
        Assert.Contains("Token", json);

        // The total QR value (data + signature) should be well under 200 chars
        // to keep the QR code at a low version for reliable scanning.
        var totalQrValue = $"{ticket.Data}.{ticket.Signature}";
        Assert.True(totalQrValue.Length < 200,
            $"QR value is too long ({totalQrValue.Length} chars). Reduce payload to keep QR scannable.");
    }
}