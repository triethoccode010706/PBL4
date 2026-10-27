using System;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using Microsoft.Win32;

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
                IpAddress = GetIpAddress(),
                OsDescription = GetWindowsVersion(),
                CpuInfo = GetCpuInfo(),
                RamInfo = GetRamInfo()
            };
        }

        private static string GetIpAddress()
        {
            try
            {
                var addresses = NetworkInterface
                    .GetAllNetworkInterfaces()
                    .Where(n =>
                        n.OperationalStatus == OperationalStatus.Up &&
                        n.NetworkInterfaceType != NetworkInterfaceType.Loopback &&
                        n.NetworkInterfaceType != NetworkInterfaceType.Tunnel)
                    .SelectMany(n => n.GetIPProperties().UnicastAddresses)
                    .Select(a => a.Address)
                    .Where(ip =>
                        ip.AddressFamily == AddressFamily.InterNetwork &&
                        !IPAddress.IsLoopback(ip));

                return addresses.FirstOrDefault()?.ToString()
                    ?? "Không tìm thấy IPv4";
            }
            catch
            {
                return "Không lấy được IP";
            }
        }

        private static string GetWindowsVersion()
        {
            try
            {
                using RegistryKey? key = Registry.LocalMachine.OpenSubKey(
                    @"SOFTWARE\Microsoft\Windows NT\CurrentVersion");

                if (key != null)
                {
                    string? productName =
                        key.GetValue("ProductName")?.ToString();

                    string? displayVersion =
                        key.GetValue("DisplayVersion")?.ToString();

                    string? buildNumber =
                        key.GetValue("CurrentBuildNumber")?.ToString();

                    string result = string.Join(
                        " ",
                        new[] { productName, displayVersion }
                            .Where(s => !string.IsNullOrWhiteSpace(s)));

                    if (!string.IsNullOrWhiteSpace(buildNumber))
                    {
                        result += $" (Build {buildNumber})";
                    }

                    if (!string.IsNullOrWhiteSpace(result))
                    {
                        return result;
                    }
                }
            }
            catch
            {
                // Dùng thông tin dự phòng nếu không đọc được Registry.
            }

            return RuntimeInformation.OSDescription;
        }

        private static string GetCpuInfo()
        {
            try
            {
                using RegistryKey? key = Registry.LocalMachine.OpenSubKey(
                    @"HARDWARE\DESCRIPTION\System\CentralProcessor\0");

                string? name = key?.GetValue("ProcessorNameString")?.ToString();

                int logicalProcessors = Environment.ProcessorCount;

                if (!string.IsNullOrWhiteSpace(name))
                {
                    return $"{name.Trim()} ({logicalProcessors} logical processors)";
                }

                return $"{logicalProcessors} logical processors";
            }
            catch
            {
                return $"{Environment.ProcessorCount} logical processors";
            }
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        private class MemoryStatus
        {
            public uint Length;
            public uint MemoryLoad;
            public ulong TotalPhysical;
            public ulong AvailablePhysical;
            public ulong TotalPageFile;
            public ulong AvailablePageFile;
            public ulong TotalVirtual;
            public ulong AvailableVirtual;
            public ulong AvailableExtendedVirtual;

            public MemoryStatus()
            {
                Length = (uint)Marshal.SizeOf(typeof(MemoryStatus));
            }
        }

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GlobalMemoryStatusEx(
            [In, Out] MemoryStatus buffer);

        private static string GetRamInfo()
        {
            try
            {
                var status = new MemoryStatus();

                if (GlobalMemoryStatusEx(status))
                {
                    double totalGb = status.TotalPhysical /
                                     (1024.0 * 1024 * 1024);

                    double availableGb = status.AvailablePhysical /
                                         (1024.0 * 1024 * 1024);

                    double usedGb = totalGb - availableGb;

                    return $"{usedGb:F2} GB đang sử dụng / {totalGb:F2} GB tổng";
                }

                return "Không lấy được thông tin RAM";
            }
            catch
            {
                return "Không lấy được thông tin RAM";
            }
        }
    }
}
