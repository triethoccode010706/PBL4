using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Server.Infrastructure;
using Shared.Models;

namespace Server.Application
{
    public sealed class HeartbeatService : IHeartbeatService
    {
        private readonly IConnectionManager _connections;
        private readonly IClientManager _clients;

        public HeartbeatService(
            IConnectionManager connections,
            IClientManager clients)
        {
            _connections = connections;
            _clients = clients;
        }

        public async Task RunAsync(
            CancellationToken cancellationToken)
        {
            using var timer =
                new PeriodicTimer(TimeSpan.FromSeconds(5));

            try
            {
                while (await timer.WaitForNextTickAsync(
                    cancellationToken))
                {
                    var tasks = new List<Task>();

                    foreach (var session in _connections.GetSessions())
                    {
                        Task task = CheckClientAsync(
                            session,
                            cancellationToken);

                        tasks.Add(task);
                    }

                    await Task.WhenAll(tasks);
                }
            }
            catch (OperationCanceledException)
                when (cancellationToken.IsCancellationRequested)
            {
                // Server yêu cầu dừng heartbeat.
            }
        }

        private async Task CheckClientAsync(
            IClientSession session,
            CancellationToken cancellationToken)
        {
            using var deadline =
                CancellationTokenSource.CreateLinkedTokenSource(
                    cancellationToken);

            deadline.CancelAfter(TimeSpan.FromSeconds(3));

            try
            {
                Command ping = Command.Create(CommandType.PING);

                Response response = await session.SendCommandAsync(
                    ping,
                    deadline.Token);

                if (deadline.IsCancellationRequested)
                {
                    return;
                }

                if (!response.Success)
                {
                    return;
                }

                _clients.UpdateHeartbeat(
                    session.SessionId,
                    DateTime.UtcNow);
            }
            catch (OperationCanceledException)
            {
                // Hết deadline hoặc Server đang dừng.
            }
            catch (TimeoutException)
            {
                // Request không nhận được phản hồi đúng hạn.
            }
            catch (Exception ex)
            {
                Console.WriteLine(
                    $"Heartbeat lỗi ở {session.SessionId}: {ex.Message}");
            }
        }
    }
}
