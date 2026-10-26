using Client.Application;
using Client.Infrastructure;
using Client.Domain;
class Program
{
    static async Task Main(string[] args)
    {
        using var cts = new CancellationTokenSource();
        Console.CancelKeyPress += (_, e) =>
        {
            e.Cancel = true;
            cts.Cancel();
        };
        System.Windows.Forms.Application.SetHighDpiMode(
        System.Windows.Forms.HighDpiMode.PerMonitorV2);
        if (args.Length == 1 && args[0] == "--capture")
        {
            try
            {
                System.Windows.Forms.Application.SetHighDpiMode(
                    System.Windows.Forms.HighDpiMode.PerMonitorV2);

                IScreenCapture capture = new ScreenCaptureService();

                byte[] image = capture.CaptureJpeg(40);

                string folder = Path.Combine(
                    Environment.GetFolderPath(
                        Environment.SpecialFolder.LocalApplicationData),
                    "PBL4",
                    "Captures");

                Directory.CreateDirectory(folder);

                string filePath = Path.Combine(
                    folder,
                    $"screen-{DateTime.Now:yyyyMMdd-HHmmss-fff}.jpg");

                await File.WriteAllBytesAsync(filePath, image);
                double sizeKb = image.Length / 1024.0;

                Console.WriteLine($"Dung lượng: {sizeKb:F1} KiB");

                Console.WriteLine("Đã lưu ảnh: " + filePath);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Chụp màn hình thất bại: " + ex.Message);
            }

            return;
        }
        IClientAgent agent = new ClientAgent(new TcpClientService(), new MachineInfoProvider());
        agent.Log += Console.WriteLine;

        try
        {
            await agent.RunAsync("127.0.0.1", 5000, cts.Token);
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Lỗi: {ex.Message}");
        }
    }
}
