using System.Globalization;
using System.Net.Mail;
using System.Text.RegularExpressions;

namespace GlowUpRD.API.Validation;

public static partial class InputRules
{
    public const string NameMessage = "El nombre solo puede contener letras, espacios, apóstrofes y guiones. Elimina los números o símbolos especiales.";
    public const string PhoneMessage = "El teléfono no es válido. Escribe entre 8 y 15 dígitos, por ejemplo +18095551234 o 8095551234.";
    public const string EmailMessage = "El correo no tiene un formato válido. Utiliza una dirección como nombre@dominio.com.";
    public const string PasswordMessage = "La contraseña debe tener al menos 8 caracteres e incluir una mayúscula, una minúscula, un número y un símbolo.";

    public static bool IsPersonName(string? value)
    {
        var name = InputNormalizer.RequiredText(value);
        return name.Length >= 2 && name.All(character => char.IsLetter(character) || character is ' ' or '\'' or '-') &&
            name[0] is not '\'' and not '-' && name[^1] is not '\'' and not '-' && !name.Contains("--") && !name.Contains("''");
    }

    public static bool IsCommercialText(string? value)
    {
        var text = InputNormalizer.RequiredText(value);
        return text.Length > 0 && text.Any(char.IsLetterOrDigit) && text.All(character => !char.IsControl(character) && character != '<' && character != '>');
    }

    public static bool IsEmail(string? value)
    {
        var email = InputNormalizer.NormalizeEmail(value);
        if (email.Length == 0 || email.Contains(' ') || email.Count(character => character == '@') != 1) return false;
        try
        {
            var address = new MailAddress(email);
            var at = email.LastIndexOf('@');
            return address.Address == email && at > 0 && email[(at + 1)..].Contains('.') && !email.EndsWith(".", StringComparison.Ordinal);
        }
        catch { return false; }
    }

    public static bool IsPhone(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return true;
        var raw = value.Trim();
        if (raw.Any(character => !char.IsDigit(character) && character is not '+' and not '-' and not '(' and not ')' and not ' ') ||
            raw.Count(character => character == '+') > 1 || (raw.Contains('+') && !raw.StartsWith('+'))) return false;
        var normalized = InputNormalizer.NormalizePhone(raw);
        if (normalized is null || !PhoneRegex().IsMatch(normalized)) return false;
        var digits = normalized[1..];
        return digits.Distinct().Count() > 1;
    }

    public static bool IsPasswordStrong(string? password, params string?[] disallowedValues)
    {
        if (password is null || password.Length is < 8 or > 100 || password != password.Trim()) return false;
        if (!password.Any(char.IsUpper) || !password.Any(char.IsLower) || !password.Any(char.IsDigit) || !password.Any(character => !char.IsLetterOrDigit(character))) return false;
        return disallowedValues.Where(value => !string.IsNullOrWhiteSpace(value))
            .All(value => !string.Equals(password, value!.Trim(), StringComparison.OrdinalIgnoreCase));
    }

    public static bool IsValidBirthDate(DateOnly? value, DateOnly today) =>
        !value.HasValue || (value.Value <= today && value.Value >= today.AddYears(-120));

    public static bool HasCurrencyScale(decimal value) => decimal.Round(value, 2) == value;
    public static bool IsValidOptionalUrl(string? value) => string.IsNullOrWhiteSpace(value) ||
        (Uri.TryCreate(value.Trim(), UriKind.Absolute, out var uri) && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps));

    [GeneratedRegex("^\\+[0-9]{8,15}$")]
    private static partial Regex PhoneRegex();
}
