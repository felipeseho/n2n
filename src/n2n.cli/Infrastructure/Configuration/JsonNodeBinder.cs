using System.Text.Json.Nodes;

namespace n2n.Infrastructure.Configuration;

public static class JsonNodeBinder
{
    public static JsonNode? BindToJsonNode(IConfigurationSection section)
    {
        if (!section.GetChildren().Any())
        {
            var value = section.Value;
            return value != null ? JsonValue.Create(value as object) : null;
        }

        if (int.TryParse(section.GetChildren().First().Key, out _))
        {
            var array = new JsonArray();
            foreach (var child in section.GetChildren())
            {
                var childNode = BindToJsonNode(child);
                if (childNode != null)
                {
                    array.Add(childNode);
                }
            }
            return array;
        }
        else
        {
            var obj = new JsonObject();
            foreach (var child in section.GetChildren())
            {
                var childNode = BindToJsonNode(child);
                obj[child.Key] = childNode;
            }
            return obj;
        }
    }

    public static RootConfig BindRootConfig(this IConfiguration configuration)
    {
        var rootConfig = new RootConfig();

        var sourcesSection = configuration.GetSection("sources");
        foreach (var sourceSection in sourcesSection.GetChildren())
        {
            var sourceConfig = new SourceConfig
            {
                Id = sourceSection["id"] ?? string.Empty,
                Type = sourceSection["type"] ?? string.Empty,
                Config = BindToJsonNode(sourceSection.GetSection("config"))
            };
            rootConfig.Sources.Add(sourceConfig);
        }

        var destinationsSection = configuration.GetSection("destinations");
        foreach (var destSection in destinationsSection.GetChildren())
        {
            var destConfig = new DestinationConfig
            {
                Id = destSection["id"] ?? string.Empty,
                Type = destSection["type"] ?? string.Empty,
                Config = BindToJsonNode(destSection.GetSection("config"))
            };
            rootConfig.Destinations.Add(destConfig);
        }

        var pipelinesSection = configuration.GetSection("pipelines");
        foreach (var pipelineSection in pipelinesSection.GetChildren())
        {
            var pipelineConfig = new PipelineConfig
            {
                Id = pipelineSection["id"] ?? string.Empty,
                Source = pipelineSection["source"] ?? string.Empty,
                Mapping = BindToJsonNode(pipelineSection.GetSection("mapping"))
            };

            var destinationsArray = pipelineSection.GetSection("destinations");
            foreach (var dest in destinationsArray.GetChildren())
            {
                if (dest.Value != null)
                {
                    pipelineConfig.Destinations.Add(dest.Value);
                }
            }

            rootConfig.Pipelines.Add(pipelineConfig);
        }

        return rootConfig;
    }
}

