using CsvToApi.Models;

namespace CsvToApi.Services;

/// <summary>
/// Serviço para logging de erros
/// </summary>
public class LoggingService
{
    private readonly SemaphoreSlim _logSemaphore = new(1, 1);

    /// <summary>
    /// Registra um erro no arquivo de log
    /// </summary>
    public async Task LogError(string logPath, CsvRecord record, int httpCode, 
        string errorMessage, string[] headers)
    {
        await _logSemaphore.WaitAsync();
        try
        {
            var logExists = File.Exists(logPath);
            using var writer = new StreamWriter(logPath, append: true);

            // Escrever cabeçalho se arquivo não existir
            if (!logExists)
            {
                var headerLine = "LineNumber," + string.Join(",", headers) + ",HttpCode,ErrorMessage";
                await writer.WriteLineAsync(headerLine);
            }

            // Escrever linha de erro
            var values = new List<string> { record.LineNumber.ToString() };
            foreach (var header in headers)
            {
                var value = record.Data.GetValueOrDefault(header, string.Empty);
                // Escapar valores com vírgula ou aspas
                if (value.Contains(',') || value.Contains('"') || value.Contains('\n'))
                {
                    value = $"\"{value.Replace("\"", "\"\"")}\"";
                }
                values.Add(value);
            }
            
            // Escapar mensagem de erro
            var escapedError = errorMessage.Replace("\"", "\"\"");
            if (escapedError.Contains(',') || escapedError.Contains('"') || escapedError.Contains('\n'))
            {
                escapedError = $"\"{escapedError}\"";
            }
            
            values.Add(httpCode.ToString());
            values.Add(escapedError);

            await writer.WriteLineAsync(string.Join(",", values));
        }
        finally
        {
            _logSemaphore.Release();
        }
    }
}

