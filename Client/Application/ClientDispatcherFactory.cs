using Client.Application.Handlers;
using Client.Domain;
using Shared.Models;
using Shared.Protocol;
using Client.Infrastructure;
namespace Client.Application
{
    public static class ClientDispatcherFactory
    {
        public static CommandDispatcher Create( IMachineInfoProvider machineInfoProvider,string secretKey)
        {
            var dispatcher = new CommandDispatcher();
            dispatcher.Register(new PingCommandHandler());
            dispatcher.Register(new DisconnectCommandHandler());
            dispatcher.Register(new GetInfoCommandHandler(machineInfoProvider));
            dispatcher.Register(new AuthenticateCommandHandler(secretKey));
            dispatcher.Register(new CaptureScreenCommandHandler(new ScreenCaptureService()));
            dispatcher.RegisterNotImplemented(
                CommandType.START_SCREEN,
                CommandType.STOP_SCREEN,
                CommandType.MOUSE_MOVE,
                CommandType.MOUSE_CLICK,
                CommandType.KEY_DOWN,
                CommandType.KEY_UP);
            return dispatcher;
        }
    }
}
