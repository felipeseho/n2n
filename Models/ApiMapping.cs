namespace CsvToApi.Models;

/// <summary>
/// Mapeamento entre coluna CSV e atributo da API
/// </summary>
public class ApiMapping
{
    public string Attribute { get; set; } = string.Empty;
    public string? CsvColumn { get; set; }
    public string? Transform { get; set; }
    public string? FixedValue { get; set; }
}

