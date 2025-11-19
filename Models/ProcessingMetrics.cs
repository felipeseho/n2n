namespace CsvToApi.Models;

/// <summary>
/// Modelo para armazenar métricas de processamento
/// </summary>
public class ProcessingMetrics
{
    public DateTime StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public int TotalLines { get; set; }
    public int ProcessedLines { get; set; }
    public int SuccessCount { get; set; }
    public int ErrorCount { get; set; }
    public int ValidationErrors { get; set; }
    public int SkippedLines { get; set; }
    public TimeSpan ElapsedTime => (EndTime ?? DateTime.Now) - StartTime;
    public double LinesPerSecond => ElapsedTime.TotalSeconds > 0 
        ? ProcessedLines / ElapsedTime.TotalSeconds 
        : 0;
    public double SuccessRate => ProcessedLines > 0 
        ? (SuccessCount * 100.0 / ProcessedLines) 
        : 0;
    public double ErrorRate => ProcessedLines > 0 
        ? (ErrorCount * 100.0 / ProcessedLines) 
        : 0;
    public TimeSpan EstimatedTimeRemaining
    {
        get
        {
            if (LinesPerSecond <= 0 || TotalLines == 0)
                return TimeSpan.Zero;
            
            var remainingLines = TotalLines - ProcessedLines;
            var secondsRemaining = remainingLines / LinesPerSecond;
            return TimeSpan.FromSeconds(secondsRemaining);
        }
    }
    public double ProgressPercentage => TotalLines > 0 
        ? (ProcessedLines * 100.0 / TotalLines) 
        : 0;

    // Métricas de requisições HTTP
    public int TotalRetries { get; set; }
    public double AverageResponseTimeMs { get; set; }
    public long MinResponseTimeMs { get; set; } = long.MaxValue;
    public long MaxResponseTimeMs { get; set; }
    public Dictionary<int, int> HttpStatusCodes { get; set; } = new();

    // Métricas de batch
    public int BatchesProcessed { get; set; }
    public double AverageBatchTimeMs { get; set; }
}
