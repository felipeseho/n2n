namespace n2n.Models;

/// <summary>
///     Configuração de arquivo CSV
/// </summary>
public class FileConfiguration
{
    public string InputPath { get; set; } = string.Empty;
    public int BatchLines { get; set; } = 100;
    public string LogDirectory { get; set; } = "logs";
    public string CsvDelimiter { get; set; } = ",";
    public int StartLine { get; set; } = 1;
    public int? MaxLines { get; set; }
    public string CheckpointDirectory { get; set; } = "checkpoints";
    public List<ColumnMapping> Columns { get; set; } = new();
}