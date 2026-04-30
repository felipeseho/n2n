namespace n2n.Models;

/// <summary>
///     Configuração principal da aplicação
/// </summary>
public class Configuration
{
    public FileConfiguration File { get; set; } = new();
    public List<NamedEndpoint> Endpoints { get; set; } = new();

    /// <summary>
    ///     Coluna CSV que contém o nome do endpoint a ser usado
    /// </summary>
    public string? EndpointColumnName { get; set; }

    /// <summary>
    ///     Nome do endpoint padrão a ser usado quando não especificado
    /// </summary>
    public string? DefaultEndpoint { get; set; }

    /// <summary>
    ///     Roteamento dinâmico: mapeia valores de uma coluna CSV para nomes de endpoints
    /// </summary>
    public EndpointRouting? Routing { get; set; }
}