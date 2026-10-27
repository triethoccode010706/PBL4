
namespace Server.Domain
{
    public sealed class ConnectedClient
    {
        public string SessionId { get; }
        public string RemoteEndPoint { get; }
        public DateTime ConnectedAt { get; }

        public string MachineName { get; private set; }
        public string UserName { get; private set; } = string.Empty;
        public string IpAddress { get; private set; } = string.Empty;
        public string OsDescription { get; private set; } = string.Empty;
        public string CpuInfo { get; private set; } = string.Empty;
        public string RamInfo { get; private set; } = string.Empty;

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

        public void UpdateSystemInfo(
            string machineName,
            string userName,
            string ipAddress,
            string osDescription,
            string cpuInfo,
            string ramInfo)
        {
            lock (_stateLock)
            {
                if (!string.IsNullOrWhiteSpace(machineName))
                    MachineName = machineName;

                UserName = userName ?? string.Empty;
                IpAddress = ipAddress ?? string.Empty;
                OsDescription = osDescription ?? string.Empty;
                CpuInfo = cpuInfo ?? string.Empty;
                RamInfo = ramInfo ?? string.Empty;
            }
        }

        public bool UpdateHeartbeat(DateTime heartbeatAt)
        {
            lock (_stateLock)
            {
                if (Status != ClientStatus.Connected)
                    return false;

                LastHeartbeatAt = heartbeatAt;
                return true;
            }
        }

        public bool TryMarkTimedOut(DateTime now, TimeSpan timeout)
        {
            lock (_stateLock)
            {
                if (Status != ClientStatus.Connected)
                    return false;

                DateTime lastSeen = LastHeartbeatAt ?? ConnectedAt;

                if (now - lastSeen < timeout)
                    return false;

                Status = ClientStatus.TimedOut;
                return true;
            }
        }

        public override string ToString()
        {
            return $"Tên máy: {MachineName} | "
                + $"Người dùng: {UserName} | "
                + $"IP: {IpAddress} | "
                + $"Hệ điều hành: {OsDescription} | "
                + $"CPU: {CpuInfo} | "
                + $"RAM: {RamInfo}";
        }
    }
}
