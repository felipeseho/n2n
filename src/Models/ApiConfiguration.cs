namespace n2n.Models;

/// <summary>
///     Configuração de API: endpoints, roteamento e defaults
/// </summary>
public class ApiConfiguration
{
    public string? DefaultEndpoint { get; set; }

    public string? EndpointColumnName { get; set; }

    public EndpointRouting? Routing { get; set; }

    public List<NamedEndpoint> Endpoints { get; set; } = new();
}
