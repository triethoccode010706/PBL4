using System.Net.Sockets;

namespace Client.Infrastructure
{
    public class TcpClientService : ITcpClient
    {
        private readonly SemaphoreSlim _sendLock = new(1, 1);
        private readonly object _sync = new();
        private TcpClient? _client;
        private NetworkStream? _stream;
        private volatile bool _isConnected;

        public bool IsConnected => _isConnected;

        public Stream Stream =>
            _stream ?? throw new InvalidOperationException("Chưa kết nối tới Server.");

        public async Task<bool> ConnectAsync(
            string serverIp,
            int port,
            CancellationToken cancellationToken = default)
        {
            Disconnect();

            TcpClient? client = null;
            try
            {
                client = new TcpClient();
                await client.ConnectAsync(serverIp, port, cancellationToken);

                lock (_sync)
                {
                    _client = client;
                    _stream = client.GetStream();
                    _isConnected = true;
                }

                Console.WriteLine($"[TcpClientService] Đã kết nối {serverIp}:{port}");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[TcpClientService] Không thể kết nối: {ex.Message}");
                try
                {
                    client?.Close();
                }
                catch
                {
                }

                Disconnect();
                return false;
            }
        }

        public async Task SendAsync(byte[] data, CancellationToken cancellationToken = default)
        {
            if (!_isConnected || _stream == null)
            {
                throw new InvalidOperationException("Chưa kết nối tới Server.");
            }

            await _sendLock.WaitAsync(cancellationToken);
            try
            {
                if (!_isConnected || _stream == null)
                {
                    throw new InvalidOperationException("Chưa kết nối tới Server.");
                }

                await _stream.WriteAsync(data, cancellationToken);
                await _stream.FlushAsync(cancellationToken);
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
            if (_stream == null)
            {
                throw new InvalidOperationException("Chưa kết nối tới Server.");
            }

            return await _stream.ReadAsync(buffer.AsMemory(offset, count), cancellationToken);
        }

        public void Disconnect()
        {
            lock (_sync)
            {
                if (!_isConnected && _client == null && _stream == null)
                {
                    return;
                }

                _isConnected = false;

                try
                {
                    _stream?.Close();
                }
                catch
                {
                }

                try
                {
                    _client?.Close();
                }
                catch
                {
                }

                _stream = null;
                _client = null;
            }

            Console.WriteLine("[TcpClientService] Đã ngắt kết nối và giải phóng tài nguyên.");
        }
    }
}
