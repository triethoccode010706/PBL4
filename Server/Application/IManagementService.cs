using Server.Domain;
using Shared.Models;

namespace Server.Application
{
    public interface IManagementService
    {
        event Action<ConnectedClient>? ClientConnected;
        event Action<string>? ClientDisconnected;
        event Action<string, Command>? CommandSent;
        event Action<string, Response>? ResponseReceived;
        event Action<string>? ErrorOccurred;

        void Start();
        void Stop();

        Task<Response> SendCommandAsync(
            string sessionId,
            CommandType commandType,
            CancellationToken cancellationToken = default);
    }
}
