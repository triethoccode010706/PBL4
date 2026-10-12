using System.Collections.Concurrent;

namespace Server.Infrastructure
{
    public class ConnectionManager : IConnectionManager
    {
        private readonly ConcurrentDictionary<string, IClientSession> _sessions = new();

        public event Action<IClientSession>? SessionAdded;
        public event Action<string>? SessionRemoved;

        public void AddSession(IClientSession session)
        {
            _sessions[session.SessionId] = session;
            Console.WriteLine(
                $"[ConnectionManager] Session {session.SessionId} từ {session.RemoteEndPoint}. Tổng: {_sessions.Count}");
            SessionAdded?.Invoke(session);
        }

        public void RemoveSession(string sessionId)
        {
            if (_sessions.TryRemove(sessionId, out var session))
            {
                session.Disconnect();
                Console.WriteLine(
                    $"[ConnectionManager] Đã xóa session: {sessionId}. Tổng: {_sessions.Count}");
                SessionRemoved?.Invoke(sessionId);
            }
        }

        public IClientSession? GetSession(string sessionId)
        {
            _sessions.TryGetValue(sessionId, out var session);
            return session;
        }

        public IReadOnlyList<IClientSession> GetSessions()
        {
            return _sessions.Values.ToList();
        }

        public void DisconnectAll()
        {
            foreach (var sessionId in _sessions.Keys.ToList())
            {
                RemoveSession(sessionId);
            }
        }
    }
}
