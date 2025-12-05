using n2n.Core.Models;

namespace n2n.Core.Abstractions;

public interface IDataSubscriber
{
    IAsyncEnumerable<MessageEnvelope> SubscribeAsync(string channel, CancellationToken ct);
}
