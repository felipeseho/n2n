namespace n2n.Models;

/// <summary>
///     Tipo de operação de filtro
/// </summary>
public enum FilterOperator
{
    /// <summary>
    ///     Valor exatamente igual
    /// </summary>
    Equals,

    /// <summary>
    ///     Valor diferente
    /// </summary>
    NotEquals,

    /// <summary>
    ///     Valor contém o texto especificado
    /// </summary>
    Contains,

    /// <summary>
    ///     Valor não contém o texto especificado
    /// </summary>
    NotContains
}

/// <summary>
///     Filtro a ser aplicado em uma coluna do CSV
/// </summary>
public class ColumnFilter
{
    /// <summary>
    ///     Nome da coluna a ser filtrada
    /// </summary>
    public string Column { get; set; } = string.Empty;

    /// <summary>
    ///     Operador do filtro
    /// </summary>
    public FilterOperator Operator { get; set; }

    /// <summary>
    ///     Valor a ser comparado
    /// </summary>
    public string Value { get; set; } = string.Empty;

    /// <summary>
    ///     Se true, a comparação ignora maiúsculas/minúsculas
    /// </summary>
    public bool CaseInsensitive { get; set; } = true;
}