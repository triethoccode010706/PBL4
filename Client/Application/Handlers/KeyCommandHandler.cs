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
    public sealed class KeyCommandHandler : ICommandHandler
    {
        private readonly IKeyboardController _keyboardController;

        public CommandType CommandType { get; }

        public KeyCommandHandler(
            CommandType commandType,
            IKeyboardController keyboardController)
        {
            if (commandType != CommandType.KEY_DOWN &&
                commandType != CommandType.KEY_UP)
            {
                throw new ArgumentException(
                    "Handler chỉ hỗ trợ KEY_DOWN và KEY_UP.",
                    nameof(commandType));
            }

            CommandType = commandType;
            _keyboardController = keyboardController;
        }

        public Task<Response> HandleAsync(
            Command command,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (string.IsNullOrWhiteSpace(command.Data))
            {
                return Fail(
                    command,
                    "INVALID_KEY_INPUT",
                    "Lệnh thiếu dữ liệu bàn phím.");
            }

            KeyInputDto? input;

            try
            {
                input = JsonSerializer.Deserialize<KeyInputDto>(
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
                    "INVALID_KEY_INPUT",
                    "Dữ liệu bàn phím không đúng định dạng.");
            }

            if (input == null ||
                input.VirtualKeyCode < 1 ||
                input.VirtualKeyCode > 254)
            {
                return Fail(
                    command,
                    "INVALID_KEY_INPUT",
                    "Mã phím phải nằm trong khoảng 1 đến 254.");
            }

            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (CommandType == CommandType.KEY_DOWN)
                {
                    _keyboardController.KeyDown(
                        input.VirtualKeyCode,
                        input.IsExtended);
                }
                else
                {
                    _keyboardController.KeyUp(
                        input.VirtualKeyCode,
                        input.IsExtended);
                }

                return Task.FromResult(
                    Response.Ok(
                        command.RequestId,
                        "Đã xử lý thao tác bàn phím."));
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail(
                    command,
                    "KEY_INPUT_FAILED",
                    $"Không thể thực hiện thao tác bàn phím: {ex.Message}");
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
