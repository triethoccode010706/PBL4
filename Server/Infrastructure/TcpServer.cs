using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Server.Infrastructure
{
    public class TcpServer : IServer
    {
        private TcpListener? _listener;
        private bool _isRunning = false;
        private readonly int _port;
        private readonly IConnectionManager _connectionManager;
        public TcpServer(int port, IConnectionManager connectionManager)
        {
            _port = port;
            _connectionManager = connectionManager;
        }
        public async Task StartAsync()
        {
            _listener = new TcpListener(System.Net.IPAddress.Any, _port);
            _listener.Start();
            _isRunning = true;
            Console.WriteLine($"Server started on port {_port}.");
            while (_isRunning)
            {
                try
                {
                    TcpClient client = await _listener.AcceptTcpClientAsync();
                    Console.WriteLine("Client connected.");
                    ClientSession session= new ClientSession(client);
                    _connectionManager.AddSession(session);
                    _ = Task.Run(async () =>
                    {
                        await session.StartReceivingAsync((sessionId, message) =>
                        {
                            // Đây chính là callback (Action) nhận message từ Client gửi lên
                            Console.WriteLine($"[Received from {sessionId}]: {message}");
                        });

                        // Khi Client ngắt kết nối, xóa khỏi danh sách quản lý
                        _connectionManager.RemoveSession(session.SessionId);
                    });
                }
                catch (Exception ex)
                {
                    if (_isRunning)
                    {
                        Console.WriteLine("Server stopped.");
                    }
                    else
                    {
                        Console.WriteLine($"Error accepting client: {ex.Message}");
                    }
                }
            }
        }
        public void Stop()
        {
            _isRunning = false;
            _listener?.Stop();
            Console.WriteLine("[Server] Đã dừng hoạt động.");
        }
    }
}
