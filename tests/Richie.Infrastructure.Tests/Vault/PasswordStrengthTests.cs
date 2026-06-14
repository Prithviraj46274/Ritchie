using Richie.Application.Vault;

namespace Richie.Infrastructure.Tests.Vault;

public sealed class PasswordStrengthTests
{
    [Theory]
    [InlineData("", 0)]            // empty
    [InlineData("abc", 0)]         // short, one class — 0 points
    [InlineData("abcdefgh", 0)]    // length 8 only — 1 point → score 0
    [InlineData("Abcdefgh", 1)]    // length 8 + mixed case — 2 points → score 1
    [InlineData("Abcdefg1", 2)]    // length 8 + mixed + digit — 3 points → score 2
    [InlineData("Abcdef1!", 3)]    // length 8 + mixed + digit + symbol — 4 points → score 3
    [InlineData("Abcdefghijk1!", 4)] // length 12 + mixed + digit + symbol — 5 points → score 4
    public void Evaluate_ScoresAsDocumented(string password, int expectedScore)
    {
        Assert.Equal(expectedScore, PasswordStrength.Evaluate(password).Score);
    }

    [Fact]
    public void IsWeak_FlagsVeryWeakAndWeak()
    {
        Assert.True(PasswordStrength.Evaluate("abc").IsWeak);
        Assert.True(PasswordStrength.Evaluate("abcdefgh").IsWeak);
        Assert.False(PasswordStrength.Evaluate("Abcdef1!").IsWeak);
    }

    [Fact]
    public void Evaluate_ProducesLabelAndBoundedPercent()
    {
        PasswordStrengthResult result = PasswordStrength.Evaluate("Abcdefghijk1!");
        Assert.Equal("Very strong", result.Label);
        Assert.InRange(result.Percent, 0, 100);
    }
}
