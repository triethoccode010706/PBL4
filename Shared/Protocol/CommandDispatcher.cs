using Shared.Models;

namespace Shared.Protocol
{
    public class CommandDispatcher
    {
        private readonly Dictionary<CommandType, ICommandHandler> _handlers = new();

        public void Register(ICommandHandler handler)
        {
            ArgumentNullException.ThrowIfNull(handler);
            _handlers[handler.CommandType] = handler;
        }

        public void RegisterNotImplemented(params CommandType[] commandTypes)
        {
            foreach (CommandType commandType in commandTypes)
            {
                Register(new NotImplementedCommandHandler(commandType));
            }
        }

        public bool CanHandle(CommandType commandType)
        {
            return _handlers.ContainsKey(commandType);
        }

        public async Task<Response> DispatchAsync(
            Command command,
            CancellationToken cancellationToken = default)
        {
            if (!_handlers.TryGetValue(command.Type, out ICommandHandler? handler))
            {
                return Response.Fail(
                    command.RequestId,
                    ErrorCodes.UnknownCommand,
                    $"Không có handler cho lệnh {command.Type}.");
            }

            try
            {
                Response response = await handler.HandleAsync(command, cancellationToken);
                if (string.IsNullOrWhiteSpace(response.RequestId))
                {
                    response.RequestId = command.RequestId;
                }

                return response;
            }
            catch (Exception ex)
            {
                return Response.Fail(
                    command.RequestId,
                    ErrorCodes.InternalError,
                    $"Lỗi xử lý lệnh {command.Type}: {ex.Message}");
            }
        }
    }
}
