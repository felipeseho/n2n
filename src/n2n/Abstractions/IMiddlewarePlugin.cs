using System.Text.Json.Nodes;

namespace n2n.Abstractions;

public interface IMiddlewarePlugin
{
    JsonNode Process(JsonNode data, JsonNode pluginConfig);
}
