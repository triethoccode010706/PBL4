using Client.Domain;
using Client.Infrastructure;
using Shared.Protocol;
namespace Client.Application
{
    public sealed class ClientAgent : IClientAgent
    {
        private readonly ITcpClient _tcpClient;
        private readonly IMachineInfoProvider _machineInfoProvider;
        private readonly IScreenCapture _screenCapture;
        private readonly IScreenStreamingService _streaming;
        public event Action<string>? Log;

        public ClientAgent(ITcpClient tcpClient,IMachineInfoProvider machineInfoProvider,IScreenCapture screenCapture,IScreenStreamingService streaming)
        {
            _tcpClient = tcpClient;
            _machineInfoProvider = machineInfoProvider;
            _screenCapture = screenCapture;
            _streaming = streaming;
        }

        public async Task RunAsync(
            string serverIp,
            int port,
            CancellationToken cancellationToken = default)
        {
            string? secretKey =Environment.GetEnvironmentVariable("PBL4_AUTH_KEY");

            if (string.IsNullOrWhiteSpace(secretKey))
            {
                throw new InvalidOperationException(
                    "Chưa cấu hình biến môi trường PBL4_AUTH_KEY.");
            }

            var dispatcher = ClientDispatcherFactory.Create( _machineInfoProvider, secretKey, _screenCapture, _streaming, cancellationToken);
            bool connected = await _tcpClient.ConnectAsync(serverIp, port, cancellationToken);
            if (!connected)
            {
                WriteLog("Không kết nối được máy quản lý.");
                return;
            }

            var protocol = new ProtocolSession(_tcpClient, _tcpClient.Stream, dispatcher);

            protocol.CommandReceived += command =>
                WriteLog($"[Máy quản lý] {command.Type} (id={command.RequestId})");

            protocol.ResponseSent += response =>
            {
                if (response.Success)
                {
                    WriteLog($"[OK] {response.Message}");
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
                try
                {
                    await _streaming.StopAsync();
                }
                finally
                {
                    _tcpClient.Disconnect();
                }
            }
        }

        private void WriteLog(string message)
        {
            Log?.Invoke(message);
        }
    }
}
