using System.Text.Json;
using n2n.Models;

namespace n2n.Services;

/// <summary>
///     Serviço para gerenciamento de checkpoint (retomar processamento)
/// </summary>
public class CheckpointService
{
    /// <summary>
    ///     Salva checkpoint no arquivo com informações completas da execução
    /// </summary>
    public async Task SaveCheckpointAsync(
        string checkpointPath, 
        int lineNumber, 
        int totalProcessed,
        int successCount, 
        int errorCount,
        AppExecutionContext context,
        DateTime executionStartTime,
        string? errorMessage = null,
        bool isCompleted = false)
    {
        var checkpoint = new Checkpoint
        {
            // Progresso
            LastProcessedLine = lineNumber,
            LastUpdate = DateTime.Now,
            TotalProcessed = totalProcessed,
            SuccessCount = successCount,
            ErrorCount = errorCount,
            IsCompleted = isCompleted,
            
            // Informações da execução
            ExecutionId = context.ExecutionId,
            ExecutionStartTime = executionStartTime,
            
            // Configurações
            Configuration = new CheckpointConfiguration
            {
                InputPath = context.ExecutionPaths.CurrentInputFile ?? context.Configuration.File.InputPath,
                ConfigPath = context.CommandLineOptions.ConfigPath ?? "config.yaml",
                BatchLines = context.Configuration.File.BatchLines,
                Delimiter = context.Configuration.File.CsvDelimiter,
                StartLine = context.Configuration.File.StartLine,
                MaxLines = context.Configuration.File.MaxLines,
                LogPath = context.Configuration.File.Log?.Path,
                LogLevel = context.Configuration.File.Log?.Level,
                DryRun = context.IsDryRun,
                Verbose = context.IsVerbose,
                EndpointNameOverride = context.CommandLineOptions.EndpointName
            },
            
            // Endpoint ativo
            EndpointInfo = new CheckpointEndpointInfo
            {
                EndpointName = context.ActiveEndpoint.Name,
                BaseUrl = context.ActiveEndpoint.EndpointUrl,
                Method = context.ActiveEndpoint.Method,
                Timeout = context.ActiveEndpoint.RequestTimeout,
                TotalMappings = context.ActiveEndpoint.Mapping?.Count ?? 0
            }
        };

        // Se há mensagem de erro (arquivo não encontrado, etc), adicionar ao JSON
        var jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true
        };

        var json = errorMessage != null
            ? JsonSerializer.Serialize(new { checkpoint, errorMessage }, jsonOptions)
            : JsonSerializer.Serialize(checkpoint, jsonOptions);

        // Criar diretório se não existir
        var directory = Path.GetDirectoryName(checkpointPath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory)) Directory.CreateDirectory(directory);

        await File.WriteAllTextAsync(checkpointPath, json);
    }

    /// <summary>
    ///     Carrega checkpoint do arquivo
    /// </summary>
    public Checkpoint? LoadCheckpoint(string checkpointPath)
    {
        if (!File.Exists(checkpointPath)) return null;

        try
        {
            var json = File.ReadAllText(checkpointPath);
            return JsonSerializer.Deserialize<Checkpoint>(json);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    ///     Limpa arquivo de checkpoint
    /// </summary>
    public void ClearCheckpoint(string checkpointPath)
    {
        if (File.Exists(checkpointPath)) File.Delete(checkpointPath);
    }
}