using n2n.Models;

namespace n2n.Services;

public enum AppLogLevel { DEBUG = 0, INFO = 1, WARNING = 2, ERROR = 3 }

/// <summary>
///     Serviço para logging multi-nível: texto (todos os níveis) e CSV de erros
/// </summary>
public class LoggingService
{
    private readonly SemaphoreSlim _csvSemaphore = new(1, 1);
    private readonly SemaphoreSlim _textSemaphore = new(1, 1);
    private readonly DashboardService _dashboardService;
    private readonly AppExecutionContext _context;

    public LoggingService(DashboardService dashboardService, AppExecutionContext context)
    {
        _dashboardService = dashboardService;
        _context = context;
    }

    private AppLogLevel ConfiguredLevel
    {
        get
        {
            var levelStr = _context.Configuration?.File?.Log?.Level?.ToUpper() ?? "INFO";
            return Enum.TryParse<AppLogLevel>(levelStr, out var level) ? level : AppLogLevel.INFO;
        }
    }

    private bool ShouldLog(AppLogLevel level) => level >= ConfiguredLevel;

    private async Task WriteToTextLogAsync(AppLogLevel level, string message)
    {
        var logPath = _context.ExecutionPaths?.LogPath;
        if (string.IsNullOrEmpty(logPath)) return;

        await _textSemaphore.WaitAsync();
        try
        {
            var dir = Path.GetDirectoryName(logPath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
            var line = $"{timestamp} [{level,-7}] {message}{Environment.NewLine}";
            await File.AppendAllTextAsync(logPath, line);
        }
        finally
        {
            _textSemaphore.Release();
        }
    }

    /// <summary>
    ///     Registra erro: escreve no log de texto e no CSV de erros
    /// </summary>
    public async Task LogError(CsvRecord record, int httpCode, string errorMessage, string[] headers)
    {
        // CSV de erros (dados da linha + código HTTP + mensagem)
        await _csvSemaphore.WaitAsync();
        try
        {
            var errorLogPath = _context.ExecutionPaths?.ErrorLogPath;
            if (!string.IsNullOrEmpty(errorLogPath))
            {
                var dir = Path.GetDirectoryName(errorLogPath);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                var logExists = File.Exists(errorLogPath);
                using var writer = new StreamWriter(errorLogPath, true);

                if (!logExists)
                {
                    var headerLine = "LineNumber," + string.Join(",", headers) + ",HttpCode,ErrorMessage";
                    await writer.WriteLineAsync(headerLine);
                }

                var values = new List<string> { record.LineNumber.ToString() };
                foreach (var header in headers)
                {
                    var value = record.Data.GetValueOrDefault(header, string.Empty);
                    if (value.Contains(',') || value.Contains('"') || value.Contains('\n'))
                        value = $"\"{value.Replace("\"", "\"\"")}\"";
                    values.Add(value);
                }

                var escapedError = errorMessage.Replace("\"", "\"\"");
                if (escapedError.Contains(',') || escapedError.Contains('"') || escapedError.Contains('\n'))
                    escapedError = $"\"{escapedError}\"";

                values.Add(httpCode.ToString());
                values.Add(escapedError);
                await writer.WriteLineAsync(string.Join(",", values));
            }
        }
        finally
        {
            _csvSemaphore.Release();
        }

        // Log de texto
        await WriteToTextLogAsync(AppLogLevel.ERROR, $"Line {record.LineNumber}: HTTP {httpCode} - {errorMessage}");

        // Dashboard
        var shortMessage = errorMessage.Length > 60 ? errorMessage[..57] + "..." : errorMessage;
        _dashboardService.AddLogMessage($"Erro linha {record.LineNumber}: {EscapeMarkup(shortMessage)}", "ERROR");
    }

    /// <summary>
    ///     Log de debug assíncrono — usado para dados de requisição HTTP
    /// </summary>
    public async Task LogDebugAsync(string message)
    {
        if (!ShouldLog(AppLogLevel.DEBUG)) return;
        await WriteToTextLogAsync(AppLogLevel.DEBUG, message);
    }

    /// <summary>
    ///     Log de debug fire-and-forget
    /// </summary>
    public void LogDebug(string message)
    {
        if (!ShouldLog(AppLogLevel.DEBUG)) return;
        _ = WriteToTextLogAsync(AppLogLevel.DEBUG, message);
    }

    public void LogInfo(string message)
    {
        if (ShouldLog(AppLogLevel.INFO))
            _ = WriteToTextLogAsync(AppLogLevel.INFO, message);
        _dashboardService.AddLogMessage(EscapeMarkup(message), "INFO");
    }

    public void LogWarning(string message)
    {
        if (ShouldLog(AppLogLevel.WARNING))
            _ = WriteToTextLogAsync(AppLogLevel.WARNING, message);
        _dashboardService.AddLogMessage(EscapeMarkup(message), "WARNING");
    }

    public void LogSuccess(string message)
    {
        if (ShouldLog(AppLogLevel.INFO))
            _ = WriteToTextLogAsync(AppLogLevel.INFO, $"[SUCCESS] {message}");
        _dashboardService.AddLogMessage(EscapeMarkup(message), "SUCCESS");
    }

    private static string EscapeMarkup(string text) =>
        text.Replace("[", "[[").Replace("]", "]]");
}
