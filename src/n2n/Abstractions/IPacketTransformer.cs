using System.Text.Json.Nodes;

namespace n2n.Abstractions;

public interface IPacketTransformer
{
    JsonNode Transform(JsonNode data);
}
