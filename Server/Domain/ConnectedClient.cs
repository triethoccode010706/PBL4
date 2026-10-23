namespace Server.Domain
{
    public sealed class ConnectedClient
    {
        public string SessionId { get; }
        public string RemoteEndPoint { get; }
        public DateTime ConnectedAt { get; }

        public string MachineName { get; private set; }
        public ClientStatus Status { get; private set; }

        public DateTime? LastHeartbeatAt { get; private set; }
        private readonly object _stateLock = new();
        public ConnectedClient(
            string sessionId,
            string remoteEndPoint)
        {
            SessionId = sessionId;
            RemoteEndPoint = remoteEndPoint;

            ConnectedAt = DateTime.UtcNow;
            MachineName = "Chưa có tên";
            Status = ClientStatus.Connected;
            LastHeartbeatAt = null;
        }

        public void UpdateMachineName(string machineName)
        {
            if (string.IsNullOrWhiteSpace(machineName))
            {
                return;
            }

            MachineName = machineName;
        }
        public void UpdateHeartbeat(DateTime heartbeatAt)
        {
            lock (_stateLock)
            {
                if (Status != ClientStatus.Connected)
                {
                    return;
                }

                LastHeartbeatAt = heartbeatAt;
            }
        }
        public bool TryMarkTimedOut( DateTime now,TimeSpan timeout)
        {
            lock (_stateLock)
            {
                if (Status != ClientStatus.Connected)
                {
                    return false;
                }

                DateTime lastSeen = LastHeartbeatAt ?? ConnectedAt;

                if (now - lastSeen < timeout)
                {
                    return false;
                }

                Status = ClientStatus.TimedOut;
                return true;
            }
        }
        public override string ToString()
        {
            string heartbeatText = "Chưa có";

            if (LastHeartbeatAt.HasValue)
            {
                heartbeatText = LastHeartbeatAt.Value
                    .ToLocalTime()
                    .ToString("HH:mm:ss");
            }

            return $"{MachineName} | {RemoteEndPoint} "
                + $"| {Status} | Heartbeat: {heartbeatText}";
        }
    }
}