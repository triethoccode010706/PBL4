using Shared.Models;

namespace Shared.Protocol
{
    public interface IMessageValidator
    {
        void ValidateCommand(Command command);
        void ValidateResponse(Response response);
        void ValidateEnvelope(ProtocolEnvelope envelope);
    }
}
