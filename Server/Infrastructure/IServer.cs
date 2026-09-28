using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server.Infrastructure
{
    public interface IServer
    {
        Task StartAsync();
        void Stop();
    }
}
