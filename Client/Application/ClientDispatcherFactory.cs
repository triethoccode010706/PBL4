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
    IKeyboardController keyboardController,
    Action releaseInput,
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

            dispatcher.Register( new StopScreenCommandHandler( streaming, releaseInput));
            dispatcher.Register(
                new MouseMoveCommandHandler(mouseController));

            dispatcher.Register(
                new MouseClickCommandHandler(mouseController));
            dispatcher.Register(new KeyCommandHandler(CommandType.KEY_DOWN, keyboardController));

            dispatcher.Register( new KeyCommandHandler( CommandType.KEY_UP,keyboardController));
            dispatcher.Register( new MouseActionCommandHandler( CommandType.MOUSE_DOWN, mouseController));

            dispatcher.Register( new MouseActionCommandHandler( CommandType.MOUSE_UP,mouseController));

            dispatcher.Register( new MouseActionCommandHandler(  CommandType.MOUSE_WHEEL,  mouseController));

            return dispatcher;
        }
    }
}
