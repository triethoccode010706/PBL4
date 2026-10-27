using Client.Application.Handlers;
using Client.Domain;
using Shared.Models;
using Shared.Protocol;
namespace Client.Application
{
    public static class ClientDispatcherFactory
    {
        public static CommandDispatcher Create(
    IMachineInfoProvider machineInfoProvider,
    string secretKey,
    IScreenCapture screenCapture,
    IScreenStreamingService streaming,
    CancellationToken sessionToken)
        {
            var dispatcher = new CommandDispatcher();

            dispatcher.Register(new PingCommandHandler());
            dispatcher.Register(new DisconnectCommandHandler());

            dispatcher.Register(
                new GetInfoCommandHandler(machineInfoProvider));

            dispatcher.Register(
                new AuthenticateCommandHandler(secretKey));

            dispatcher.Register(
                new CaptureScreenCommandHandler(screenCapture));

            dispatcher.Register(
                new StartScreenCommandHandler(
                    streaming,
                    sessionToken));

            dispatcher.Register(
                new StopScreenCommandHandler(streaming));

            dispatcher.RegisterNotImplemented(
                CommandType.MOUSE_MOVE,
                CommandType.MOUSE_CLICK,
                CommandType.KEY_DOWN,
                CommandType.KEY_UP);

            return dispatcher;
        }
    }
}
