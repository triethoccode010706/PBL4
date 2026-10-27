using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Shared.Models;
using Shared.Protocol;

namespace Client.Application.Handlers
{
    public sealed class StartScreenCommandHandler: ICommandHandler
    {
        private readonly IScreenStreamingService _streaming;
        private readonly CancellationToken _sessionToken;

        public StartScreenCommandHandler(
            IScreenStreamingService streaming,
            CancellationToken sessionToken)
        {
            _streaming = streaming;
            _sessionToken = sessionToken;
        }

        public CommandType CommandType =>
            CommandType.START_SCREEN;

        public async Task<Response> HandleAsync(
            Command command,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            await _streaming.StartAsync(_sessionToken);

            return Response.Ok(
                command.RequestId,
                "Đã khởi động tác vụ truyền màn hình.");
        }
    }
}