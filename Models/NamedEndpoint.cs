namespace n2n.Models;

/// <summary>
///     Configuração de um endpoint
/// </summary>
public class NamedEndpoint
{
    public string Name { get; set; } = string.Empty;
    public string EndpointUrl { get; set; } = string.Empty;
    public Dictionary<string, string> Headers { get; set; } = new();
    public string Method { get; set; } = "POST";
    public int RequestTimeout { get; set; } = 30;
    public int RetryAttempts { get; set; } = 3;
    public int RetryDelaySeconds { get; set; } = 5;
    public int? MaxRequestsPerSecond { get; set; }
    public List<ApiMapping> Mapping { get; set; } = new();
}