using System.Globalization;
using System.Text.RegularExpressions;
using n2n.Models;

namespace n2n.Services;

/// <summary>
///     Serviço para validação de dados CSV
/// </summary>
public class ValidationService
{
    /// <summary>
    ///     Valida um registro CSV de acordo com o mapeamento configurado
    /// </summary>
    public string? ValidateRecord(CsvRecord record, List<ColumnMapping> mappings)
    {
        foreach (var mapping in mappings)
        {
            if (!record.Data.TryGetValue(mapping.Column, out var value))
                return $"Coluna '{mapping.Column}' não encontrada";

            if (string.IsNullOrWhiteSpace(value)) continue; // Campo vazio é permitido

            // Validar regex
            if (!string.IsNullOrWhiteSpace(mapping.Regex))
                if (!Regex.IsMatch(value, mapping.Regex))
                    return $"Valor '{value}' inválido para coluna '{mapping.Column}'";

            // Validar data
            if (mapping.Type == "date" && !string.IsNullOrWhiteSpace(mapping.Format))
            {
                var format = mapping.Format
                    .Replace("YYYY", "yyyy")
                    .Replace("MM", "MM")
                    .Replace("DD", "dd");

                if (!DateTime.TryParseExact(value, format, CultureInfo.InvariantCulture,
                        DateTimeStyles.None, out _))
                    return $"Data '{value}' inválida para formato '{mapping.Format}' na coluna '{mapping.Column}'";
            }
        }

        return null;
    }
}