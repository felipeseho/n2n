using n2n.Models;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace n2n.Services;

/// <summary>
///     Serviço para carregar configurações de pipeline
/// </summary>
public class PipelineConfigurationService
{
    /// <summary>
    ///     Carrega configuração de pipeline de um arquivo YAML
    /// </summary>
    public PipelineConfiguration LoadFromFile(string filePath)
    {
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"Arquivo de configuração não encontrado: {filePath}");
        }

        var yaml = File.ReadAllText(filePath);
        var deserializer = new DeserializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .Build();

        var configuration = deserializer.Deserialize<PipelineConfiguration>(yaml);

        if (configuration == null)
        {
            throw new InvalidOperationException($"Falha ao carregar configuração de: {filePath}");
        }

        return configuration;
    }

    /// <summary>
    ///     Valida a configuração do pipeline
    /// </summary>
    public List<string> ValidateConfiguration(PipelineConfiguration configuration)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(configuration.Name))
        {
            errors.Add("Nome do pipeline é obrigatório");
        }

        if (string.IsNullOrWhiteSpace(configuration.Source.Type))
        {
            errors.Add("Tipo de origem é obrigatório");
        }

        if (string.IsNullOrWhiteSpace(configuration.Destination.Type))
        {
            errors.Add("Tipo de destino é obrigatório");
        }
        
        return errors;
    }
}
