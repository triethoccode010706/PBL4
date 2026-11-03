using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Text.Json;
using Client.Application;
using Shared.Models;
using Shared.Protocol;

namespace Client.Application.Handlers
{
    public sealed class MouseMoveCommandHandler : ICommandHandler
    {
        private readonly IMouseController _mouseController;

        public MouseMoveCommandHandler(
            IMouseController mouseController)
        {
            _mouseController = mouseController;
        }

        public CommandType CommandType => CommandType.MOUSE_MOVE;

        public Task<Response> HandleAsync(
            Command command,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            MouseMoveDto? position;

            try
            {
                if (string.IsNullOrWhiteSpace(command.Data))
                {
                    return Fail(
                        command,
                        "INVALID_MOUSE_POSITION",
                        "Lệnh thiếu tọa độ chuột.");
                }

                position = JsonSerializer.Deserialize<MouseMoveDto>(
                    command.Data,
                    new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });
            }
            catch (JsonException)
            {
                return Fail(
                    command,
                    "INVALID_MOUSE_POSITION",
                    "Dữ liệu tọa độ chuột không đúng định dạng.");
            }

            if (position == null ||
                !double.IsFinite(position.XRatio) ||
                !double.IsFinite(position.YRatio) ||
                position.XRatio < 0 ||
                position.XRatio > 1 ||
                position.YRatio < 0 ||
                position.YRatio > 1)
            {
                return Fail(
                    command,
                    "INVALID_MOUSE_POSITION",
                    "Tọa độ chuột phải nằm trong khoảng 0 đến 1.");
            }

            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                _mouseController.MoveTo(
                    position.XRatio,
                    position.YRatio);

                return Task.FromResult(
                    Response.Ok(
                        command.RequestId,
                        "Đã di chuyển chuột."));
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail(
                    command,
                    "MOUSE_MOVE_FAILED",
                    $"Không thể di chuyển chuột: {ex.Message}");
            }
        }

        private static Task<Response> Fail(
            Command command,
            string errorCode,
            string message)
        {
            return Task.FromResult(
                Response.Fail(
                    command.RequestId,
                    errorCode,
                    message));
        }
    }
}
