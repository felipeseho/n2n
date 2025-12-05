using System.Text.Json.Nodes;

namespace n2n.Core.Plugins;

public interface ISourcePlugin
{
    IAsyncEnumerable<JsonNode> ExtractAsync(JsonNode pluginConfig, CancellationToken ct);
}
