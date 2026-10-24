using Client.Domain;
using Shared.Models;
using Shared.Protocol;
using System.Text.Json;
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

        public Task<Response> HandleAsync(Command command,CancellationToken cancellationToken = default)
        {
            MachineInfo info = _machineInfoProvider.GetMachineInfo();

            var dto = new MachineInfoDto
            {
                MachineName = info.MachineName,
                UserName = info.UserName,
                OsDescription = info.OsDescription
            };

            string json = JsonSerializer.Serialize(dto);

            Response response = Response.Ok(
                command.RequestId,
                "Machine info",
                json);

            return Task.FromResult(response);
        }
    }
}
