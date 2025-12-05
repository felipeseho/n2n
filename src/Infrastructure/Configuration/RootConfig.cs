using System.Text.Json.Nodes;

namespace n2n.Infrastructure.Configuration;

public class RootConfig
{
    public List<SourceConfig> Sources { get; set; } = new();
    public List<DestinationConfig> Destinations { get; set; } = new();
    public List<PipelineConfig> Pipelines { get; set; } = new();
}

public class SourceConfig
{
    public string Id { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public JsonNode? Config { get; set; }
}

public class DestinationConfig
{
    public string Id { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public JsonNode? Config { get; set; }
}

public class PipelineConfig
{
    public string Id { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public List<string> Destinations { get; set; } = new();
    public JsonNode? Mapping { get; set; }
}
