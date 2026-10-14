using Client.Domain;
using Shared.Models;
using Shared.Protocol;

namespace Client.Application.Handlers
{
    public sealed class GetInfoCommandHandler : ICommandHandler
    {
        private readonly IMachineInfoProvider _machineInfoProvider;

        public GetInfoCommandHandler(IMachineInfoProvider machineInfoProvider)
        {
            _machineInfoProvider = machineInfoProvider;
        }

        public CommandType CommandType => CommandType.GET_INFO;

        public Task<Response> HandleAsync(
            Command command,
            CancellationToken cancellationToken = default)
        {
            MachineInfo info = _machineInfoProvider.GetMachineInfo();
            return Task.FromResult(Response.Ok(command.RequestId, "Machine info", info.ToString()));
        }
    }
}
