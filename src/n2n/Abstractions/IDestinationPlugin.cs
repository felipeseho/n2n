using System.Text.Json.Nodes;

namespace n2n.Abstractions;

public interface IDestinationPlugin
{
    Task Load(JsonNode data, JsonNode pluginConfig, CancellationToken ct);
}
