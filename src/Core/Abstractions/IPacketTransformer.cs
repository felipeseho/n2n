using System.Text.Json.Nodes;

namespace n2n.Core.Abstractions;

public interface IPacketTransformer
{
    JsonNode Transform(JsonNode data);
}
