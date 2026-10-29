namespace Shared.Models
{
    public class Response
    {
        public string RequestId { get; set; } = string.Empty;
        public bool Success { get; set; }
        public string ErrorCode { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string Data { get; set; } = string.Empty;
        public long Timestamp { get; set; } = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        public static Response Ok(string requestId, string message = "OK", string data = "")
        {
            return new Response
            {
                RequestId = requestId,
                Success = true,
                Message = message,
                Data = data,
                Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
            };
        }

        public static Response Fail(string requestId, string errorCode, string message)
        {
            return new Response
            {
                RequestId = requestId,
                Success = false,
                ErrorCode = errorCode,
                Message = message,
                Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
            };
        }
    }
}
