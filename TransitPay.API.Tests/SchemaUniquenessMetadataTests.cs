using System.Linq;
using Microsoft.EntityFrameworkCore;
using TransitPay.API.Data;
using TransitPay.API.Models;
using Xunit;

namespace TransitPay.API.Tests;

public class SchemaUniquenessMetadataTests
{
    private static TransitPayDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<TransitPayDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new TransitPayDbContext(options);
    }

    [Fact]
    public void BusinessIdentifierUniquenessRules_ShouldBeExposedInModelMetadata()
    {
        using var context = CreateContext();

        var discountTypeEntity = context.Model.FindEntityType(typeof(DiscountType));
        var terminalEntity = context.Model.FindEntityType(typeof(Terminal));
        var transactionEntity = context.Model.FindEntityType(typeof(Transaction));

        Assert.NotNull(discountTypeEntity);
        Assert.NotNull(terminalEntity);
        Assert.NotNull(transactionEntity);

        Assert.Contains(
            discountTypeEntity!.GetIndexes(),
            i => i.IsUnique && i.Properties.Select(p => p.Name).SequenceEqual([nameof(DiscountType.Name)]));

        Assert.Contains(
            terminalEntity!.GetIndexes(),
            i => i.IsUnique && i.Properties.Select(p => p.Name).SequenceEqual([nameof(Terminal.TerminalName)]));


        Assert.Contains(
            transactionEntity!.GetIndexes(),
            i => i.IsUnique && i.Properties.Select(p => p.Name).SequenceEqual([nameof(Transaction.ReferenceNumber)]));
    }
}