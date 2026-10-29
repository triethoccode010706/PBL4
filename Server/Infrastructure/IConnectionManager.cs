namespace Server.Infrastructure
{
    public interface IConnectionManager
    {
        event Action<IClientSession>? SessionAdded;
        event Action<string>? SessionRemoved;

        void AddSession(IClientSession session);
        void RemoveSession(string sessionId);
        IClientSession? GetSession(string sessionId);
        IReadOnlyList<IClientSession> GetSessions();
        void DisconnectAll();
    }
}
