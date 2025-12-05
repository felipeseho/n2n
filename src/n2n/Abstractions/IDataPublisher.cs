using n2n.Models;

namespace n2n.Abstractions;

public interface IDataPublisher
{
    ValueTask Publish(string channel, MessageEnvelope message);
}
