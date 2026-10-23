using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Server.Domain;

namespace Server.Application
{
    public interface IClientManager
    {
        bool AddClient(ConnectedClient client);

        bool RemoveClient(string sessionId);

        ConnectedClient? GetClient(string sessionId);

        IReadOnlyList<ConnectedClient> GetClients();
        bool UpdateMachineName(string sessionId, string machineName);
        bool UpdateHeartbeat(string sessionId, DateTime heartbeatAt);
        IReadOnlyList<ConnectedClient> GetTimedOutClients(TimeSpan timeout,DateTime now);
        event Action<ConnectedClient>? ClientUpdated;
    }
}