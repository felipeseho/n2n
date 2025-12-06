using System.Globalization;

namespace n2n.Utils;

/// <summary>
///     Utilitário para avaliação de fórmulas dinâmicas
/// </summary>
public static class FormulaEvaluator
{
    /// <summary>
    ///     Avalia uma fórmula e retorna o valor calculado
    /// </summary>
    /// <param name="formula">Fórmula a avaliar (ex: now(), uuid(), today())</param>
    /// <returns>Valor calculado pela fórmula</returns>
    public static string? EvaluateFormula(string? formula)
    {
        if (string.IsNullOrWhiteSpace(formula))
            return null;

        var normalizedFormula = formula.Trim().ToLower();

        // Fórmulas sem parâmetros
        if (normalizedFormula == "now()")
            return DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ssZ");

        if (normalizedFormula == "utcnow()")
            return DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");

        if (normalizedFormula == "today()")
            return DateTime.Today.ToString("yyyy-MM-dd");

        if (normalizedFormula == "uuid()" || normalizedFormula == "guid()")
            return Guid.NewGuid().ToString();

        if (normalizedFormula == "timestamp()")
            return DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();

        if (normalizedFormula == "timestamp_ms()")
            return DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString();

        // Fórmulas com parâmetros
        if (normalizedFormula.StartsWith("now(") && normalizedFormula.EndsWith(")"))
        {
            var format = ExtractParameter(normalizedFormula, "now");
            return format != null ? DateTime.Now.ToString(format) : DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ssZ");
        }

        if (normalizedFormula.StartsWith("utcnow(") && normalizedFormula.EndsWith(")"))
        {
            var format = ExtractParameter(normalizedFormula, "utcnow");
            return format != null ? DateTime.UtcNow.ToString(format) : DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");
        }

        if (normalizedFormula.StartsWith("today(") && normalizedFormula.EndsWith(")"))
        {
            var format = ExtractParameter(normalizedFormula, "today");
            return format != null ? DateTime.Today.ToString(format) : DateTime.Today.ToString("yyyy-MM-dd");
        }

        if (normalizedFormula.StartsWith("adddays(") && normalizedFormula.EndsWith(")"))
        {
            var param = ExtractParameter(normalizedFormula, "adddays");
            if (param != null && int.TryParse(param, out var days))
                return DateTime.Today.AddDays(days).ToString("yyyy-MM-dd");
        }

        if (normalizedFormula.StartsWith("addhours(") && normalizedFormula.EndsWith(")"))
        {
            var param = ExtractParameter(normalizedFormula, "addhours");
            if (param != null && int.TryParse(param, out var hours))
                return DateTime.Now.AddHours(hours).ToString("yyyy-MM-ddTHH:mm:ssZ");
        }

        if (normalizedFormula.StartsWith("random(") && normalizedFormula.EndsWith(")"))
        {
            var param = ExtractParameter(normalizedFormula, "random");
            if (param != null)
            {
                var parts = param.Split(',');
                if (parts.Length == 2 && 
                    int.TryParse(parts[0].Trim(), out var min) && 
                    int.TryParse(parts[1].Trim(), out var max))
                {
                    return Random.Shared.Next(min, max + 1).ToString();
                }
            }
        }

        // Fórmula não reconhecida, retorna a string original
        return formula;
    }

    /// <summary>
    ///     Extrai o parâmetro de uma fórmula com formato: funcao(parametro)
    /// </summary>
    /// <param name="formula">Fórmula completa</param>
    /// <param name="functionName">Nome da função</param>
    /// <returns>Parâmetro extraído ou null</returns>
    private static string? ExtractParameter(string formula, string functionName)
    {
        var startIndex = functionName.Length + 1; // +1 para pular o '('
        var endIndex = formula.LastIndexOf(')');
        
        if (endIndex <= startIndex)
            return null;

        var param = formula.Substring(startIndex, endIndex - startIndex).Trim();
        
        // Remove aspas se houver
        if (param.StartsWith("'") && param.EndsWith("'"))
            param = param.Substring(1, param.Length - 2);
        else if (param.StartsWith("\"") && param.EndsWith("\""))
            param = param.Substring(1, param.Length - 2);

        return string.IsNullOrWhiteSpace(param) ? null : param;
    }
}
