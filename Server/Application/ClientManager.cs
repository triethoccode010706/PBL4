using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Collections.Concurrent;
using Server.Domain;

namespace Server.Application
{
    public sealed class ClientManager : IClientManager
    {
        private readonly ConcurrentDictionary<string, ConnectedClient>
            _clients = new();

        public bool AddClient(ConnectedClient client)
        {
            return _clients.TryAdd(client.SessionId, client);
        }

        public bool RemoveClient(string sessionId)
        {
            return _clients.TryRemove(sessionId, out _);
        }

        public ConnectedClient? GetClient(string sessionId)
        {
            _clients.TryGetValue(sessionId, out var client);

            return client;
        }

        public IReadOnlyList<ConnectedClient> GetClients()
        {
            return _clients.Values.ToList();
        }
        public bool UpdateMachineName( string sessionId,string machineName)
        {
            if (string.IsNullOrWhiteSpace(machineName))
            {
                return false;
            }

            ConnectedClient? client = GetClient(sessionId);

            if (client == null)
            {
                return false;
            }

            client.UpdateMachineName(machineName);

            return true;
        }
        public bool UpdateHeartbeat(string sessionId,DateTime heartbeatAt)
        {
            ConnectedClient? client = GetClient(sessionId);

            if (client == null)
            {
                return false;
            }

            bool updated = client.UpdateHeartbeat(heartbeatAt);

            if (!updated)
            {
                return false;
            }

            ClientUpdated?.Invoke(client);

            return true;
        }
        public IReadOnlyList<ConnectedClient> GetTimedOutClients( TimeSpan timeout,DateTime now)
        {
            var timedOutClients = new List<ConnectedClient>();

            foreach (ConnectedClient client in GetClients())
            {
                bool timedOut = client.TryMarkTimedOut(now, timeout);

                if (timedOut)
                {
                    timedOutClients.Add(client);
                }
            }

            return timedOutClients;
        }
        public event Action<ConnectedClient>? ClientUpdated;
    }
}
