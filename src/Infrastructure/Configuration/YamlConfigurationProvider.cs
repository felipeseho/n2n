using System.Text;
using Microsoft.Extensions.Configuration;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace n2n.Infrastructure.Configuration;

public class YamlConfigurationProvider : FileConfigurationProvider
{
    public YamlConfigurationProvider(FileConfigurationSource source) : base(source) { }

    public override void Load(Stream stream)
    {
        var deserializer = new DeserializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .Build();

        using var reader = new StreamReader(stream, Encoding.UTF8);
        var yamlObject = deserializer.Deserialize(reader);

        Data = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        if (yamlObject != null)
        {
            ProcessYamlObject(string.Empty, yamlObject);
        }
    }

    private void ProcessYamlObject(string context, object yamlObject)
    {
        if (yamlObject is IDictionary<object, object> dictionary)
        {
            foreach (var (key, value) in dictionary)
            {
                ProcessYamlObject(string.IsNullOrEmpty(context) ? key.ToString()! : $"{context}:{key}", value);
            }
        }
        else if (yamlObject is IList<object> list)
        {
            for (var i = 0; i < list.Count; i++)
            {
                ProcessYamlObject($"{context}:{i}", list[i]);
            }
        }
        else
        {
            Data[context] = yamlObject?.ToString();
        }
    }
}
