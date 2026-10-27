using System.Net;
using System.Net.Sockets;
using Shared.Models;
using Shared.Protocol;

namespace Server.Infrastructure
{
    public class TcpServer : IServer
    {
        private readonly int _port;
        private readonly IConnectionManager _connectionManager;
        private readonly IMessageFramer _framer;
        private readonly IMessageSerializer _serializer;
        private readonly IMessageValidator _validator;
        private readonly Func<CommandDispatcher> _dispatcherFactory;
        private readonly object _sync = new();
        private CancellationTokenSource? _runCts;
        private TcpListener? _listener;
        private volatile bool _isRunning;
        private readonly List<Task> _sessionTasks = new();
        public event Action<string, Command>? CommandSent;
        public event Action<string, Response>? ResponseReceived;
        public event Action<string, Command>? CommandReceived;
        public event Action<string, Response>? ResponseSent;
        public event Action<string, ScreenFrame>? ScreenFrameReceived;
        public TcpServer(
            int port,
            IConnectionManager connectionManager,
            IMessageFramer? framer = null,
            IMessageSerializer? serializer = null,
            IMessageValidator? validator = null,
            Func<CommandDispatcher>? dispatcherFactory = null)
        {
            _port = port;
            _connectionManager = connectionManager;
            _framer = framer ?? new MessageFramer();
            _serializer = serializer ?? new MessageSerializer();
            _validator = validator ?? new MessageValidator();
            _dispatcherFactory = dispatcherFactory ?? ProtocolServices.CreateServerDispatcher;
        }

        public async Task StartAsync(CancellationToken cancellationToken = default)
        {
            lock (_sync)
            {
                if (_isRunning)
                {
                    throw new InvalidOperationException("TcpServer đang chạy.");
                }

                _runCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                _listener = new TcpListener(IPAddress.Any, _port);
                _listener.Start();
                _isRunning = true;
            }

            CancellationToken runToken = _runCts.Token;
            Console.WriteLine($"[TcpServer] Đang lắng nghe cổng {_port}.");

            try
            {
                while (_isRunning && !runToken.IsCancellationRequested)
                {
                    try
                    {
                        TcpClient client = await _listener.AcceptTcpClientAsync(runToken);
                        Task sessionTask = HandleAcceptedClientAsync(client, runToken);

                        lock (_sync)
                        {
                            _sessionTasks.RemoveAll(task => task.IsCompleted);

                            _sessionTasks.Add(sessionTask);
                        }
                    }
                    catch (OperationCanceledException) when (!_isRunning || runToken.IsCancellationRequested)
                    {
                        break;
                    }
                    catch (ObjectDisposedException) when (!_isRunning || runToken.IsCancellationRequested)
                    {
                        break;
                    }
                    catch (SocketException) when (!_isRunning || runToken.IsCancellationRequested)
                    {
                        break;
                    }
                    catch (Exception ex)
                    {
                        if (_isRunning && !runToken.IsCancellationRequested)
                        {
                            Console.WriteLine($"[TcpServer] Lỗi chấp nhận client: {ex.Message}");
                        }
                        else
                        {
                            Console.WriteLine("[TcpServer] Server đã dừng.");
                            break;
                        }
                    }
                }
            }
            finally
            {
                Console.WriteLine("[TcpServer] Vòng Accept đã kết thúc.");
                Stop();
            }
        }

        public void Stop()
        {
            lock (_sync)
            {
                _isRunning = false;

                try
                {
                    _runCts?.Cancel();
                }
                catch
                {
                }

                try
                {
                    _listener?.Stop();
                }
                catch
                {
                }

                _listener = null;
            }

            _connectionManager.DisconnectAll();
            Console.WriteLine("[TcpServer] Đã dừng và giải phóng tài nguyên.");
        }

        public async Task<Response> SendCommandAsync(
            string sessionId,
            Command command,
            CancellationToken cancellationToken = default)
        {
            IClientSession? session = _connectionManager.GetSession(sessionId);
            if (session == null || !session.IsConnected)
            {
                throw new InvalidOperationException($"Không tìm thấy client {sessionId}.");
            }

            return await session.SendCommandAsync(command, cancellationToken);
        }

        private async Task HandleAcceptedClientAsync(TcpClient client, CancellationToken cancellationToken)
        {
            ClientSession? session = null;
            try
            {
                session = new ClientSession(client);
                ProtocolSession protocol = CreateProtocol(session);
                session.AttachProtocol(protocol);
                _connectionManager.AddSession(session);
                await protocol.RunAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[TcpServer] Lỗi phiên: {ex.Message}");
            }
            finally
            {
                if (session != null)
                {
                    _connectionManager.RemoveSession(session.SessionId);
                }
                else
                {
                    try
                    {
                        client.Close();
                    }
                    catch
                    {
                    }
                }
            }
        }

        private ProtocolSession CreateProtocol(ClientSession session)
        {
            var protocol = new ProtocolSession(
                session,
                session.Stream,
                _dispatcherFactory(),
                _framer,
                _serializer,
                _validator);

            protocol.CommandSent += command => CommandSent?.Invoke(session.SessionId, command);
            protocol.ResponseReceived += response => ResponseReceived?.Invoke(session.SessionId, response);
            protocol.CommandReceived += command => CommandReceived?.Invoke(session.SessionId, command);
            protocol.ResponseSent += response => ResponseSent?.Invoke(session.SessionId, response);
            protocol.ScreenFrameReceived += frame =>
            {
                ScreenFrameReceived?.Invoke(
                    session.SessionId,
                    frame);
            };
            return protocol;
        }
        public async Task WaitForSessionsAsync()
        {
            Task[] tasks;

            lock (_sync)
            {
                tasks = _sessionTasks.ToArray();
            }

            try
            {
                await Task.WhenAll(tasks);
            }
            finally
            {
                lock (_sync)
                {
                    _sessionTasks.RemoveAll(task => task.IsCompleted);
                }
            }
        }
    }
}
