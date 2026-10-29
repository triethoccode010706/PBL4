using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server.Domain
{
    public sealed class ClientSystemInfo
    {
        public string MachineName { get; init; } = string.Empty;
        public string UserName { get; init; } = string.Empty;
        public string IpAddress { get; init; } = string.Empty;
        public string OsDescription { get; init; } = string.Empty;
        public string CpuInfo { get; init; } = string.Empty;
        public string RamInfo { get; init; } = string.Empty;
    }
}
