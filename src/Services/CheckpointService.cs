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
        DateTime executionStartTime)
    {
        var checkpoint = new Checkpoint
        {
            // Progresso
            LastProcessedLine = lineNumber,
            LastUpdate = DateTime.Now,
            TotalProcessed = totalProcessed,
            SuccessCount = successCount,
            ErrorCount = errorCount,
            
            // Informações da execução
            ExecutionId = context.ExecutionId,
            ExecutionStartTime = executionStartTime,
            
            // Configurações
            Configuration = new CheckpointConfiguration
            {
                InputPath = context.Configuration.File.InputPath,
                ConfigPath = context.CommandLineOptions.ConfigPath ?? "config.yaml",
                BatchLines = context.Configuration.File.BatchLines,
                Delimiter = context.Configuration.File.CsvDelimiter,
                StartLine = context.Configuration.File.StartLine,
                MaxLines = context.Configuration.File.MaxLines,
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

        var json = JsonSerializer.Serialize(checkpoint, new JsonSerializerOptions
        {
            WriteIndented = true
        });

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