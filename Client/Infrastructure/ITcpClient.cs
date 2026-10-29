using Shared.Network;

namespace Client.Infrastructure
{
    public interface ITcpClient : IDataSender, IDataReceiver
    {
        bool IsConnected { get; }
        Task<bool> ConnectAsync(string serverIp, int port, CancellationToken cancellationToken = default);
        void Disconnect();
    }
}
