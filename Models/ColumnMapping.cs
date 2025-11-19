namespace CsvToApi.Models;

/// <summary>
/// Mapeamento e validação de coluna CSV
/// </summary>
public class ColumnMapping
{
    public string Column { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string? Regex { get; set; }
    public string? Format { get; set; }
}

