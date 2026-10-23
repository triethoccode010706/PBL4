using Server.Domain;
using Server.Infrastructure;
using Shared.Models;
using System.Text.Json;
namespace Server.Application
{
    public sealed class ManagementService : IManagementService
    {
        private readonly IServer _server;
        private readonly IConnectionManager _connectionManager;
        private readonly CancellationTokenSource _cts = new();
        private int _stopped;
        private readonly IHeartbeatService _heartbeatService;
        private Task? _runTask;
        private readonly IClientManager _clientManager;
        private readonly object _initialInfoLock = new();
        private bool _isStopping;

        private readonly List<Task> _initialInfoTasks = new();
        public event Action<ConnectedClient>? ClientConnected; 
        public event Action<ConnectedClient>? ClientUpdated;
        public event Action<string>? ClientDisconnected;
        public event Action<string, Command>? CommandSent;
        public event Action<string, Response>? ResponseReceived;
        public event Action<string>? ErrorOccurred;
        public event Action? ServerStarted;
        public event Action<string>? ServerStartFailed;

        public ManagementService(IServer server,IConnectionManager connectionManager,IClientManager clientManager,IHeartbeatService heartbeatService)
        {
            _server = server;
            _clientManager = clientManager;
            _heartbeatService = heartbeatService;
            _connectionManager = connectionManager;
            connectionManager.SessionAdded += session =>
            {
                var client = new ConnectedClient(
                    session.SessionId,
                    session.RemoteEndPoint);

                bool added = _clientManager.AddClient(client);

                if (!added)
                {
                    return;
                }

                ClientConnected?.Invoke(client);

                lock (_initialInfoLock)
                {
                    if (_isStopping)
                    {
                        return;
                    }

                    _initialInfoTasks.RemoveAll(task => task.IsCompleted);

                    Task task = Task.Run(() =>
                        LoadInitialInfoAsync(session.SessionId));

                    _initialInfoTasks.Add(task);
                }
            };

            connectionManager.SessionRemoved += sessionId =>
            {
                bool removed = _clientManager.RemoveClient(sessionId);

                if (removed)
                {
                    ClientDisconnected?.Invoke(sessionId);
                }
            };

            _server.CommandSent += (sessionId, command) =>
            {
                CommandSent?.Invoke(sessionId, command);
            };

            _server.ResponseReceived += (sessionId, response) =>
            {
                ResponseReceived?.Invoke(sessionId, response);
            };
            _clientManager.ClientUpdated += client =>
            {
                ClientUpdated?.Invoke(client);
            };
        }
        public void Start()
        {
            if (_runTask != null)
            {
                return;
            }

            _runTask = Task.Run(RunAsync);
        }

        public void Stop()
        {
            if (Interlocked.Exchange(ref _stopped, 1) == 1)
            {
                return;
            }

            try
            {
                lock (_initialInfoLock)
                {
                    _isStopping = true;
                    
                }
                _cts.Cancel();
            }
            catch (ObjectDisposedException)
            {
                // Tác vụ đã kết thúc và giải phóng token source.
            }

            _server.Stop();
        }

        public async Task StopAsync()
        {
            Stop();

            if (_runTask != null)
            {
                await _runTask;
            }
        }
        private async Task RunAsync()
        {
            Task? serverTask = null;
            Task? heartbeatTask = null;
            Task? timeoutTask = null;
            bool serverStarted = false;
            try
            {
                serverTask = _server.StartAsync(_cts.Token);

                // Với TcpServer hiện tại, lỗi mở listener được
                // trả về trong task trước khi vòng accept chờ.
                if (serverTask.IsCompleted)
                {
                    await serverTask;
                    return;
                }
                serverStarted = true;
                ServerStarted?.Invoke();

                heartbeatTask =
                    _heartbeatService.RunAsync(_cts.Token);
                timeoutTask = CheckTimeoutsAsync(_cts.Token);

                // Theo dõi cả hai: nếu một tác vụ kết thúc,
                // bắt đầu dừng toàn bộ.
                Task completedTask = await Task.WhenAny(
                    serverTask,
                    heartbeatTask,
                    timeoutTask);

                await completedTask;
            }
            catch (OperationCanceledException)
                when (_cts.IsCancellationRequested)
            {
                // Dừng theo yêu cầu.
            }
            catch (Exception ex)
            {
                if (!serverStarted)
                {
                    ServerStartFailed?.Invoke(ex.Message);
                }
                else
                {
                    ErrorOccurred?.Invoke(ex.Message);
                }
            }
            finally
            {
                lock (_initialInfoLock)
                {
                    _isStopping = true;
                }

                _cts.Cancel();
                _server.Stop();

                try
                {
                    if (serverTask != null)
                    {
                        await serverTask;
                    }
                }

                catch (Exception)
                {
                    // Lỗi đã được quan sát; vẫn tiếp tục dọn heartbeat.
                }
                try
                {
                    await _server.WaitForSessionsAsync();
                }
                catch (Exception ex)
                {
                    Console.WriteLine(
                        "Lỗi khi chờ các session kết thúc: " + ex.Message);
                }

                try
                {
                    if (heartbeatTask != null)
                    {
                        await heartbeatTask;
                    }
                }
                catch (Exception)
                {
                    // Vẫn hoàn tất dọn tài nguyên.
                }
                try
                {
                    if (timeoutTask != null)
                    {
                        await timeoutTask;
                    }
                }
                catch (Exception)
                {
                    // Tiếp tục hoàn tất dọn tài nguyên.
                }
                Task[] initialInfoTasks;

                lock (_initialInfoLock)
                {
                    initialInfoTasks = _initialInfoTasks.ToArray();
                }

                await Task.WhenAll(initialInfoTasks);
                _cts.Dispose();
            }
        }
        private void UpdateClientInfo(string sessionId, string json)
        {
            MachineInfoDto? info;

            try
            {
                info = JsonSerializer.Deserialize<MachineInfoDto>(json);
            }
            catch (JsonException)
            {
                ErrorOccurred?.Invoke(
                    "GET_INFO trả về dữ liệu JSON không hợp lệ.");

                return;
            }

            if (info == null ||
                string.IsNullOrWhiteSpace(info.MachineName))
            {
                ErrorOccurred?.Invoke(
                    "GET_INFO thiếu tên máy.");

                return;
            }

            bool updated = _clientManager.UpdateMachineName(
                sessionId,
                info.MachineName);

            if (!updated)
            {
                return;
            }

            ConnectedClient? client =
                _clientManager.GetClient(sessionId);

            if (client != null)
            {
                ClientUpdated?.Invoke(client);
            }
        }
        public async Task<Response> SendCommandAsync( string sessionId,CommandType commandType,CancellationToken cancellationToken = default)
        {
            Command command = Command.Create(commandType);

            Response response = await _server.SendCommandAsync(
                sessionId,
                command,
                cancellationToken);

            if (commandType == CommandType.GET_INFO && response.Success)
            {
                UpdateClientInfo(sessionId, response.Data);
            }
            if (commandType == CommandType.PING && response.Success)
            {
                _clientManager.UpdateHeartbeat(
                    sessionId,
                    DateTime.UtcNow);
            }

            return response;
        }

        private async Task CheckTimeoutsAsync(CancellationToken cancellationToken)
        {
            using var timer =
                new PeriodicTimer(TimeSpan.FromSeconds(1));

            TimeSpan timeout = TimeSpan.FromSeconds(15);

            try
            {
                while (await timer.WaitForNextTickAsync(
                    cancellationToken))
                {
                    var clients = _clientManager.GetTimedOutClients(
                        timeout,
                        DateTime.UtcNow);

                    foreach (ConnectedClient client in clients)
                    {
                        try
                        {
                            ErrorOccurred?.Invoke(
                                $"Client {client.SessionId} mất phản hồi "
                                + "heartbeat quá 15 giây.");
                        }
                        finally
                        {
                            _connectionManager.RemoveSession(
                                client.SessionId);
                        }
                    }
                }
            }
            catch (OperationCanceledException)
                when (cancellationToken.IsCancellationRequested)
            {
                // Dừng kiểm tra khi Server dừng.
            }
        }
        private async Task LoadInitialInfoAsync(string sessionId)
        {
            try
            {
                await SendCommandAsync(
                    sessionId,
                    CommandType.GET_INFO,
                    _cts.Token);
            }
            catch (OperationCanceledException)
            {
                // Server đang dừng hoặc phiên đã kết thúc.
            }
            catch (Exception ex)
            {
                ErrorOccurred?.Invoke(
                    $"Không lấy được thông tin Client {sessionId}: "
                    + ex.Message);
            }
        }
    }
}
