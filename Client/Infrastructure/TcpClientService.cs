using System;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace Client.Infrastructure
{
    public class TcpClientService : ITcpClient
    {
        private TcpClient? _client;
        private NetworkStream? _stream;
        private bool _isConnected;

        public bool IsConnected => _isConnected;

        public async Task<bool> ConnectAsync(string serverIp, int port)
        {
            try
            {
               
                _client = new TcpClient();

                // Kết nối tới server
                await _client.ConnectAsync(serverIp, port);

                // Lấy NetworkStream để gửi/nhận dữ liệu
                _stream = _client.GetStream();

                _isConnected = true;

                Console.WriteLine(
                    $"[TcpClientService] Đã kết nối thành công tới Server {serverIp}:{port}");

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine(
                    $"[TcpClientService Error] Không thể kết nối: {ex.Message}");

                _isConnected = false;

                return false;
            }
        }

        public async Task SendAsync(byte[] data)
        {
            if (!_isConnected || _stream == null)
            {
                throw new InvalidOperationException(
                    "Chưa kết nối tới Server.");
            }

            await _stream.WriteAsync(data, 0, data.Length);
        }

        public async Task StartReceivingAsync(Action<byte[]> onDataReceived)
        {
            byte[] buffer = new byte[4096];

            try
            {
                while (_isConnected && _stream != null)
                {
                    int bytesRead =
                        await _stream.ReadAsync(
                            buffer,
                            0,
                            buffer.Length);

                    if (bytesRead == 0)
                    {
                        Console.WriteLine(
                            "[TcpClientService] Server đã ngắt kết nối.");

                        break;
                    }

                    byte[] receivedData = new byte[bytesRead];

                    Array.Copy(
                        buffer,
                        receivedData,
                        bytesRead);

                    onDataReceived(receivedData);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(
                    $"[TcpClientService Error] Lỗi nhận dữ liệu: {ex.Message}");
            }
            finally
            {
                Disconnect();
            }
        }

        public void Disconnect()
        {
            if (!_isConnected)
                return;

            _isConnected = false;

            _stream?.Close();
            _client?.Close();

            Console.WriteLine(
                "[TcpClientService] Đã ngắt kết nối và giải phóng tài nguyên.");
        }
    }
}