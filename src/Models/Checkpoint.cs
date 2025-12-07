namespace n2n.Models;

/// <summary>
///     Modelo de checkpoint para retomar processamento
/// </summary>
public class Checkpoint
{
    // Informações de progresso
    public int LastProcessedLine { get; set; }
    public DateTime LastUpdate { get; set; }
    public int TotalProcessed { get; set; }
    public int SuccessCount { get; set; }
    public int ErrorCount { get; set; }
    
    // Informações da execução
    public string ExecutionId { get; set; } = string.Empty;
    public DateTime ExecutionStartTime { get; set; }
    
    // Configurações da execução
    public CheckpointConfiguration Configuration { get; set; } = new();
    
    // Informações do endpoint ativo
    public CheckpointEndpointInfo EndpointInfo { get; set; } = new();
}

/// <summary>
///     Configurações relevantes salvas no checkpoint
/// </summary>
public class CheckpointConfiguration
{
    // Arquivo
    public string InputPath { get; set; } = string.Empty;
    public string ConfigPath { get; set; } = string.Empty;
    public int BatchLines { get; set; }
    public string Delimiter { get; set; } = string.Empty;
    public int? StartLine { get; set; }
    public int? MaxLines { get; set; }
    
    // Opções de execução
    public bool DryRun { get; set; }
    public bool Verbose { get; set; }
    public string? EndpointNameOverride { get; set; }
}

/// <summary>
///     Informações do endpoint ativo no checkpoint
/// </summary>
public class CheckpointEndpointInfo
{
    public string EndpointName { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = string.Empty;
    public string Method { get; set; } = string.Empty;
    public int? Timeout { get; set; }
    public int TotalMappings { get; set; }
}