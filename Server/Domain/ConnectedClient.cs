namespace Server.Domain
{
    public sealed class ConnectedClient
    {
        public string SessionId { get; }
        public string RemoteEndPoint { get; }

        public ConnectedClient(string sessionId, string remoteEndPoint)
        {
            SessionId = sessionId;
            RemoteEndPoint = remoteEndPoint;
        }

        public override string ToString()
        {
            return $"{SessionId} ({RemoteEndPoint})";
        }
    }
}
