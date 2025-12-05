using System.Text.Json.Nodes;

namespace n2n.Abstractions;

public interface ISourcePlugin
{
    IAsyncEnumerable<JsonNode> Extract(JsonNode pluginConfig, CancellationToken ct);
}
