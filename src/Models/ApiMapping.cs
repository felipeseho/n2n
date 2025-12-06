namespace n2n.Models;

/// <summary>
///     Mapeamento entre coluna CSV e atributo da API
/// </summary>
public class ApiMapping
{
    public string Attribute { get; set; } = string.Empty;
    public string? CsvColumn { get; set; }
    
    /// <summary>
    ///     Transformação única a ser aplicada (para compatibilidade com configurações antigas)
    /// </summary>
    public string? Transform { get; set; }
    
    /// <summary>
    ///     Lista de transformações a serem aplicadas em sequência (modo recomendado para múltiplas transformações)
    /// </summary>
    public List<string>? Transforms { get; set; }
    
    public string? FixedValue { get; set; }
    
    /// <summary>
    ///     Fórmula a ser avaliada para gerar valor dinamicamente (ex: now(), uuid(), today())
    /// </summary>
    public string? Formula { get; set; }
}