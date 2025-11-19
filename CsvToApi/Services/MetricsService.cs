using System.Diagnostics;
using CsvToApi.Models;

namespace CsvToApi.Services;

/// <summary>
/// Serviço para coletar e gerenciar métricas de processamento
/// </summary>
public class MetricsService
{
    private readonly ProcessingMetrics _metrics;
    private readonly List<long> _responseTimes = new();
    private readonly List<long> _batchTimes = new();
    private readonly object _lock = new();

    public MetricsService()
    {
        _metrics = new ProcessingMetrics
        {
            StartTime = DateTime.Now
        };
    }

    public ProcessingMetrics GetMetrics() => _metrics;

    /// <summary>
    /// Inicia o rastreamento do processamento
    /// </summary>
    public void StartProcessing(int totalLines)
    {
        lock (_lock)
        {
            _metrics.StartTime = DateTime.Now;
            _metrics.TotalLines = totalLines;
        }
    }

    /// <summary>
    /// Finaliza o rastreamento do processamento
    /// </summary>
    public void EndProcessing()
    {
        lock (_lock)
        {
            _metrics.EndTime = DateTime.Now;
        }
    }

    /// <summary>
    /// Registra linhas puladas
    /// </summary>
    public void RecordSkippedLines(int count)
    {
        lock (_lock)
        {
            _metrics.SkippedLines += count;
        }
    }

    /// <summary>
    /// Registra uma linha processada com sucesso
    /// </summary>
    public void RecordSuccess()
    {
        lock (_lock)
        {
            _metrics.ProcessedLines++;
            _metrics.SuccessCount++;
        }
    }

    /// <summary>
    /// Registra um erro de processamento
    /// </summary>
    public void RecordError()
    {
        lock (_lock)
        {
            _metrics.ProcessedLines++;
            _metrics.ErrorCount++;
        }
    }

    /// <summary>
    /// Registra um erro de validação
    /// </summary>
    public void RecordValidationError()
    {
        lock (_lock)
        {
            _metrics.ValidationErrors++;
        }
    }

    /// <summary>
    /// Registra uma tentativa de retry
    /// </summary>
    public void RecordRetry()
    {
        lock (_lock)
        {
            _metrics.TotalRetries++;
        }
    }

    /// <summary>
    /// Registra tempo de resposta HTTP
    /// </summary>
    public void RecordResponseTime(long milliseconds)
    {
        lock (_lock)
        {
            _responseTimes.Add(milliseconds);
            
            if (milliseconds < _metrics.MinResponseTimeMs)
                _metrics.MinResponseTimeMs = milliseconds;
            
            if (milliseconds > _metrics.MaxResponseTimeMs)
                _metrics.MaxResponseTimeMs = milliseconds;
            
            _metrics.AverageResponseTimeMs = _responseTimes.Average();
        }
    }

    /// <summary>
    /// Registra código de status HTTP
    /// </summary>
    public void RecordHttpStatusCode(int statusCode)
    {
        lock (_lock)
        {
            if (!_metrics.HttpStatusCodes.ContainsKey(statusCode))
            {
                _metrics.HttpStatusCodes[statusCode] = 0;
            }
            _metrics.HttpStatusCodes[statusCode]++;
        }
    }

    /// <summary>
    /// Registra tempo de processamento de um batch
    /// </summary>
    public void RecordBatchTime(long milliseconds)
    {
        lock (_lock)
        {
            _batchTimes.Add(milliseconds);
            _metrics.BatchesProcessed++;
            _metrics.AverageBatchTimeMs = _batchTimes.Average();
        }
    }

    /// <summary>
    /// Retorna uma medição de tempo para um batch
    /// </summary>
    public Stopwatch StartBatchTimer()
    {
        return Stopwatch.StartNew();
    }

    /// <summary>
    /// Retorna uma medição de tempo para uma requisição
    /// </summary>
    public Stopwatch StartRequestTimer()
    {
        return Stopwatch.StartNew();
    }

    /// <summary>
    /// Renderiza dashboard de métricas no console
    /// </summary>
    public void DisplayDashboard(bool showDetails = true)
    {
        var metrics = GetMetrics();
        var elapsed = metrics.ElapsedTime;

        Console.WriteLine();
        Console.WriteLine("═══════════════════════════════════════════════════════════════");
        Console.WriteLine("                    📊 DASHBOARD DE PERFORMANCE                ");
        Console.WriteLine("═══════════════════════════════════════════════════════════════");
        Console.WriteLine();

        // Progresso
        Console.WriteLine("📈 PROGRESSO");
        Console.WriteLine($"   Total de Linhas:       {metrics.TotalLines:N0}");
        Console.WriteLine($"   Linhas Processadas:    {metrics.ProcessedLines:N0} ({metrics.ProgressPercentage:F1}%)");
        if (metrics.SkippedLines > 0)
            Console.WriteLine($"   Linhas Puladas:        {metrics.SkippedLines:N0}");
        
        // Barra de progresso
        DrawProgressBar(metrics.ProgressPercentage);
        Console.WriteLine();

        // Resultados
        Console.WriteLine("✅ RESULTADOS");
        Console.WriteLine($"   Sucessos:              {metrics.SuccessCount:N0} ({metrics.SuccessRate:F1}%)");
        Console.WriteLine($"   Erros HTTP:            {metrics.ErrorCount:N0} ({metrics.ErrorRate:F1}%)");
        Console.WriteLine($"   Erros de Validação:    {metrics.ValidationErrors:N0}");
        Console.WriteLine();

        // Tempo
        Console.WriteLine("⏱️  TEMPO");
        Console.WriteLine($"   Tempo Decorrido:       {FormatTimeSpan(elapsed)}");
        if (metrics.TotalLines > metrics.ProcessedLines && metrics.ProcessedLines > 0)
        {
            Console.WriteLine($"   Tempo Restante:        {FormatTimeSpan(metrics.EstimatedTimeRemaining)}");
        }
        Console.WriteLine($"   Velocidade:            {metrics.LinesPerSecond:F1} linhas/seg");
        Console.WriteLine();

        if (showDetails)
        {
            // Performance HTTP
            if (metrics.ProcessedLines > 0)
            {
                Console.WriteLine("🌐 PERFORMANCE HTTP");
                Console.WriteLine($"   Tempo Médio:           {metrics.AverageResponseTimeMs:F0} ms");
                if (_responseTimes.Count > 0)
                {
                    Console.WriteLine($"   Tempo Mínimo:          {metrics.MinResponseTimeMs} ms");
                    Console.WriteLine($"   Tempo Máximo:          {metrics.MaxResponseTimeMs} ms");
                }
                if (metrics.TotalRetries > 0)
                    Console.WriteLine($"   Total de Retries:      {metrics.TotalRetries}");
                Console.WriteLine();
            }

            // Batches
            if (metrics.BatchesProcessed > 0)
            {
                Console.WriteLine("📦 PROCESSAMENTO EM LOTE");
                Console.WriteLine($"   Batches Processados:   {metrics.BatchesProcessed}");
                Console.WriteLine($"   Tempo Médio/Batch:     {metrics.AverageBatchTimeMs:F0} ms");
                Console.WriteLine();
            }

            // Status HTTP
            if (metrics.HttpStatusCodes.Count > 0)
            {
                Console.WriteLine("📊 CÓDIGOS HTTP");
                foreach (var kvp in metrics.HttpStatusCodes.OrderBy(x => x.Key))
                {
                    var emoji = GetStatusEmoji(kvp.Key);
                    var percentage = (kvp.Value * 100.0 / metrics.ProcessedLines);
                    Console.WriteLine($"   {emoji} {kvp.Key}: {kvp.Value:N0} ({percentage:F1}%)");
                }
                Console.WriteLine();
            }
        }

        Console.WriteLine("═══════════════════════════════════════════════════════════════");
        Console.WriteLine();
    }

    /// <summary>
    /// Exibe resumo compacto durante o processamento
    /// </summary>
    public void DisplayProgressUpdate()
    {
        var metrics = GetMetrics();
        Console.Write($"\r⏳ Processadas: {metrics.ProcessedLines:N0}/{metrics.TotalLines:N0} " +
                      $"| Sucessos: {metrics.SuccessCount:N0} " +
                      $"| Erros: {metrics.ErrorCount:N0} " +
                      $"| {metrics.LinesPerSecond:F1} linhas/seg " +
                      $"| {metrics.ProgressPercentage:F1}%");
    }

    /// <summary>
    /// Desenha barra de progresso no console
    /// </summary>
    private void DrawProgressBar(double percentage)
    {
        const int barWidth = 50;
        var filled = (int)(barWidth * percentage / 100);
        var empty = barWidth - filled;

        Console.Write("   [");
        Console.Write(new string('█', filled));
        Console.Write(new string('░', empty));
        Console.Write($"] {percentage:F1}%");
        Console.WriteLine();
    }

    /// <summary>
    /// Formata TimeSpan para exibição amigável
    /// </summary>
    private string FormatTimeSpan(TimeSpan ts)
    {
        if (ts.TotalSeconds < 60)
            return $"{ts.TotalSeconds:F0}s";
        if (ts.TotalMinutes < 60)
            return $"{ts.Minutes}min {ts.Seconds}s";
        return $"{ts.Hours}h {ts.Minutes}min {ts.Seconds}s";
    }

    /// <summary>
    /// Retorna emoji apropriado para código HTTP
    /// </summary>
    private string GetStatusEmoji(int statusCode)
    {
        return statusCode switch
        {
            >= 200 and < 300 => "✅",
            >= 300 and < 400 => "↩️",
            >= 400 and < 500 => "⚠️",
            >= 500 => "❌",
            _ => "❓"
        };
    }
}
