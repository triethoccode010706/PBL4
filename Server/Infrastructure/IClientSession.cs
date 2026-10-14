using Shared.Models;
using Shared.Network;

namespace Server.Infrastructure
{
    public interface IClientSession : IDataSender, IDataReceiver
    {
        string SessionId { get; }
        bool IsConnected { get; }
        string RemoteEndPoint { get; }

        Task<Response> SendCommandAsync(
            Command command,
            CancellationToken cancellationToken = default);

        void Disconnect();
    }
}
