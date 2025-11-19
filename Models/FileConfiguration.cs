namespace CsvToApi.Models;

/// <summary>
/// Configuração de arquivo CSV
/// </summary>
public class FileConfiguration
{
    public string InputPath { get; set; } = string.Empty;
    public int BatchLines { get; set; } = 100;
    public string LogPath { get; set; } = string.Empty;
    public string CsvDelimiter { get; set; } = ",";
    public int StartLine { get; set; } = 1;
    public int? MaxLines { get; set; }
    public string? CheckpointPath { get; set; }
    public List<ColumnMapping> Mapping { get; set; } = new();
}

