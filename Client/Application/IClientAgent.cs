namespace Client.Application
{
    public interface IClientAgent
    {
        event Action<string>? Log;

        Task RunAsync(
            string serverIp,
            int port,
            CancellationToken cancellationToken = default);
    }
}
