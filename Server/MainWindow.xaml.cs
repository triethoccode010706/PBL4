using System.Windows;
using Server.Infrastructure;
using Shared.Models;
using Shared.Protocol;

namespace Server
{
    public partial class MainWindow : Window
    {
        private readonly ConnectionManager _connectionManager = new();
        private readonly CancellationTokenSource _serverCts = new();
        private IServer? _server;

        public MainWindow()
        {
            InitializeComponent();
            Closed += OnClosed;
            StartTcpServer();
        }

        private void StartTcpServer()
        {
            _connectionManager.SessionAdded += session =>
            {
                Dispatcher.Invoke(() =>
                {
                    ClientList.Items.Add($"{session.SessionId} ({session.RemoteEndPoint})");
                    AppendLog($"Client kết nối: {session.SessionId} từ {session.RemoteEndPoint}");
                });
            };

            _connectionManager.SessionRemoved += sessionId =>
            {
                Dispatcher.Invoke(() =>
                {
                    object? item = ClientList.Items
                        .Cast<object>()
                        .FirstOrDefault(x => x.ToString()?.StartsWith(sessionId, StringComparison.Ordinal) == true);
                    if (item != null)
                    {
                        ClientList.Items.Remove(item);
                    }

                    AppendLog($"Client ngắt kết nối: {sessionId}");
                });
            };

            var server = new TcpServer(
                5000,
                _connectionManager,
                dispatcherFactory: ProtocolServices.CreateServerDispatcher);

            server.CommandReceived += OnCommandReceived;
            server.ResponseSent += OnResponseSent;
            _server = server;

            StatusText.Text = "Server đang lắng nghe cổng 5000";
            AppendLog("Module mạng đã khởi động. Protocol: Command/Response + length-prefix.");

            _ = Task.Run(async () =>
            {
                try
                {
                    await _server.StartAsync(_serverCts.Token);
                }
                catch (Exception ex)
                {
                    Dispatcher.Invoke(() => AppendLog($"Lỗi server: {ex.Message}"));
                }
            });
        }

        private void OnCommandReceived(string sessionId, Command command)
        {
            Dispatcher.Invoke(() =>
            {
                AppendLog($"← {sessionId}: {command.Type} (id={command.RequestId})");
            });
        }

        private void OnResponseSent(string sessionId, Response response)
        {
            Dispatcher.Invoke(() =>
            {
                string status = response.Success ? "OK" : $"LỖI {response.ErrorCode}";
                AppendLog($"→ {sessionId}: {status} {response.Message}");
            });
        }

        private void AppendLog(string line)
        {
            LogBox.AppendText($"[{DateTime.Now:HH:mm:ss}] {line}{Environment.NewLine}");
            LogBox.ScrollToEnd();
        }

        private void OnClosed(object? sender, EventArgs e)
        {
            try
            {
                _serverCts.Cancel();
            }
            catch
            {
            }

            _server?.Stop();
            _serverCts.Dispose();
        }
    }
}
