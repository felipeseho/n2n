namespace CsvToApi.Models;

/// <summary>
/// Opções de linha de comando para sobrescrever configurações do YAML
/// </summary>
public class CommandLineOptions
{
    // Configuração geral
    public string? ConfigPath { get; set; }
    
    // Configurações de arquivo
    public string? InputPath { get; set; }
    public int? BatchLines { get; set; }
    public string? LogPath { get; set; }
    public string? CsvDelimiter { get; set; }
    public int? StartLine { get; set; }
    public int? MaxLines { get; set; }
    
    // Configurações de API
    public string? EndpointUrl { get; set; }
    public string? AuthToken { get; set; }
    public string? Method { get; set; }
    public int? RequestTimeout { get; set; }
    
    // Opções de processamento
    public bool Verbose { get; set; }
    public int? MaxParallelism { get; set; }
    public bool DryRun { get; set; }
    public bool ResetCheckpoint { get; set; }
}
