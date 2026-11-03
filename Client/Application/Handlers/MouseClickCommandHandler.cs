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
    public sealed class MouseClickCommandHandler : ICommandHandler
    {
        private readonly IMouseController _mouseController;

        public MouseClickCommandHandler(
            IMouseController mouseController)
        {
            _mouseController = mouseController;
        }

        public CommandType CommandType => CommandType.MOUSE_CLICK;

        public Task<Response> HandleAsync(
            Command command,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (string.IsNullOrWhiteSpace(command.Data))
            {
                return Fail(
                    command,
                    "INVALID_MOUSE_CLICK",
                    "Lệnh thiếu dữ liệu click chuột.");
            }

            MouseClickDto? click;

            try
            {
                click = JsonSerializer.Deserialize<MouseClickDto>(
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
                    "INVALID_MOUSE_CLICK",
                    "Dữ liệu click chuột không đúng định dạng.");
            }

            if (click == null ||
                !double.IsFinite(click.XRatio) ||
                !double.IsFinite(click.YRatio) ||
                click.XRatio < 0 ||
                click.XRatio > 1 ||
                click.YRatio < 0 ||
                click.YRatio > 1)
            {
                return Fail(
                    command,
                    "INVALID_MOUSE_CLICK",
                    "Tọa độ chuột phải nằm trong khoảng 0 đến 1.");
            }

            if (click.Button != MouseButtonType.Left &&
                click.Button != MouseButtonType.Right)
            {
                return Fail(
                    command,
                    "INVALID_MOUSE_CLICK",
                    "Nút chuột không được hỗ trợ.");
            }

            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                _mouseController.Click(
                    click.XRatio,
                    click.YRatio,
                    click.Button);

                return Task.FromResult(
                    Response.Ok(
                        command.RequestId,
                        "Đã gửi thao tác click chuột."));
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail(
                    command,
                    "MOUSE_CLICK_FAILED",
                    $"Không thể click chuột: {ex.Message}");
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