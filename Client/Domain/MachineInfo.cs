namespace Client.Domain
{
    public sealed class MachineInfo
    {
        public string MachineName { get; init; } = string.Empty;
        public string UserName { get; init; } = string.Empty;
        public string IpAddress { get; init; } = string.Empty;
        public string OsDescription { get; init; } = string.Empty;
        public string CpuInfo { get; init; } = string.Empty;
        public string RamInfo { get; init; } = string.Empty;

        public override string ToString()
        {
            return
                $"MachineName: {MachineName}\n" +
                $"UserName: {UserName}\n" +
                $"IP: {IpAddress}\n" +
                $"Windows: {OsDescription}\n" +
                $"CPU: {CpuInfo}\n" +
                $"RAM: {RamInfo}";
        }
    }
}
