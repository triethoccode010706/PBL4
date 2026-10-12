namespace Shared.Models
{
    public class Command
    {
        public string RequestId { get; set; } = string.Empty;
        public CommandType Type { get; set; }
        public string Data { get; set; } = string.Empty;
        public long Timestamp { get; set; } = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        public static Command Create(CommandType type, string data = "")
        {
            return new Command
            {
                RequestId = Guid.NewGuid().ToString(),
                Type = type,
                Data = data,
                Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
            };
        }
    }
}
