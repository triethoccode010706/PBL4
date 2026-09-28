using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server.Infrastructure
{
    public interface IConnectionManager
    {
        void AddSession(IClientSession session);
        void RemoveSession(string sessionId);
        IClientSession? GetSession(string sessionId);
    }
}
