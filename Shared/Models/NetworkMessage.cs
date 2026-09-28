using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.Models
{
    public class NetworkMessage
    {
        public string RequestId { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string Data { get; set; } = string.Empty; // Chứa payload dạng JSON hoặc chuỗi
        public long Timestamp { get; set; } = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
    }
}
