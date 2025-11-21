namespace n2n.Models;

/// <summary>
///     Caminhos de arquivos gerados para uma execução específica
/// </summary>
public class ExecutionPaths
{
    public string ExecutionId { get; set; } = string.Empty;
    public string LogPath { get; set; } = string.Empty;
    public string CheckpointPath { get; set; } = string.Empty;
}