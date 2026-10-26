using Shared.Models;
using Shared.Network;

namespace Server.Infrastructure
{
    public interface IClientSession : IDataSender, IDataReceiver
    {
        string SessionId { get; }
        bool IsConnected { get; }
        string RemoteEndPoint { get; }
        AuthenticationStatus AuthStatus { get; }

        void MarkAuthenticated();
        Task<Response> SendCommandAsync(
            Command command,
            CancellationToken cancellationToken = default);

        void Disconnect();
    }
}
