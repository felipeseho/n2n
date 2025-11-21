using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace n2n.Utils;

/// <summary>
///     Utilitário para transformação de dados antes do envio para a API
/// </summary>
public static class DataTransformer
{
    /// <summary>
    ///     Aplica transformação ao valor conforme o tipo especificado
    /// </summary>
    /// <param name="value">Valor original</param>
    /// <param name="transform">Tipo de transformação a aplicar</param>
    /// <returns>Valor transformado</returns>
    public static string ApplyTransformation(string value, string? transform)
    {
        if (string.IsNullOrWhiteSpace(transform)) return value;

        return transform.ToLower() switch
        {
            "uppercase" => value.ToUpper(),
            "lowercase" => value.ToLower(),
            "trim" => value.Trim(),
            "remove-spaces" => value.Replace(" ", ""),
            "remove-all-spaces" => Regex.Replace(value, @"\s+", ""),
            "capitalize" => Capitalize(value),
            "title-case" => ToTitleCase(value),
            "remove-accents" => RemoveAccents(value),
            "format-cpf" => FormatCpf(value),
            "format-cnpj" => FormatCnpj(value),
            "format-phone-br" => FormatPhoneBr(value),
            "format-cep" => FormatCep(value),
            "remove-non-numeric" => Regex.Replace(value, @"[^\d]", ""),
            "remove-non-alphanumeric" => Regex.Replace(value, @"[^a-zA-Z0-9]", ""),
            "slugify" => Slugify(value),
            "reverse" => new string(value.Reverse().ToArray()),
            "base64-encode" => Convert.ToBase64String(Encoding.UTF8.GetBytes(value)),
            "url-encode" => Uri.EscapeDataString(value),
            _ when transform.StartsWith("date-format:") =>
                DateFormat(value, transform.Substring("date-format:".Length)),
            _ => value
        };
    }

    /// <summary>
    ///     Capitaliza a primeira letra da string
    /// </summary>
    private static string Capitalize(string value)
    {
        if (string.IsNullOrEmpty(value)) return value;
        return char.ToUpper(value[0]) + value[1..].ToLower();
    }

    /// <summary>
    ///     Converte para Title Case (Primeira Letra De Cada Palavra Maiúscula)
    /// </summary>
    private static string ToTitleCase(string value)
    {
        if (string.IsNullOrEmpty(value)) return value;
        var textInfo = new CultureInfo("pt-BR", false).TextInfo;
        return textInfo.ToTitleCase(value.ToLower());
    }

    private static string DateFormat(string value, string format)
    {
        if (DateTime.TryParse(value, out var date)) return date.ToString(format);
        return value;
    }

    /// <summary>
    ///     Remove acentos e caracteres especiais
    /// </summary>
    private static string RemoveAccents(string value)
    {
        if (string.IsNullOrEmpty(value)) return value;

        var normalizedString = value.Normalize(NormalizationForm.FormD);
        var stringBuilder = new StringBuilder();

        foreach (var c in normalizedString)
        {
            var unicodeCategory = CharUnicodeInfo.GetUnicodeCategory(c);
            if (unicodeCategory != UnicodeCategory.NonSpacingMark) stringBuilder.Append(c);
        }

        return stringBuilder.ToString().Normalize(NormalizationForm.FormC);
    }

    /// <summary>
    ///     Formata CPF (000.000.000-00)
    /// </summary>
    private static string FormatCpf(string value)
    {
        var numbers = Regex.Replace(value, @"[^\d]", "");
        if (numbers.Length != 11) return value;

        return $"{numbers[..3]}.{numbers[3..6]}.{numbers[6..9]}-{numbers[9..]}";
    }

    /// <summary>
    ///     Formata CNPJ (00.000.000/0000-00)
    /// </summary>
    private static string FormatCnpj(string value)
    {
        var numbers = Regex.Replace(value, @"[^\d]", "");
        if (numbers.Length != 14) return value;

        return $"{numbers[..2]}.{numbers[2..5]}.{numbers[5..8]}/{numbers[8..12]}-{numbers[12..]}";
    }

    /// <summary>
    ///     Formata telefone brasileiro (11) 98765-4321 ou (11) 3456-7890
    /// </summary>
    private static string FormatPhoneBr(string value)
    {
        var numbers = Regex.Replace(value, @"[^\d]", "");

        if (numbers.Length == 11) // Celular
            return $"({numbers[..2]}) {numbers[2..7]}-{numbers[7..]}";

        if (numbers.Length == 10) // Fixo
            return $"({numbers[..2]}) {numbers[2..6]}-{numbers[6..]}";

        return value;
    }

    /// <summary>
    ///     Formata CEP (00000-000)
    /// </summary>
    private static string FormatCep(string value)
    {
        var numbers = Regex.Replace(value, @"[^\d]", "");
        if (numbers.Length != 8) return value;

        return $"{numbers[..5]}-{numbers[5..]}";
    }

    /// <summary>
    ///     Converte string para slug (minúsculas, sem espaços, sem caracteres especiais)
    /// </summary>
    private static string Slugify(string value)
    {
        if (string.IsNullOrEmpty(value)) return value;

        // Remove acentos
        value = RemoveAccents(value);

        // Converte para lowercase
        value = value.ToLower();

        // Remove caracteres especiais e substitui espaços por hífen
        value = Regex.Replace(value, @"[^a-z0-9\s-]", "");
        value = Regex.Replace(value, @"\s+", "-");
        value = Regex.Replace(value, @"-+", "-");

        return value.Trim('-');
    }
}