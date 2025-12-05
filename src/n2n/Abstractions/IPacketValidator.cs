using System.Text.Json.Nodes;

namespace n2n.Abstractions;

public interface IPacketValidator
{
    bool IsValid(JsonNode data);
}
