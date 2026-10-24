using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.Models
{
    public sealed class MachineInfoDto
    {
        public string MachineName { get; set; } = "";
        public string UserName { get; set; } = "";
        public string OsDescription { get; set; } = "";
    }
}
