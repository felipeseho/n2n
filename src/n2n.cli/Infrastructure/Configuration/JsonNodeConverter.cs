using System.Text.Json.Nodes;
using YamlDotNet.Core;
using YamlDotNet.Serialization;

namespace n2n.Infrastructure.Configuration;

public class JsonNodeConverter : IYamlTypeConverter
{
    public bool Accepts(Type type) => type == typeof(JsonNode);

    public object? ReadYaml(IParser parser, Type type, ObjectDeserializer rootDeserializer)
    {
        var deserializer = new DeserializerBuilder().Build();
        var yamlObject = deserializer.Deserialize(parser);

        if (yamlObject is null)
        {
            return null;
        }

        var serializer = new SerializerBuilder()
            .JsonCompatible()
            .Build();

        var json = serializer.Serialize(yamlObject);

        return JsonNode.Parse(json);
    }

    public void WriteYaml(IEmitter emitter, object? value, Type type, ObjectSerializer rootSerializer)
    {
        if (value is null)
        {
            return;
        }
        
        var json = ((JsonNode)value).ToJsonString();
        var deserializer = new DeserializerBuilder().Build();
        var yamlObject = deserializer.Deserialize(json);

        var serializer = new SerializerBuilder().Build();
        serializer.Serialize(emitter, yamlObject);
    }
}
