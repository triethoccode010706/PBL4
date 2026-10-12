using Shared.Models;

namespace Shared.Protocol
{
    public static class ProtocolServices
    {
        public static CommandDispatcher CreateServerDispatcher()
        {
            var dispatcher = new CommandDispatcher();
            dispatcher.Register(new PingCommandHandler());
            dispatcher.Register(new DisconnectCommandHandler());
            dispatcher.RegisterNotImplemented(
                CommandType.GET_INFO,
                CommandType.START_SCREEN,
                CommandType.STOP_SCREEN,
                CommandType.MOUSE_MOVE,
                CommandType.MOUSE_CLICK,
                CommandType.KEY_DOWN,
                CommandType.KEY_UP);
            return dispatcher;
        }

        public static CommandDispatcher CreateClientDispatcher()
        {
            var dispatcher = new CommandDispatcher();
            dispatcher.Register(new PingCommandHandler());
            dispatcher.Register(new DisconnectCommandHandler());
            dispatcher.RegisterNotImplemented(
                CommandType.GET_INFO,
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
