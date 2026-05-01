namespace n2n.Models;

/// <summary>
///     Caminhos de arquivos gerados para uma execução específica
/// </summary>
public class ExecutionPaths
{
    public string ExecutionId { get; set; } = string.Empty;
    /// <summary>Log de texto com todos os níveis (INFO, DEBUG, WARNING, ERROR)</summary>
    public string LogPath { get; set; } = string.Empty;
    /// <summary>Log CSV com registros de erro (dados originais + código HTTP + mensagem)</summary>
    public string ErrorLogPath { get; set; } = string.Empty;
    public string CheckpointPath { get; set; } = string.Empty;
    public string CurrentInputFile { get; set; } = string.Empty;
}