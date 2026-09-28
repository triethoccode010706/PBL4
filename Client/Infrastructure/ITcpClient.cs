using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Client.Infrastructure
{
    public interface ITcpClient
    {
        bool IsConnected { get; }
        Task<bool> ConnectAsync(string serverIp, int port);
        Task SendAsync(byte[] data);
        Task StartReceivingAsync(Action<byte[]>onDataReceived);
        void Disconnect();
    }
}
