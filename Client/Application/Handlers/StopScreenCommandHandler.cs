using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Shared.Models;
using Shared.Protocol;

namespace Client.Application.Handlers
{
    public sealed class StopScreenCommandHandler
        : ICommandHandler
    {
        private readonly IScreenStreamingService _streaming;

        public StopScreenCommandHandler(
            IScreenStreamingService streaming)
        {
            _streaming = streaming;
        }

        public CommandType CommandType =>
            CommandType.STOP_SCREEN;

        public async Task<Response> HandleAsync(
            Command command,
            CancellationToken cancellationToken = default)
        {
            await _streaming.StopAsync();

            return Response.Ok(
                command.RequestId,
                "Tác vụ truyền màn hình đã kết thúc.");
        }
    }
}
