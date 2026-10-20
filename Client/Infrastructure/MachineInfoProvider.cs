using System.Runtime.InteropServices;
using Client.Domain;

namespace Client.Infrastructure
{
    public sealed class MachineInfoProvider : IMachineInfoProvider
    {
        public MachineInfo GetMachineInfo()
        {
            return new MachineInfo
            {
                MachineName = Environment.MachineName,
                UserName = Environment.UserName,
                OsDescription = RuntimeInformation.OSDescription
            };
        }
    }
}
