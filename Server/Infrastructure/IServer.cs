using Shared.Models;

namespace Server.Infrastructure
{
    public interface IServer
    {
        event Action<string, Command>? CommandReceived;
        event Action<string, Response>? ResponseSent;
        Task StartAsync(CancellationToken cancellationToken = default);
        void Stop();
    }
}
