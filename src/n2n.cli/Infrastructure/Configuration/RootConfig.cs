using System.Text.Json.Nodes;

namespace n2n.Infrastructure.Configuration;

public record RootConfig
{
    public List<SourceConfig> Sources { get; init; } = new();
    public List<DestinationConfig> Destinations { get; init; } = new();
    public List<PipelineConfig> Pipelines { get; init; } = new();
}

public record SourceConfig
{
    public string Id { get; init; } = string.Empty;
    public string Type { get; init; } = string.Empty;
    public JsonNode? Config { get; init; }
}

public record DestinationConfig
{
    public string Id { get; init; } = string.Empty;
    public string Type { get; init; } = string.Empty;
    public JsonNode? Config { get; init; }
}

public record PipelineConfig
{
    public string Id { get; init; } = string.Empty;
    public string Source { get; init; } = string.Empty;
    public List<PipelineDestinationConfig> Destinations { get; init; } = new();
}

public record PipelineDestinationConfig
{
    public string Id { get; init; } = string.Empty;
    public JsonNode? Mapping { get; init; }
}
