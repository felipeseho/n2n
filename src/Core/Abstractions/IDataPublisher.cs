using n2n.Core.Models;

namespace n2n.Core.Abstractions;

public interface IDataPublisher
{
    ValueTask PublishAsync(string channel, MessageEnvelope message);
}
