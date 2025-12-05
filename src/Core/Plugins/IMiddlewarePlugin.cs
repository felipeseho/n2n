using System.Text.Json.Nodes;

namespace n2n.Core.Plugins;

public interface IMiddlewarePlugin
{
    JsonNode Process(JsonNode data, JsonNode pluginConfig);
}
