using System.Text.Json;
using System.Text.Json.Serialization;
using Shared.Models;

namespace Shared.Protocol
{
    public class MessageSerializer : IMessageSerializer
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            Converters = { new JsonStringEnumConverter() }
        };

        public byte[] SerializeCommand(Command command)
        {
            return JsonSerializer.SerializeToUtf8Bytes(command, JsonOptions);
        }

        public Command DeserializeCommand(byte[] payload)
        {
            return DeserializeRequired<Command>(payload, "Command JSON không hợp lệ.");
        }

        public byte[] SerializeResponse(Response response)
        {
            return JsonSerializer.SerializeToUtf8Bytes(response, JsonOptions);
        }

        public Response DeserializeResponse(byte[] payload)
        {
            return DeserializeRequired<Response>(payload, "Response JSON không hợp lệ.");
        }

        public byte[] SerializeEnvelope(ProtocolEnvelope envelope)
        {
            return JsonSerializer.SerializeToUtf8Bytes(envelope, JsonOptions);
        }

        public ProtocolEnvelope DeserializeEnvelope(byte[] payload)
        {
            return DeserializeRequired<ProtocolEnvelope>(payload, "Envelope JSON không hợp lệ.");
        }

        private static T DeserializeRequired<T>(byte[] payload, string errorMessage)
        {
            if (payload == null || payload.Length == 0)
            {
                throw new ProtocolValidationException(errorMessage);
            }

            try
            {
                T? value = JsonSerializer.Deserialize<T>(payload, JsonOptions);
                if (value == null)
                {
                    throw new ProtocolValidationException(errorMessage);
                }

                return value;
            }
            catch (JsonException ex)
            {
                throw new ProtocolValidationException($"{errorMessage} {ex.Message}");
            }
        }
    }
}
