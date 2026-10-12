using Client.Infrastructure;
using Shared.Models;
using Shared.Protocol;

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

        ITcpClient client = new TcpClientService();
        bool connected = await client.ConnectAsync("127.0.0.1", 5000, cts.Token);
        if (!connected)
        {
            return;
        }

        var protocol = new ProtocolSession(
            client,
            client.Stream,
            ProtocolServices.CreateClientDispatcher());

        Task receiveTask = protocol.RunAsync(cts.Token);

        Console.WriteLine("Lệnh: ping | info | screen | disconnect | exit");

        try
        {
            while (client.IsConnected && !cts.IsCancellationRequested)
            {
                string? input = await Task.Run(Console.ReadLine, cts.Token);
                if (input == null || input.Equals("exit", StringComparison.OrdinalIgnoreCase))
                {
                    break;
                }

                if (string.IsNullOrWhiteSpace(input))
                {
                    continue;
                }

                Command? command = TryParseCommand(input);
                if (command == null)
                {
                    Console.WriteLine("Lệnh không nhận diện. Dùng: ping, info, screen, disconnect, exit.");
                    continue;
                }

                Response response = await protocol.SendCommandAsync(command, cts.Token);
                PrintResponse(response);

                if (command.Type == CommandType.DISCONNECT)
                {
                    break;
                }
            }
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Lỗi: {ex.Message}");
        }
        finally
        {
            cts.Cancel();
            client.Disconnect();
            try
            {
                await receiveTask;
            }
            catch (OperationCanceledException)
            {
            }
        }
    }

    static Command? TryParseCommand(string input)
    {
        return input.Trim().ToLowerInvariant() switch
        {
            "ping" => Command.Create(CommandType.PING),
            "info" => Command.Create(CommandType.GET_INFO),
            "screen" => Command.Create(CommandType.START_SCREEN),
            "disconnect" => Command.Create(CommandType.DISCONNECT),
            _ => Enum.TryParse(input.Trim(), true, out CommandType type)
                ? Command.Create(type)
                : null
        };
    }

    static void PrintResponse(Response response)
    {
        if (response.Success)
        {
            Console.WriteLine($"[OK] {response.Message} {response.Data}");
        }
        else
        {
            Console.WriteLine($"[LỖI {response.ErrorCode}] {response.Message}");
        }
    }
}
