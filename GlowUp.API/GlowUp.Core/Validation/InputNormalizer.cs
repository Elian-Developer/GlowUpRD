using System.Text.RegularExpressions;

namespace GlowUpRD.API.Validation;

public static partial class InputNormalizer
{
    public static string RequiredText(string? value) => Collapse(value ?? string.Empty);
    public static string? OptionalText(string? value)
    {
        var normalized = Collapse(value ?? string.Empty);
        return normalized.Length == 0 ? null : normalized;
    }

    public static string NormalizeEmail(string? value) => RequiredText(value).ToLowerInvariant();

    public static string? NormalizePhone(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var trimmed = value.Trim();
        if (trimmed.Contains('+') && !trimmed.StartsWith('+')) return trimmed;
        var digits = new string(trimmed.Where(char.IsDigit).ToArray());
        if (digits.Length == 10 && (digits.StartsWith("809") || digits.StartsWith("829") || digits.StartsWith("849")))
            digits = "1" + digits;
        return digits.Length == 0 ? null : "+" + digits;
    }

    [GeneratedRegex("\\s+")]
    private static partial Regex MultipleWhitespace();

    private static string Collapse(string value) => MultipleWhitespace().Replace(value.Trim(), " ");
}
