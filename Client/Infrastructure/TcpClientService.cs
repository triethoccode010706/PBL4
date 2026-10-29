using System.IO;
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
            catch (OperationCanceledException)
            {
                try
                {
                    client?.Close();
                }
                catch
                {
                }

                Disconnect();
                throw;
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
            catch (OperationCanceledException)
            {
                Disconnect();
                throw;
            }
            catch (Exception ex) when (IsConnectionLost(ex))
            {
                Disconnect();
                throw new InvalidOperationException("Chưa kết nối tới Server.", ex);
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
            NetworkStream? stream = _stream;
            if (!_isConnected || stream == null)
            {
                throw new InvalidOperationException("Chưa kết nối tới Server.");
            }

            try
            {
                return await stream.ReadAsync(buffer.AsMemory(offset, count), cancellationToken);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex) when (IsConnectionLost(ex))
            {
                Disconnect();
                throw new InvalidOperationException("Chưa kết nối tới Server.", ex);
            }
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
                    _client?.Client.Shutdown(SocketShutdown.Both);
                }
                catch
                {
                }

                try
                {
                    _stream?.Close();
                    _stream?.Dispose();
                }
                catch
                {
                }

                try
                {
                    _client?.Close();
                    _client?.Dispose();
                }
                catch
                {
                }

                _stream = null;
                _client = null;
            }

            Console.WriteLine("[TcpClientService] Đã ngắt kết nối và giải phóng tài nguyên.");
        }

        private static bool IsConnectionLost(Exception ex)
        {
            return ex is IOException or ObjectDisposedException or SocketException;
        }
    }
}
