namespace n2n.Models;

/// <summary>
///     Configuração de arquivo CSV
/// </summary>
public class FileConfiguration
{
    public string InputPath { get; set; } = string.Empty;
    public List<string> InputPaths { get; set; } = new();
    public int BatchLines { get; set; } = 100;
    public string LogDirectory { get; set; } = "logs";
    public string CsvDelimiter { get; set; } = ",";
    public int StartLine { get; set; } = 1;
    public int? MaxLines { get; set; }
    public string CheckpointDirectory { get; set; } = "checkpoints";
    public List<ColumnMapping> Columns { get; set; } = new();

    /// <summary>
    ///     Retorna a lista de arquivos a serem processados.
    ///     Prioriza InputPaths se configurado, senão usa InputPath.
    /// </summary>
    public List<string> GetInputFiles()
    {
        if (InputPaths != null && InputPaths.Count > 0)
            return InputPaths;

        if (!string.IsNullOrWhiteSpace(InputPath))
            return new List<string> { InputPath };

        return new List<string>();
    }
}