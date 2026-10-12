namespace Shared.Models
{
    public static class ProtocolMessageKinds
    {
        public const string Command = "command";
        public const string Response = "response";
    }

    public class ProtocolEnvelope
    {
        public string Kind { get; set; } = string.Empty;
        public Command? Command { get; set; }
        public Response? Response { get; set; }
    }
}
