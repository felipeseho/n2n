using n2n.Models;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace n2n.Services;

/// <summary>
///     Serviço para carregar e validar configurações
/// </summary>
public class ConfigurationService
{
    /// <summary>
    ///     Carrega configuração a partir de arquivo YAML
    /// </summary>
    public Configuration LoadConfiguration(string path)
    {
        var yaml = File.ReadAllText(path);
        var deserializer = new DeserializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .Build();

        return deserializer.Deserialize<Configuration>(yaml);
    }

    /// <summary>
    ///     Mescla opções de linha de comando com a configuração carregada do YAML
    /// </summary>
    public Configuration MergeWithCommandLineOptions(Configuration config, CommandLineOptions options)
    {
        // Sobrescrever configurações de arquivo se fornecidas
        if (!string.IsNullOrWhiteSpace(options.InputPath))
            config.File.InputPath = options.InputPath;

        if (options.BatchLines.HasValue)
            config.File.BatchLines = options.BatchLines.Value;

        if (!string.IsNullOrWhiteSpace(options.LogDirectory))
            config.File.LogDirectory = options.LogDirectory;

        if (!string.IsNullOrWhiteSpace(options.CsvDelimiter))
            config.File.CsvDelimiter = options.CsvDelimiter;

        if (options.StartLine.HasValue)
            config.File.StartLine = options.StartLine.Value;

        if (options.MaxLines.HasValue)
            config.File.MaxLines = options.MaxLines.Value;

        return config;
    }

    /// <summary>
    ///     Retorna a configuração do endpoint a ser usado, baseado no nome do endpoint
    /// </summary>
    /// <param name="config">Configuração principal</param>
    /// <param name="endpointName">Nome do endpoint (pode vir de argumento ou CSV)</param>
    /// <returns>Configuração do endpoint especificado</returns>
    public NamedEndpoint GetEndpointConfiguration(Configuration config, string? endpointName = null)
    {
        // Se não há nome de endpoint especificado, usar endpoint padrão configurado
        if (string.IsNullOrWhiteSpace(endpointName))
        {
            if (!string.IsNullOrWhiteSpace(config.DefaultEndpoint))
                endpointName = config.DefaultEndpoint;
            else if (config.Endpoints.Count == 1)
                // Se há apenas um endpoint, usar ele
                return config.Endpoints[0];
            else
                throw new InvalidOperationException(
                    "Nome do endpoint não especificado. Use --endpoint-name, configure 'endpointColumnName' no CSV, " +
                    "ou defina 'defaultEndpoint' na configuração. " +
                    $"Endpoints disponíveis: {string.Join(", ", config.Endpoints.Select(e => e.Name))}");
        }

        // Buscar endpoint pelo nome
        var endpoint = config.Endpoints.FirstOrDefault(e =>
            e.Name.Equals(endpointName, StringComparison.OrdinalIgnoreCase));

        if (endpoint == null)
            throw new InvalidOperationException(
                $"Endpoint '{endpointName}' não encontrado na configuração. " +
                $"Endpoints disponíveis: {string.Join(", ", config.Endpoints.Select(e => e.Name))}");

        return endpoint;
    }

    /// <summary>
    ///     Valida a configuração carregada
    /// </summary>
    public bool ValidateConfiguration(Configuration config)
    {
        if (!File.Exists(config.File.InputPath))
        {
            Console.WriteLine($"Arquivo CSV não encontrado: {config.File.InputPath}");
            return false;
        }

        // Deve haver pelo menos um endpoint configurado
        if (config.Endpoints.Count == 0)
        {
            Console.WriteLine("É necessário configurar pelo menos um endpoint na lista 'endpoints'");
            return false;
        }

        // Validar cada endpoint
        foreach (var endpoint in config.Endpoints)
        {
            if (string.IsNullOrWhiteSpace(endpoint.Name))
            {
                Console.WriteLine("Todos os endpoints devem ter um 'name' definido");
                return false;
            }

            if (string.IsNullOrWhiteSpace(endpoint.EndpointUrl))
            {
                Console.WriteLine($"Endpoint '{endpoint.Name}' deve ter 'endpointUrl' definido");
                return false;
            }

            if (!ValidateApiMappings(endpoint.Mapping, endpoint.Name)) return false;
        }

        // Validar endpoint padrão se especificado
        if (!string.IsNullOrWhiteSpace(config.DefaultEndpoint))
        {
            var defaultExists = config.Endpoints.Any(e =>
                e.Name.Equals(config.DefaultEndpoint, StringComparison.OrdinalIgnoreCase));

            if (!defaultExists)
            {
                Console.WriteLine($"Endpoint padrão '{config.DefaultEndpoint}' não encontrado na lista de endpoints.");
                Console.WriteLine($"Endpoints disponíveis: {string.Join(", ", config.Endpoints.Select(e => e.Name))}");
                return false;
            }
        }

        return true;
    }

    /// <summary>
    ///     Valida os mappings de uma configuração de API
    /// </summary>
    private bool ValidateApiMappings(List<ApiMapping> mappings, string contextName)
    {
        foreach (var mapping in mappings)
        {
            if (string.IsNullOrWhiteSpace(mapping.Attribute))
            {
                Console.WriteLine($"Mapping em '{contextName}' deve ter um 'attribute' definido");
                return false;
            }

            // Cada mapping deve ter FixedValue OU CsvColumn, mas não ambos
            var hasFixedValue = !string.IsNullOrWhiteSpace(mapping.FixedValue);
            var hasCsvColumn = !string.IsNullOrWhiteSpace(mapping.CsvColumn);

            if (!hasFixedValue && !hasCsvColumn)
            {
                Console.WriteLine(
                    $"Mapping para '{mapping.Attribute}' em '{contextName}' deve ter 'fixedValue' ou 'csvColumn' definido");
                return false;
            }

            if (hasFixedValue && hasCsvColumn)
            {
                Console.WriteLine(
                    $"Mapping para '{mapping.Attribute}' em '{contextName}' não pode ter 'fixedValue' e 'csvColumn' ao mesmo tempo");
                return false;
            }
        }

        return true;
    }

    /// <summary>
    ///     Gera os caminhos de arquivos para uma execução específica
    /// </summary>
    public ExecutionPaths GenerateExecutionPaths(Configuration config, string executionId)
    {
        return new ExecutionPaths
        {
            ExecutionId = executionId,
            LogPath = Path.Combine(config.File.LogDirectory, $"process_{executionId}.log"),
            CheckpointPath = Path.Combine(config.File.CheckpointDirectory, $"checkpoint_{executionId}.json")
        };
    }

    /// <summary>
    ///     Cria diretórios necessários
    /// </summary>
    public void EnsureDirectoriesExist(Configuration config)
    {
        if (!string.IsNullOrEmpty(config.File.LogDirectory) && !Directory.Exists(config.File.LogDirectory))
            Directory.CreateDirectory(config.File.LogDirectory);

        if (!string.IsNullOrEmpty(config.File.CheckpointDirectory) &&
            !Directory.Exists(config.File.CheckpointDirectory))
            Directory.CreateDirectory(config.File.CheckpointDirectory);
    }
}