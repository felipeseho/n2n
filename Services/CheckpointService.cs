using System.Text.Json;
using n2n.Models;

namespace n2n.Services;

/// <summary>
///     Serviço para gerenciamento de checkpoint (retomar processamento)
/// </summary>
public class CheckpointService
{
    /// <summary>
    ///     Salva checkpoint no arquivo
    /// </summary>
    public async Task SaveCheckpointAsync(string checkpointPath, int lineNumber, int totalProcessed,
        int successCount, int errorCount)
    {
        var checkpoint = new Checkpoint
        {
            LastProcessedLine = lineNumber,
            LastUpdate = DateTime.Now,
            TotalProcessed = totalProcessed,
            SuccessCount = successCount,
            ErrorCount = errorCount
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