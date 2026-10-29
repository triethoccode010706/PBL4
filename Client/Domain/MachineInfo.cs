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
    }
}
