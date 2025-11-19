using CsvToApi.Models;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace CsvToApi.Services;

/// <summary>
/// Serviço para carregar e validar configurações
/// </summary>
public class ConfigurationService
{
    /// <summary>
    /// Carrega configuração a partir de arquivo YAML
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
    /// Mescla opções de linha de comando com a configuração carregada do YAML
    /// </summary>
    public Configuration MergeWithCommandLineOptions(Configuration config, CommandLineOptions options)
    {
        // Sobrescrever configurações de arquivo se fornecidas
        if (!string.IsNullOrWhiteSpace(options.InputPath))
            config.File.InputPath = options.InputPath;
        
        if (options.BatchLines.HasValue)
            config.File.BatchLines = options.BatchLines.Value;
        
        if (!string.IsNullOrWhiteSpace(options.LogPath))
            config.File.LogPath = options.LogPath;
        
        if (!string.IsNullOrWhiteSpace(options.CsvDelimiter))
            config.File.CsvDelimiter = options.CsvDelimiter;
        
        if (options.StartLine.HasValue)
            config.File.StartLine = options.StartLine.Value;
        
        if (options.MaxLines.HasValue)
            config.File.MaxLines = options.MaxLines.Value;
        
        // Sobrescrever resetCheckpoint se fornecido
        if (options.ResetCheckpoint)
            config.File.ResetCheckpoint = options.ResetCheckpoint;
        
        // Sobrescrever configurações de API se fornecidas
        if (!string.IsNullOrWhiteSpace(options.EndpointUrl))
            config.Api.EndpointUrl = options.EndpointUrl;
        
        if (!string.IsNullOrWhiteSpace(options.AuthToken))
            config.Api.AuthToken = options.AuthToken;
        
        if (!string.IsNullOrWhiteSpace(options.Method))
            config.Api.Method = options.Method;
        
        if (options.RequestTimeout.HasValue)
            config.Api.RequestTimeout = options.RequestTimeout.Value;
        
        return config;
    }

    /// <summary>
    /// Valida a configuração carregada
    /// </summary>
    public bool ValidateConfiguration(Configuration config)
    {
        if (!File.Exists(config.File.InputPath))
        {
            Console.WriteLine($"Arquivo CSV não encontrado: {config.File.InputPath}");
            return false;
        }

        if (string.IsNullOrWhiteSpace(config.Api.EndpointUrl))
        {
            Console.WriteLine("URL do endpoint da API não configurada");
            return false;
        }

        // Validar mappings da API
        foreach (var mapping in config.Api.Mapping)
        {
            if (string.IsNullOrWhiteSpace(mapping.Attribute))
            {
                Console.WriteLine("Mapping da API deve ter um 'attribute' definido");
                return false;
            }

            // Cada mapping deve ter FixedValue OU CsvColumn, mas não ambos
            var hasFixedValue = !string.IsNullOrWhiteSpace(mapping.FixedValue);
            var hasCsvColumn = !string.IsNullOrWhiteSpace(mapping.CsvColumn);

            if (!hasFixedValue && !hasCsvColumn)
            {
                Console.WriteLine($"Mapping para '{mapping.Attribute}' deve ter 'fixedValue' ou 'csvColumn' definido");
                return false;
            }

            if (hasFixedValue && hasCsvColumn)
            {
                Console.WriteLine($"Mapping para '{mapping.Attribute}' não pode ter 'fixedValue' e 'csvColumn' ao mesmo tempo");
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Cria diretórios necessários
    /// </summary>
    public void EnsureDirectoriesExist(Configuration config)
    {
        var logDir = Path.GetDirectoryName(config.File.LogPath);
        if (!string.IsNullOrEmpty(logDir) && !Directory.Exists(logDir))
        {
            Directory.CreateDirectory(logDir);
        }
    }
}

