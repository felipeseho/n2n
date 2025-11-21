namespace n2n.Models;

/// <summary>
///     Contexto centralizado de execução que encapsula todas as configurações e estado da execução atual
/// </summary>
public class AppExecutionContext
{
    /// <summary>
    ///     Configuração completa (arquivo YAML + opções de linha de comando mescladas)
    /// </summary>
    public Configuration Configuration { get; set; } = null!;

    /// <summary>
    ///     Opções de linha de comando
    /// </summary>
    public CommandLineOptions CommandLineOptions { get; set; } = null!;

    /// <summary>
    ///     Caminhos de execução (logs, checkpoints)
    /// </summary>
    public ExecutionPaths ExecutionPaths { get; set; } = null!;

    /// <summary>
    ///     Endpoint ativo (selecionado para esta execução)
    /// </summary>
    public NamedEndpoint ActiveEndpoint { get; set; } = null!;

    /// <summary>
    ///     Indica se é modo Dry Run (sem enviar requisições)
    /// </summary>
    public bool IsDryRun => CommandLineOptions?.DryRun ?? false;

    /// <summary>
    ///     Indica se é modo Verbose (log detalhado)
    /// </summary>
    public bool IsVerbose => CommandLineOptions?.Verbose ?? false;

    /// <summary>
    ///     ID de execução único
    /// </summary>
    public string ExecutionId => ExecutionPaths?.ExecutionId ?? string.Empty;
}

