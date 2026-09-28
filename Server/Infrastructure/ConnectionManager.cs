using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Concurrent;
namespace Server.Infrastructure
{
    public class ConnectionManager: IConnectionManager
    {
        private readonly ConcurrentDictionary<string, IClientSession> _sessions = new();
        public void AddSession(IClientSession session)
        {
            _sessions[session.SessionId] = session;
            Console.WriteLine($"[ConnectionManager] Đã thêm session: {session.SessionId}. Tổng số client: {_sessions.Count}");
        }
        public void RemoveSession(string sessionId)
        {
            if (_sessions.TryRemove(sessionId, out var session))
            {
                session.Disconnect();
                Console.WriteLine($"[ConnectionManager] Đã xóa session: {sessionId}. Tổng số client: {_sessions.Count}");
            }
        }
        public IClientSession? GetSession(string sessionId)
        {
            _sessions.TryGetValue(sessionId, out var session);
            return session;
        }
    }
}
