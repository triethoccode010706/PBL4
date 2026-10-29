using Shared.Models;

namespace Shared.Protocol
{
    public class DisconnectCommandHandler : ICommandHandler
    {
        public CommandType CommandType => CommandType.DISCONNECT;

        public Task<Response> HandleAsync(Command command, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Response.Ok(command.RequestId, "Disconnected"));
        }
    }
}
