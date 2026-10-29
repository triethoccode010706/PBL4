namespace Shared.Models
{
    public sealed class MachineInfoDto
    {
        public string MachineName { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string IpAddress { get; set; } = string.Empty;
        public string OsDescription { get; set; } = string.Empty;
        public string CpuInfo { get; set; } = string.Empty;
        public string RamInfo { get; set; } = string.Empty;
    }
}
