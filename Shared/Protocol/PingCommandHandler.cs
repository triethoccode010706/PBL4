using Shared.Models;

namespace Shared.Protocol
{
    public class PingCommandHandler : ICommandHandler
    {
        public CommandType CommandType => CommandType.PING;

        public Task<Response> HandleAsync(Command command, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Response.Ok(command.RequestId, "PONG"));
        }
    }
}
