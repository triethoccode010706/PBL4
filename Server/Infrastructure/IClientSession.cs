using Shared.Network;

namespace Server.Infrastructure
{
    public interface IClientSession : IDataSender, IDataReceiver
    {
        string SessionId { get; }
        bool IsConnected { get; }
        string RemoteEndPoint { get; }
        void Disconnect();
    }
}
