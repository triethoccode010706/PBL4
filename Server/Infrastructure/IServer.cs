using Shared.Models;

namespace Server.Infrastructure
{
    public interface IServer
    {
        event Action<string, Command>? CommandSent;
        event Action<string, Response>? ResponseReceived;
        event Action<string, Command>? CommandReceived;
        event Action<string, Response>? ResponseSent;
        Task WaitForSessionsAsync();
        Task<Response> SendCommandAsync(
            string sessionId,
            Command command,
            CancellationToken cancellationToken = default);

        Task StartAsync(CancellationToken cancellationToken = default);
        void Stop();
    }
}
