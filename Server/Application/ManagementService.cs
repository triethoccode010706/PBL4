using Server.Domain;
using Server.Infrastructure;
using Shared.Models;

namespace Server.Application
{
    public sealed class ManagementService : IManagementService
    {
        private readonly IServer _server;
        private readonly CancellationTokenSource _cts = new();
        private int _stopped;

        public event Action<ConnectedClient>? ClientConnected;
        public event Action<string>? ClientDisconnected;
        public event Action<string, Command>? CommandSent;
        public event Action<string, Response>? ResponseReceived;
        public event Action<string>? ErrorOccurred;

        public ManagementService(IServer server, IConnectionManager connectionManager)
        {
            _server = server;

            connectionManager.SessionAdded += session =>
                ClientConnected?.Invoke(new ConnectedClient(session.SessionId, session.RemoteEndPoint));

            connectionManager.SessionRemoved += sessionId =>
                ClientDisconnected?.Invoke(sessionId);

            _server.CommandSent += (sessionId, command) =>
                CommandSent?.Invoke(sessionId, command);

            _server.ResponseReceived += (sessionId, response) =>
                ResponseReceived?.Invoke(sessionId, response);
        }

        public void Start()
        {
            _ = Task.Run(async () =>
            {
                try
                {
                    await _server.StartAsync(_cts.Token);
                }
                catch (Exception ex)
                {
                    ErrorOccurred?.Invoke(ex.Message);
                }
            });
        }

        public void Stop()
        {
            if (Interlocked.Exchange(ref _stopped, 1) == 1)
            {
                return;
            }

            try
            {
                _cts.Cancel();
            }
            catch
            {
            }

            _server.Stop();
            _cts.Dispose();
        }

        public Task<Response> SendCommandAsync(
            string sessionId,
            CommandType commandType,
            CancellationToken cancellationToken = default)
        {
            return _server.SendCommandAsync(
                sessionId,
                Command.Create(commandType),
                cancellationToken);
        }
    }
}
