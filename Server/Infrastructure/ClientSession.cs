using System.IO;
using System.Net.Sockets;
using Shared.Models;
using Shared.Protocol;

namespace Server.Infrastructure
{
    public class ClientSession : IClientSession
    {
        private readonly TcpClient _client;
        private readonly NetworkStream _stream;
        private readonly SemaphoreSlim _sendLock = new(1, 1);
        private readonly object _sync = new();
        private ProtocolSession? _protocol;
        private volatile bool _isConnected = true;

        public string SessionId { get; } = Guid.NewGuid().ToString();
        public bool IsConnected => _isConnected;
        public string RemoteEndPoint { get; }
        public Stream Stream => _stream;

        public ClientSession(TcpClient client)
        {
            _client = client;
            _stream = client.GetStream();
            RemoteEndPoint = client.Client.RemoteEndPoint?.ToString() ?? "unknown";
        }

        public void AttachProtocol(ProtocolSession protocol)
        {
            _protocol = protocol ?? throw new ArgumentNullException(nameof(protocol));
        }

        public async Task<Response> SendCommandAsync(
            Command command,
            CancellationToken cancellationToken = default)
        {
            if (_protocol == null)
            {
                throw new InvalidOperationException("Session chưa gắn ProtocolSession.");
            }

            if (!_isConnected)
            {
                throw new InvalidOperationException("Session đã ngắt kết nối.");
            }
            bool allowed = CommandAuthorization.CanExecute(command.Type,AuthStatus);

            if (!allowed)
            {
                throw new InvalidOperationException(
                    "Phiên chưa được phép thực hiện lệnh này.");
            }
            return await _protocol.SendCommandAsync(command, cancellationToken);
        }

        public async Task SendAsync(byte[] data, CancellationToken cancellationToken = default)
        {
            if (!_isConnected)
            {
                throw new InvalidOperationException("Session đã ngắt kết nối.");
            }

            await _sendLock.WaitAsync(cancellationToken);
            try
            {
                if (!_isConnected)
                {
                    throw new InvalidOperationException("Session đã ngắt kết nối.");
                }

                await _stream.WriteAsync(data, cancellationToken);
                await _stream.FlushAsync(cancellationToken);
            }
            catch (OperationCanceledException)
            {
                Disconnect();
                throw;
            }
            catch (Exception ex) when (IsConnectionLost(ex))
            {
                Disconnect();
                throw new InvalidOperationException("Session đã ngắt kết nối.", ex);
            }
            finally
            {
                _sendLock.Release();
            }
        }

        public async Task<int> ReadAsync(
            byte[] buffer,
            int offset,
            int count,
            CancellationToken cancellationToken = default)
        {
            if (!_isConnected)
            {
                throw new InvalidOperationException("Session đã ngắt kết nối.");
            }

            try
            {
                return await _stream.ReadAsync(buffer.AsMemory(offset, count), cancellationToken);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex) when (IsConnectionLost(ex))
            {
                Disconnect();
                throw new InvalidOperationException("Session đã ngắt kết nối.", ex);
            }
        }

        public void Disconnect()
        {
            lock (_sync)
            {
                if (!_isConnected)
                {
                    return;
                }

                _isConnected = false;

                try
                {
                    _client.Client.Shutdown(SocketShutdown.Both);
                }
                catch
                {
                }

                try
                {
                    _stream.Close();
                }
                catch
                {
                }

                try
                {
                    _client.Close();
                }
                catch
                {
                }
            }

            Console.WriteLine($"[ClientSession] Đã đóng phiên: {SessionId} ({RemoteEndPoint})");
        }

        private static bool IsConnectionLost(Exception ex)
        {
            return ex is IOException or ObjectDisposedException or SocketException;
        }
        public AuthenticationStatus AuthStatus
        {
            get
            {
                lock (_sync)
                {
                    return _protocol?.AuthStatus
                        ?? AuthenticationStatus.NotAuthenticated;
                }
            }
        }

        public void MarkAuthenticated()
        {
            lock (_sync)
            {
                if (!_isConnected || _protocol == null)
                {
                    throw new InvalidOperationException(
                        "Session đã ngắt kết nối.");
                }

                _protocol.MarkAuthenticated();
            }
        }
    }
}
