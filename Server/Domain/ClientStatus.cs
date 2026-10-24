using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server.Domain
{
    public enum ClientStatus
    {
        Connected,
        Disconnected,
        TimedOut
    }
}
