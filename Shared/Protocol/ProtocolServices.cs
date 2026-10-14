namespace Shared.Protocol
{
    public static class ProtocolServices
    {
        public static CommandDispatcher CreateServerDispatcher()
        {
            return new CommandDispatcher();
        }
    }
}
