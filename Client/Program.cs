using Client.Application;
using Client.Infrastructure;

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
