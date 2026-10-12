using Shared.Models;

namespace Shared.Protocol
{
    public interface IMessageSerializer
    {
        byte[] SerializeCommand(Command command);
        Command DeserializeCommand(byte[] payload);
        byte[] SerializeResponse(Response response);
        Response DeserializeResponse(byte[] payload);
        byte[] SerializeEnvelope(ProtocolEnvelope envelope);
        ProtocolEnvelope DeserializeEnvelope(byte[] payload);
    }
}
