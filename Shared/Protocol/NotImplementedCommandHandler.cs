using Shared.Models;

namespace Shared.Protocol
{
    public class NotImplementedCommandHandler : ICommandHandler
    {
        public CommandType CommandType { get; }

        public NotImplementedCommandHandler(CommandType commandType)
        {
            CommandType = commandType;
        }

        public Task<Response> HandleAsync(Command command, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Response.Fail(
                command.RequestId,
                ErrorCodes.NotImplemented,
                $"{command.Type} chưa được triển khai."));
        }
    }
}
