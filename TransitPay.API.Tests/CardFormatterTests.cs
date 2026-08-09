using TransitPay.API.Utilities;
using Xunit;

namespace TransitPay.API.Tests;

public class CardFormatterTests
{
    [Fact]
    public void MaskCardNumber_PreservesLastFourDigits()
    {
        var result = CardFormatter.MaskCardNumber("4111111111114821");
        Assert.Equal("•••• 4821", result);
    }

    [Fact]
    public void MaskCardNumber_ReturnsNull_ForNullInput()
    {
        var result = CardFormatter.MaskCardNumber(null);
        Assert.Null(result);
    }

    [Fact]
    public void MaskCardNumber_ReturnsEmpty_ForEmptyString()
    {
        var result = CardFormatter.MaskCardNumber("");
        Assert.Equal("", result);
    }

    [Fact]
    public void MaskCardNumber_ReturnsInput_ForWhitespace()
    {
        var result = CardFormatter.MaskCardNumber("   ");
        Assert.Equal("   ", result);
    }

    [Fact]
    public void MaskCardNumber_ReturnsInput_ForShortString()
    {
        var result = CardFormatter.MaskCardNumber("123");
        Assert.Equal("123", result);
    }

    [Fact]
    public void MaskCardNumber_HandlesExactlyFourDigits()
    {
        var result = CardFormatter.MaskCardNumber("4821");
        Assert.Equal("•••• 4821", result);
    }

    [Fact]
    public void MaskCardNumber_HandlesSixteenDigitCard()
    {
        var result = CardFormatter.MaskCardNumber("4111111111111111");
        Assert.Equal("•••• 1111", result);
    }
}