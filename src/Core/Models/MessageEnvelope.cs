using System.Text.Json.Nodes;

namespace n2n.Core.Models;

public record MessageEnvelope(
    JsonNode Payload,
    Guid CorrelationId,
    DateTime Timestamp,
    string SourceType
);
