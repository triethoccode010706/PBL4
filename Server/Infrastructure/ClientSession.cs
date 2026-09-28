using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Sockets;
namespace Server.Infrastructure
{
    public class ClientSession: IClientSession
    {
        public string SessionId { get; }= Guid.NewGuid().ToString();
        private readonly TcpClient _client;
        private readonly NetworkStream _stream;
        private bool _isConnected = true;
        public bool IsConnected => _isConnected;
        public ClientSession(TcpClient client)
        {
            _client = client;
            _stream = client.GetStream();
            _isConnected = true;
        }
        public async Task StartReceivingAsync(Action <string, string>onMessageReceived)
        {
            byte[] buffer = new byte[4096];
            while (_isConnected && _client.Connected)
            {
                try
                {   
                    int bytesRead = await _stream.ReadAsync (buffer, 0, buffer.Length);
                    if (bytesRead == 0) { break; }
                    string message = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                    onMessageReceived(SessionId, message);
                }
                catch(Exception ex)
                {
                    Console.WriteLine(ex);
                    break;
                }   
            }
            Disconnect();   
        }
        public async Task SendAsync(string message)
        {
            if (!_isConnected || _stream == null) return;

            byte[] data = Encoding.UTF8.GetBytes(message);
            await _stream.WriteAsync(data, 0, data.Length);
        }
        public void Disconnect()
        {
            _isConnected = false;
            _stream.Close();
            _client.Close();
            Console.WriteLine($"[ClientSession] Đã đóng phiên làm việc: {SessionId}");
        }
    }
    
}
