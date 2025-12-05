using System.Text.Json.Nodes;

namespace n2n.Models;

public record MessageEnvelope(
    JsonNode Payload,
    Guid CorrelationId,
    DateTime Timestamp,
    string SourceType
);
