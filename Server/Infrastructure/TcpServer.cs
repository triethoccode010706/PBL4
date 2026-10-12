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
        private TcpListener? _listener;
        private volatile bool _isRunning;

        public event Action<string, Command>? CommandReceived;
        public event Action<string, Response>? ResponseSent;

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
            _listener = new TcpListener(IPAddress.Any, _port);
            _listener.Start();
            _isRunning = true;
            Console.WriteLine($"[TcpServer] Đang lắng nghe cổng {_port}.");

            try
            {
                while (_isRunning && !cancellationToken.IsCancellationRequested)
                {
                    try
                    {
                        TcpClient client = await _listener.AcceptTcpClientAsync(cancellationToken);
                        var session = new ClientSession(client);
                        _connectionManager.AddSession(session);
                        _ = Task.Run(() => HandleSessionAsync(session, cancellationToken));
                    }
                    catch (OperationCanceledException) when (!_isRunning || cancellationToken.IsCancellationRequested)
                    {
                        break;
                    }
                    catch (ObjectDisposedException) when (!_isRunning)
                    {
                        break;
                    }
                    catch (SocketException) when (!_isRunning)
                    {
                        break;
                    }
                    catch (Exception ex)
                    {
                        if (_isRunning)
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
            }
        }

        public void Stop()
        {
            _isRunning = false;

            try
            {
                _listener?.Stop();
            }
            catch
            {
            }

            _connectionManager.DisconnectAll();
            Console.WriteLine("[TcpServer] Đã dừng và giải phóng tài nguyên.");
        }

        private async Task HandleSessionAsync(IClientSession session, CancellationToken cancellationToken)
        {
            var protocol = new ProtocolSession(
                session,
                session.Stream,
                _dispatcherFactory(),
                _framer,
                _serializer,
                _validator);

            protocol.CommandReceived += command => CommandReceived?.Invoke(session.SessionId, command);
            protocol.ResponseSent += response => ResponseSent?.Invoke(session.SessionId, response);

            try
            {
                await protocol.RunAsync(cancellationToken);
            }
            finally
            {
                _connectionManager.RemoveSession(session.SessionId);
            }
        }
    }
}
