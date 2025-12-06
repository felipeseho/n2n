using n2n.Models;

namespace n2n.Services;

/// <summary>
///     Serviço responsável por aplicar filtros aos registros do CSV
/// </summary>
public class FilterService
{
    private readonly List<ColumnFilter> _filters;

    public FilterService(List<ColumnMapping> columns)
    {
        // Extrair filtros das colunas que possuem configuração de filtro
        _filters = new List<ColumnFilter>();
        
        foreach (var column in columns)
        {
            if (column.Filters != null && column.Filters.Count > 0)
            {
                foreach (var filter in column.Filters)
                {
                    _filters.Add(new ColumnFilter
                    {
                        Column = column.Column,
                        Operator = filter.Operator,
                        Value = filter.Value,
                        CaseInsensitive = filter.CaseInsensitive
                    });
                }
            }
        }
    }

    /// <summary>
    ///     Verifica se um registro CSV passa por todos os filtros configurados
    /// </summary>
    /// <param name="record">Registro CSV a ser avaliado</param>
    /// <returns>True se o registro passa por todos os filtros, False caso contrário</returns>
    public bool PassesFilters(CsvRecord record)
    {
        // Se não houver filtros, todos os registros passam
        if (_filters.Count == 0) return true;

        // O registro deve passar por TODOS os filtros (operação AND)
        foreach (var filter in _filters)
            if (!ApplyFilter(record, filter))
                return false;

        return true;
    }

    /// <summary>
    ///     Aplica um filtro individual a um registro
    /// </summary>
    /// <param name="record">Registro CSV</param>
    /// <param name="filter">Filtro a ser aplicado</param>
    /// <returns>True se o registro passa pelo filtro</returns>
    private bool ApplyFilter(CsvRecord record, ColumnFilter filter)
    {
        // Verifica se a coluna existe no registro
        if (!record.Data.TryGetValue(filter.Column, out var value))
            // Se a coluna não existe, considera que não passou no filtro
            return false;

        // Converte os valores para comparação considerando case sensitivity
        var recordValue = value ?? string.Empty;
        var filterValue = filter.Value ?? string.Empty;

        if (filter.CaseInsensitive)
        {
            recordValue = recordValue.ToLowerInvariant();
            filterValue = filterValue.ToLowerInvariant();
        }

        // Aplica o operador do filtro
        return filter.Operator switch
        {
            FilterOperator.Equals => recordValue == filterValue,
            FilterOperator.NotEquals => recordValue != filterValue,
            FilterOperator.Contains => recordValue.Contains(filterValue),
            FilterOperator.NotContains => !recordValue.Contains(filterValue),
            _ => throw new InvalidOperationException($"Operador de filtro não suportado: {filter.Operator}")
        };
    }

    /// <summary>
    ///     Retorna estatísticas sobre os filtros aplicados
    /// </summary>
    public string GetFiltersSummary()
    {
        if (_filters.Count == 0) return "Nenhum filtro configurado";

        var summary = $"Filtros ativos ({_filters.Count}):\n";
        foreach (var filter in _filters)
        {
            var operatorText = filter.Operator switch
            {
                FilterOperator.Equals => "igual a",
                FilterOperator.NotEquals => "diferente de",
                FilterOperator.Contains => "contém",
                FilterOperator.NotContains => "não contém",
                _ => filter.Operator.ToString()
            };

            var caseSensitivity = filter.CaseInsensitive ? "(ignorar maiúsculas/minúsculas)" : "(case-sensitive)";
            summary += $"  - Coluna '{filter.Column}' {operatorText} '{filter.Value}' {caseSensitivity}\n";
        }

        return summary.TrimEnd('\n');
    }
}