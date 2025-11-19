namespace CsvToApi.Models;

/// <summary>
/// Modelo de checkpoint para retomar processamento
/// </summary>
public class Checkpoint
{
    public int LastProcessedLine { get; set; }
    public DateTime LastUpdate { get; set; }
    public int TotalProcessed { get; set; }
    public int SuccessCount { get; set; }
    public int ErrorCount { get; set; }
}
