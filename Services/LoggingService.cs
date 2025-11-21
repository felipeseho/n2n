using n2n.Models;
using Spectre.Console;

namespace n2n.Services;

/// <summary>
///     Serviço para logging de erros
/// </summary>
public class LoggingService
{
    private readonly SemaphoreSlim _logSemaphore = new(1, 1);
    private readonly DashboardService _dashboardService;

    public LoggingService(DashboardService dashboardService)
    {
        _dashboardService = dashboardService;
    }

    /// <summary>
    ///     Registra um erro no arquivo de log
    /// </summary>
    public async Task LogError(string logPath, CsvRecord record, int httpCode,
        string errorMessage, string[] headers)
    {
        await _logSemaphore.WaitAsync();
        try
        {
            var logExists = File.Exists(logPath);
            using var writer = new StreamWriter(logPath, true);

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
                    value = $"\"{value.Replace("\"", "\"\"")}\"";
                values.Add(value);
            }

            // Escapar mensagem de erro
            var escapedError = errorMessage.Replace("\"", "\"\"");
            if (escapedError.Contains(',') || escapedError.Contains('"') || escapedError.Contains('\n'))
                escapedError = $"\"{escapedError}\"";

            values.Add(httpCode.ToString());
            values.Add(escapedError);

            await writer.WriteLineAsync(string.Join(",", values));

            // Enviar para o dashboard
            var shortMessage = errorMessage.Length > 60 ? errorMessage.Substring(0, 57) + "..." : errorMessage;
            _dashboardService.AddLogMessage($"Erro linha {record.LineNumber}: {EscapeMarkup(shortMessage)}", "ERROR");
        }
        finally
        {
            _logSemaphore.Release();
        }
    }

    /// <summary>
    ///     Escapa caracteres especiais do markup do Spectre.Console
    /// </summary>
    private static string EscapeMarkup(string text)
    {
        return text.Replace("[", "[[").Replace("]", "]]");
    }

    /// <summary>
    ///     Exibe informação no console
    /// </summary>
    public void LogInfo(string message)
    {
        _dashboardService.AddLogMessage(EscapeMarkup(message), "INFO");
    }

    /// <summary>
    ///     Exibe aviso no console
    /// </summary>
    public void LogWarning(string message)
    {
        if (_dashboardService == null)
        {
            AnsiConsole.MarkupLine($"[yellow]⚠[/] {EscapeMarkup(message)}");
        }
        else
        {
            _dashboardService.AddLogMessage(EscapeMarkup(message), "WARNING");
        }
    }

    /// <summary>
    ///     Exibe sucesso no console
    /// </summary>
    public void LogSuccess(string message)
    {
        if (_dashboardService == null)
        {
            AnsiConsole.MarkupLine($"[green]✓[/] {EscapeMarkup(message)}");
        }
        else
        {
            _dashboardService.AddLogMessage(EscapeMarkup(message), "SUCCESS");
        }
    }
}