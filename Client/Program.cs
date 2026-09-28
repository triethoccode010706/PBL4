    // See https://aka.ms/new-console-template for more information
    using Client.Infrastructure;
    using System.Text;



    class Program
    {
        static async Task Main(string[] args)
        {
            ITcpClient client = new TcpClientService();

            // 1. Kết nối tới Server (Giả sử Server chạy ở máy local IP 127.0.0.1, cổng 5000)
            bool connected = await client.ConnectAsync("127.0.0.1", 5000);
            if (!connected) return;

            // 2. Chạy luồng lắng nghe dữ liệu từ Server gửi xuống (chạy nền)
            _ = Task.Run(async () =>
            {
                await client.StartReceivingAsync((receivedBytes) =>
                {
                    string msg = Encoding.UTF8.GetString(receivedBytes);
                    Console.WriteLine($"[Server gửi xuống]: {msg}");
                });
            });

            // 3. Thử gửi một tin nhắn test lên Server
            while (client.IsConnected)
            {
                string? input = Console.ReadLine();
                if (input == "exit") break;

                if (!string.IsNullOrEmpty(input))
                {
                    byte[] dataToSend = Encoding.UTF8.GetBytes(input);
                    await client.SendAsync(dataToSend);
                }
            }

            client.Disconnect();
        }
    }