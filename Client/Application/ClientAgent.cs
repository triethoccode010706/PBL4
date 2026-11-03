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
        private readonly IMouseController _mouseController;
        private readonly IKeyboardController _keyboardController;
        public event Action<string>? Log;

        public ClientAgent(ITcpClient tcpClient,IMachineInfoProvider machineInfoProvider,IScreenCapture screenCapture,
            IScreenStreamingService streaming,IMouseController mouseController, IKeyboardController keyboardController)
        {
            _tcpClient = tcpClient;
            _machineInfoProvider = machineInfoProvider;
            _screenCapture = screenCapture;
            _streaming = streaming;
            _mouseController = mouseController;
            _keyboardController = keyboardController;
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

            var dispatcher = ClientDispatcherFactory.Create( _machineInfoProvider, secretKey, _screenCapture, _streaming,
                _mouseController,_keyboardController,ReleaseInputState,cancellationToken);
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
                    _keyboardController.ReleaseAll();
                }
                catch (Exception ex)
                {
                    WriteLog(
                        "Không thể thả hết phím khi kết thúc phiên: "
                        + ex.Message);
                }
                finally
                {
                    try
                    {
                        ReleaseInputState();
                    }
                    catch (Exception ex)
                    {
                        WriteLog(
                            "Lỗi khi giải phóng chuột/bàn phím: "
                            + ex.Message);
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
            }
        }

        private void WriteLog(string message)
        {
            Log?.Invoke(message);
        }
        private void ReleaseInputState()
        {
            var errors = new List<Exception>();

            try
            {
                _mouseController.ReleaseAll();
            }
            catch (Exception ex)
            {
                errors.Add(ex);
            }

            try
            {
                _keyboardController.ReleaseAll();
            }
            catch (Exception ex)
            {
                errors.Add(ex);
            }

            if (errors.Count > 0)
            {
                throw new AggregateException(
                    "Không thể giải phóng hết trạng thái điều khiển.",
                    errors);
            }
        }
    }
}
