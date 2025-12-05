using n2n.Models;

namespace n2n.Abstractions;

public interface IDataSubscriber
{
    IAsyncEnumerable<MessageEnvelope> Subscribe(string channel, CancellationToken ct);
}
