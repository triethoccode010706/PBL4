namespace Client.Domain
{
    public sealed class MachineInfo
    {
        public string MachineName { get; init; } = string.Empty;
        public string UserName { get; init; } = string.Empty;
        public string OsDescription { get; init; } = string.Empty;

        public override string ToString()
        {
            return $"{MachineName}|{UserName}|{OsDescription}";
        }
    }
}
