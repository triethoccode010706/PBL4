namespace Shared.Protocol
{
    public interface IMessageFramer
    {
        int HeaderSize { get; }
        int MaxPayloadSize { get; }

        byte[] Frame(byte[] payload);

        Task<byte[]?> ReadPayloadAsync(Stream stream, CancellationToken cancellationToken = default);
    }
}
