namespace CsvToApi.Models;

/// <summary>
/// Configuração da API REST
/// </summary>
public class ApiConfiguration
{
    public string EndpointUrl { get; set; } = string.Empty;
    public string AuthToken { get; set; } = string.Empty;
    public string Method { get; set; } = "POST";
    public int RequestTimeout { get; set; } = 30;
    public int RetryAttempts { get; set; } = 3;
    public int RetryDelaySeconds { get; set; } = 5;
    public int? MaxRequestsPerSecond { get; set; }
    public List<ApiMapping> Mapping { get; set; } = new();
}

