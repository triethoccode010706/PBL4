using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.Models
{
    public sealed class MouseMoveDto
    {
        public required double XRatio { get; set; }
        public required double YRatio { get; set; }
    }
}