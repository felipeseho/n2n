using System.Text.Json.Nodes;

namespace n2n.Core.Plugins;

public interface IDestinationPlugin
{
    Task LoadAsync(JsonNode data, JsonNode pluginConfig, CancellationToken ct);
}
