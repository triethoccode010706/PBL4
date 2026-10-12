using System.Buffers.Binary;

namespace Shared.Protocol
{
    public class MessageFramer : IMessageFramer
    {
        public const int DefaultMaxPayloadSize = 8 * 1024 * 1024;

        public int HeaderSize => 4;
        public int MaxPayloadSize { get; }

        public MessageFramer(int maxPayloadSize = DefaultMaxPayloadSize)
        {
            if (maxPayloadSize <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maxPayloadSize));
            }

            MaxPayloadSize = maxPayloadSize;
        }

        public byte[] Frame(byte[] payload)
        {
            ArgumentNullException.ThrowIfNull(payload);
            if (payload.Length == 0 || payload.Length > MaxPayloadSize)
            {
                throw new ProtocolValidationException(
                    $"Độ dài payload không hợp lệ: {payload.Length}.");
            }

            byte[] packet = new byte[HeaderSize + payload.Length];
            BinaryPrimitives.WriteInt32LittleEndian(packet.AsSpan(0, HeaderSize), payload.Length);
            Buffer.BlockCopy(payload, 0, packet, HeaderSize, payload.Length);
            return packet;
        }

        public async Task<byte[]?> ReadPayloadAsync(Stream stream, CancellationToken cancellationToken = default)
        {
            byte[] header = new byte[HeaderSize];
            int headerRead = await ReadExactAsync(stream, header, cancellationToken);
            if (headerRead == 0)
            {
                return null;
            }

            if (headerRead < HeaderSize)
            {
                return null;
            }

            int length = BinaryPrimitives.ReadInt32LittleEndian(header);
            if (length <= 0 || length > MaxPayloadSize)
            {
                throw new ProtocolValidationException($"Độ dài payload không hợp lệ: {length}.");
            }

            byte[] payload = new byte[length];
            int payloadRead = await ReadExactAsync(stream, payload, cancellationToken);
            if (payloadRead < length)
            {
                return null;
            }

            return payload;
        }

        private static async Task<int> ReadExactAsync(
            Stream stream,
            byte[] buffer,
            CancellationToken cancellationToken)
        {
            int total = 0;
            while (total < buffer.Length)
            {
                int read = await stream.ReadAsync(
                    buffer.AsMemory(total, buffer.Length - total),
                    cancellationToken);

                if (read == 0)
                {
                    return total;
                }

                total += read;
            }

            return total;
        }
    }
}
