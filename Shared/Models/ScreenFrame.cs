using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.Models
{
    public sealed class ScreenFrame
    {
        public byte[] ImageData { get; set; }
            = Array.Empty<byte>();
    }
}