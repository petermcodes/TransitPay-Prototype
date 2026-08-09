using TransitPay.API.Utilities;
using Xunit;

namespace TransitPay.API.Tests;

public class PasswordPolicyTests
{
    [Theory]
    [InlineData("StrongP@ssw0rd2024")]
    [InlineData("Tr@nsitPay2024!")]
    [InlineData("P@ssw0rd#Secure2024")]
    [InlineData("V3ryStr0ng!Pass")]
    public void Validate_ReturnsValid_WhenPasswordMeetsAllRequirements(string password)
    {
        var result = PasswordPolicy.Validate(password);

        Assert.True(result.IsValid);
        Assert.Null(result.ErrorMessage);
    }

    [Fact]
    public void Validate_ReturnsInvalid_WhenPasswordIsNull()
    {
        var result = PasswordPolicy.Validate(null);

        Assert.False(result.IsValid);
        Assert.NotNull(result.ErrorMessage);
    }

    [Fact]
    public void Validate_ReturnsInvalid_WhenPasswordIsWhitespace()
    {
        var result = PasswordPolicy.Validate("   ");

        Assert.False(result.IsValid);
        Assert.NotNull(result.ErrorMessage);
    }

    [Theory]
    [InlineData("Short1!")]                       // 7 chars — too short
    [InlineData("123456!")]                       // 7 chars — too short
    public void Validate_ReturnsInvalid_WhenPasswordTooShort(string password)
    {
        var result = PasswordPolicy.Validate(password);

        Assert.False(result.IsValid);
        Assert.NotNull(result.ErrorMessage);
    }

    [Fact]
    public void Validate_ReturnsValid_WhenPasswordIsAllUppercase()
    {
        // The relaxed policy does not require lowercase characters
        var result = PasswordPolicy.Validate("UPPERCASE123!");

        Assert.True(result.IsValid);
        Assert.Null(result.ErrorMessage);
    }

    [Fact]
    public void Validate_ReturnsValid_WhenPasswordIsAllLowercase()
    {
        // The relaxed policy does not require uppercase characters
        var result = PasswordPolicy.Validate("alllowercase123!");

        Assert.True(result.IsValid);
        Assert.Null(result.ErrorMessage);
    }

    [Fact]
    public void Validate_ReturnsInvalid_WhenPasswordMissingDigit()
    {
        var result = PasswordPolicy.Validate("NoDigitsHere!");

        Assert.False(result.IsValid);
        Assert.Contains("number", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Validate_ReturnsInvalid_WhenPasswordMissingSpecialCharacter()
    {
        var result = PasswordPolicy.Validate("NoSpecial123456");

        Assert.False(result.IsValid);
        Assert.Contains("special", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("Password12345!")]
    [InlineData("Qwerty123456!")]
    [InlineData("Admin123456!")]
    [InlineData("Welcome12345!")]
    [InlineData("Letmein12345!")]
    public void Validate_ReturnsInvalid_WhenPasswordIsCommon(string password)
    {
        var result = PasswordPolicy.Validate(password);

        Assert.False(result.IsValid);
        Assert.Contains("common", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Validate_ReturnsInvalid_WhenPasswordContainsFirstName()
    {
        var result = PasswordPolicy.Validate("JuanStrongP@ss2024", firstName: "Juan");

        Assert.False(result.IsValid);
        Assert.Contains("personal", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Validate_ReturnsInvalid_WhenPasswordContainsLastName()
    {
        var result = PasswordPolicy.Validate("DelaCruzP@ss2024", lastName: "DelaCruz");

        Assert.False(result.IsValid);
        Assert.Contains("personal", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Validate_ReturnsInvalid_WhenPasswordContainsMobileNumber()
    {
        var result = PasswordPolicy.Validate("09171234567P@ss", mobileNumber: "09171234567");

        Assert.False(result.IsValid);
        Assert.Contains("mobile", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Validate_ReturnsValid_WhenPasswordDoesNotContainPersonalInfo()
    {
        var result = PasswordPolicy.Validate(
            "S3cureTp!2024",
            firstName: "Juan",
            lastName: "Dela Cruz",
            mobileNumber: "09171234567");

        Assert.True(result.IsValid);
        Assert.Null(result.ErrorMessage);
    }

    [Fact]
    public void Validate_ReturnsValid_WhenShortNameNotFlaggedAsPersonalInfo()
    {
        // Scriptural edge case: names of 1-2 chars should not trigger false positives
        var result = PasswordPolicy.Validate(
            "AbCdEfGh123!xyz",
            firstName: "Jo",
            lastName: "Li");

        Assert.True(result.IsValid);
        Assert.Null(result.ErrorMessage);
    }
}