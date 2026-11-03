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
    public sealed class MouseActionCommandHandler : ICommandHandler
    {
        private readonly IMouseController _mouseController;

        public CommandType CommandType { get; }

        public MouseActionCommandHandler(
            CommandType commandType,
            IMouseController mouseController)
        {
            if (commandType != CommandType.MOUSE_DOWN &&
                commandType != CommandType.MOUSE_UP &&
                commandType != CommandType.MOUSE_WHEEL)
            {
                throw new ArgumentException(
                    "Loại lệnh chuột không được hỗ trợ.",
                    nameof(commandType));
            }

            CommandType = commandType;
            _mouseController = mouseController;
        }

        public Task<Response> HandleAsync(
            Command command,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            Response response;

            try
            {
                if (string.IsNullOrWhiteSpace(command.Data))
                {
                    throw new ArgumentException(
                        "Lệnh thiếu dữ liệu chuột.");
                }

                if (CommandType == CommandType.MOUSE_WHEEL)
                {
                    MouseWheelDto input =
                        JsonSerializer.Deserialize<MouseWheelDto>(
                            command.Data)
                        ?? throw new ArgumentException(
                            "Dữ liệu lăn chuột không hợp lệ.");

                    ValidatePosition(input.XRatio, input.YRatio);

                    cancellationToken.ThrowIfCancellationRequested();

                    _mouseController.Scroll(
                        input.XRatio,
                        input.YRatio,
                        input.Delta);
                }
                else
                {
                    MouseClickDto input =
                        JsonSerializer.Deserialize<MouseClickDto>(
                            command.Data)
                        ?? throw new ArgumentException(
                            "Dữ liệu nút chuột không hợp lệ.");

                    ValidatePosition(input.XRatio, input.YRatio);
                    ValidateButton(input.Button);

                    cancellationToken.ThrowIfCancellationRequested();

                    if (CommandType == CommandType.MOUSE_DOWN)
                    {
                        _mouseController.ButtonDown(
                            input.XRatio,
                            input.YRatio,
                            input.Button);
                    }
                    else
                    {
                        _mouseController.ButtonUp(
                            input.XRatio,
                            input.YRatio,
                            input.Button);
                    }
                }

                response = Response.Ok(
                    command.RequestId,
                    "Đã xử lý thao tác chuột.");
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (JsonException)
            {
                response = Response.Fail(
                    command.RequestId,
                    "INVALID_MOUSE_INPUT",
                    "Dữ liệu chuột không đúng định dạng.");
            }
            catch (ArgumentException ex)
            {
                response = Response.Fail(
                    command.RequestId,
                    "INVALID_MOUSE_INPUT",
                    ex.Message);
            }
            catch (Exception ex)
            {
                response = Response.Fail(
                    command.RequestId,
                    "MOUSE_INPUT_FAILED",
                    $"Không thực hiện được thao tác chuột: {ex.Message}");
            }

            return Task.FromResult(response);
        }

        private static void ValidatePosition(
            double xRatio,
            double yRatio)
        {
            if (!double.IsFinite(xRatio) ||
                !double.IsFinite(yRatio) ||
                xRatio < 0 || xRatio > 1 ||
                yRatio < 0 || yRatio > 1)
            {
                throw new ArgumentException(
                    "Tọa độ chuột phải nằm trong khoảng 0 đến 1.");
            }
        }

        private static void ValidateButton(MouseButtonType button)
        {
            if (button != MouseButtonType.Left &&
                button != MouseButtonType.Right)
            {
                throw new ArgumentException(
                    "Nút chuột không được hỗ trợ.");
            }
        }
    }
}