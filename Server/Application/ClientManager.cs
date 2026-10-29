using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Server.Domain;

namespace Server.Application
{
    public sealed class ClientManager : IClientManager
    {
        private readonly ConcurrentDictionary<string, ConnectedClient>
            _clients = new();

        public event Action<ConnectedClient>? ClientUpdated;

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

        public bool UpdateMachineName(
            string sessionId,
            string machineName)
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
            ClientUpdated?.Invoke(client);

            return true;
        }

        public bool UpdateSystemInfo(
            string sessionId,
            string machineName,
            string userName,
            string ipAddress,
            string osDescription,
            string cpuInfo,
            string ramInfo)
        {
            ConnectedClient? client = GetClient(sessionId);

            if (client == null)
            {
                return false;
            }

            client.UpdateSystemInfo(
                machineName,
                userName,
                ipAddress,
                osDescription,
                cpuInfo,
                ramInfo);

            ClientUpdated?.Invoke(client);

            return true;
        }

        public bool UpdateHeartbeat(
            string sessionId,
            DateTime heartbeatAt)
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

        public IReadOnlyList<ConnectedClient> GetTimedOutClients(
            TimeSpan timeout,
            DateTime now)
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
    }
}
