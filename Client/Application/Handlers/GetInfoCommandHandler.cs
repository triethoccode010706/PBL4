using System.Text.Json;
using Client.Domain;
using Shared.Models;
using Shared.Protocol;


namespace Client.Application.Handlers
{
    public sealed class GetInfoCommandHandler : ICommandHandler
    {
        private readonly IMachineInfoProvider _machineInfoProvider;

        public GetInfoCommandHandler(
            IMachineInfoProvider machineInfoProvider)
        {
            _machineInfoProvider = machineInfoProvider;
        }

        public CommandType CommandType => CommandType.GET_INFO;

        public Task<Response> HandleAsync(
            Command command,
            CancellationToken cancellationToken = default)
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                MachineInfo info =
                    _machineInfoProvider.GetMachineInfo();

                cancellationToken.ThrowIfCancellationRequested();

                var dto = new MachineInfoDto
                {
                    MachineName = info.MachineName,
                    UserName = info.UserName,
                    IpAddress = info.IpAddress,
                    OsDescription = info.OsDescription,
                    CpuInfo = info.CpuInfo,
                    RamInfo = info.RamInfo
                };

                string json = JsonSerializer.Serialize(dto);

                return Task.FromResult(
                    Response.Ok(
                        command.RequestId,
                        "Thu thập thông tin hệ thống thành công",
                        json));
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Task.FromResult(
                    Response.Fail(
                        command.RequestId,
                        "SYSTEM_INFO_ERROR",
                        $"Không thể thu thập thông tin hệ thống: {ex.Message}"));
            }
        }
    }
}