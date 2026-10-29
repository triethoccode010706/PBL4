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
    IMouseController mouseController,
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
            dispatcher.Register(
                new MouseMoveCommandHandler(mouseController));

            dispatcher.Register(
                new MouseClickCommandHandler(mouseController));
            dispatcher.RegisterNotImplemented(       
                CommandType.KEY_DOWN,
                CommandType.KEY_UP);

            return dispatcher;
        }
    }
}
