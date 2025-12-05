using System.Text.Json.Nodes;

namespace n2n.Core.Abstractions;

public interface IPacketValidator
{
    bool IsValid(JsonNode data);
}
