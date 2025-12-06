using n2n.Models;

namespace n2n.Utils;

/// <summary>
///     Utilitário para construção de payloads da API
/// </summary>
public static class PayloadBuilder
{
    /// <summary>
    ///     Constrói o payload JSON a partir de um registro CSV
    /// </summary>
    public static Dictionary<string, object> BuildApiPayload(CsvRecord record, List<ApiMapping> mappings)
    {
        var payload = new Dictionary<string, object>();

        foreach (var mapping in mappings)
        {
            string? transformedValue;

            // Se há uma fórmula, avaliá-la
            if (!string.IsNullOrEmpty(mapping.Formula))
            {
                transformedValue = FormulaEvaluator.EvaluateFormula(mapping.Formula);
            }
            // Se há um valor fixo, usá-lo diretamente
            else if (!string.IsNullOrEmpty(mapping.FixedValue))
            {
                transformedValue = mapping.FixedValue;
            }
            // Caso contrário, buscar valor da coluna CSV
            else if (!string.IsNullOrEmpty(mapping.CsvColumn) &&
                     record.Data.TryGetValue(mapping.CsvColumn, out var value))
            {
                // Aplicar múltiplas transformações se especificadas (novo formato)
                if (mapping.Transforms != null && mapping.Transforms.Count > 0)
                {
                    transformedValue = DataTransformer.ApplyTransformations(value, mapping.Transforms);
                }
                // Aplicar transformação única se especificada (formato antigo - retrocompatibilidade)
                else if (!string.IsNullOrEmpty(mapping.Transform))
                {
                    transformedValue = DataTransformer.ApplyTransformation(value, mapping.Transform);
                }
                else
                {
                    transformedValue = value;
                }
            }
            else
            {
                continue;
            }

            // Se o valor transformado for null, pular este mapeamento
            if (transformedValue == null)
                continue;

            // Suportar atributos aninhados (ex: "address.street")
            var parts = mapping.Attribute.Split('.');
            if (parts.Length == 1)
            {
                payload[mapping.Attribute] = transformedValue;
            }
            else
            {
                // Criar estrutura aninhada
                var current = payload;
                for (var i = 0; i < parts.Length - 1; i++)
                {
                    if (!current.ContainsKey(parts[i])) current[parts[i]] = new Dictionary<string, object>();
                    current = (Dictionary<string, object>)current[parts[i]];
                }

                current[parts[^1]] = transformedValue;
            }
        }

        return payload;
    }
}