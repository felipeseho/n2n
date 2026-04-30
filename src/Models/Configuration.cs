namespace n2n.Models;

/// <summary>
///     Configuração principal da aplicação
/// </summary>
public class Configuration
{
    public FileConfiguration File { get; set; } = new();
    public ApiConfiguration Api { get; set; } = new();
}