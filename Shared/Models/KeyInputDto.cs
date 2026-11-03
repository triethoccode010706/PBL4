using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.Models
{
    public sealed class KeyInputDto
    {
        public required int VirtualKeyCode { get; set; }

        public required bool IsExtended { get; set; }
    }
}
