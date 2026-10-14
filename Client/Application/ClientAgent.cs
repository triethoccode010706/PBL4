using Client.Domain;
using Client.Infrastructure;
using Shared.Protocol;

namespace Client.Application
{
    public sealed class ClientAgent : IClientAgent
    {
        private readonly ITcpClient _tcpClient;
        private readonly IMachineInfoProvider _machineInfoProvider;

        public event Action<string>? Log;

        public ClientAgent(ITcpClient tcpClient, IMachineInfoProvider machineInfoProvider)
        {
            _tcpClient = tcpClient;
            _machineInfoProvider = machineInfoProvider;
        }

        public async Task RunAsync(
            string serverIp,
            int port,
            CancellationToken cancellationToken = default)
        {
            bool connected = await _tcpClient.ConnectAsync(serverIp, port, cancellationToken);
            if (!connected)
            {
                WriteLog("Không kết nối được máy quản lý.");
                return;
            }

            var protocol = new ProtocolSession(
                _tcpClient,
                _tcpClient.Stream,
                ClientDispatcherFactory.Create(_machineInfoProvider));

            protocol.CommandReceived += command =>
                WriteLog($"[Máy quản lý] {command.Type} (id={command.RequestId})");

            protocol.ResponseSent += response =>
            {
                if (response.Success)
                {
                    WriteLog($"[OK] {response.Message} {response.Data}".Trim());
                }
                else
                {
                    WriteLog($"[LỖI {response.ErrorCode}] {response.Message}");
                }
            };

            WriteLog("Đã kết nối máy quản lý. Đang chờ lệnh... (Ctrl+C để thoát)");

            try
            {
                await protocol.RunAsync(cancellationToken);
            }
            finally
            {
                _tcpClient.Disconnect();
            }
        }

        private void WriteLog(string message)
        {
            Log?.Invoke(message);
        }
    }
}
