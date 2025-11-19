namespace CsvToApi.Models;

/// <summary>
/// Registro de uma linha do CSV
/// </summary>
public class CsvRecord
{
    public int LineNumber { get; set; }
    public Dictionary<string, string> Data { get; set; } = new();
}

