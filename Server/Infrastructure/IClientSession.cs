using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server.Infrastructure
{
    public interface IClientSession
    {
        string SessionId { get; }
        bool IsConnected { get; }
        Task StartReceivingAsync(Action<string, string> onMessageReceived);
        void Disconnect();
        Task SendAsync(string message);
    }
}
