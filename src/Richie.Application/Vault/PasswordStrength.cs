namespace Richie.Application.Vault;

/// <summary>Result of a password-strength evaluation, with a visible 0–4 scale (CLAUDE.md:
/// every score ships with its logic). <see cref="IsWeak"/> drives weak-password health flags.</summary>
public sealed record PasswordStrengthResult(int Score, string Label, int Percent)
{
    /// <summary>Below the acceptable bar (Very weak / Weak).</summary>
    public bool IsWeak => Score <= 1;
}

/// <summary>
/// Transparent password-strength scoring. Points (max 5) are awarded for length and character
/// variety, then mapped to a 0–4 score:
/// <list type="bullet">
/// <item>+1 length ≥ 8, +1 length ≥ 12</item>
/// <item>+1 has both lower- and upper-case letters</item>
/// <item>+1 has a digit, +1 has a symbol</item>
/// </list>
/// 0 Very weak · 1 Weak · 2 Fair · 3 Strong · 4 Very strong.
/// </summary>
public static class PasswordStrength
{
    public static PasswordStrengthResult Evaluate(string? password)
    {
        password ??= string.Empty;
        if (password.Length == 0)
            return new PasswordStrengthResult(0, "Very weak", 0);

        int points = 0;
        if (password.Length >= 8) points++;
        if (password.Length >= 12) points++;

        bool lower = password.Any(char.IsLower);
        bool upper = password.Any(char.IsUpper);
        bool digit = password.Any(char.IsDigit);
        bool symbol = password.Any(c => !char.IsLetterOrDigit(c));

        if (lower && upper) points++;
        if (digit) points++;
        if (symbol) points++;

        int score = points <= 1 ? 0 : points == 2 ? 1 : points == 3 ? 2 : points == 4 ? 3 : 4;
        string label = score switch
        {
            0 => "Very weak",
            1 => "Weak",
            2 => "Fair",
            3 => "Strong",
            _ => "Very strong"
        };
        // Keep a sliver visible even for the weakest non-empty password.
        int percent = score == 0 ? 8 : score * 25;
        return new PasswordStrengthResult(score, label, percent);
    }
}
