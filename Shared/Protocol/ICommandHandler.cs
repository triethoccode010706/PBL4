using Shared.Models;

namespace Shared.Protocol
{
    public interface ICommandHandler
    {
        CommandType CommandType { get; }

        Task<Response> HandleAsync(Command command, CancellationToken cancellationToken = default);
    }
}
