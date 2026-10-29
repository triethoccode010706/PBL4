namespace Shared.Protocol
{
    public class ProtocolValidationException : Exception
    {
        public ProtocolValidationException(string message)
            : base(message)
        {
        }
    }
}
