using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Shared.Models;
using Shared.Protocol;

namespace Client.Application.Handlers
{
    public sealed class StopScreenCommandHandler : ICommandHandler
    {
        private readonly IScreenStreamingService _streaming;
        private readonly Action _releaseInput;

        public StopScreenCommandHandler(
            IScreenStreamingService streaming,
            Action releaseInput)
        {
            _streaming = streaming;
            _releaseInput = releaseInput;
        }

        public CommandType CommandType => CommandType.STOP_SCREEN;

        public async Task<Response> HandleAsync(
            Command command,
            CancellationToken cancellationToken = default)
        {
            var errors = new List<Exception>();

            try
            {
                _releaseInput();
            }
            catch (Exception ex)
            {
                errors.Add(ex);
            }

            try
            {
                await _streaming.StopAsync();
            }
            catch (Exception ex)
            {
                errors.Add(ex);
            }

            if (errors.Count > 0)
            {
                throw new AggregateException(
                    "Không thể hoàn tất việc dừng màn hình.",
                    errors);
            }

            return Response.Ok(
                command.RequestId,
                "Đã thả chuột/phím và dừng truyền màn hình.");
        }
    }
}