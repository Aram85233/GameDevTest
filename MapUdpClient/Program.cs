using System.Text;
using TileMap.MapUdpClient;
class Program
{
    static void Main()
    {
        Console.OutputEncoding = Encoding.UTF8;

        var client = new MapUdpClient();

        client.Connect("tilemap-server", 9050);


        var cts = new CancellationTokenSource();

        Console.CancelKeyPress += (s, e) =>
        {
            e.Cancel = true;
            cts.Cancel();
        };

        while (!cts.Token.IsCancellationRequested)
        {
            client.PollEvents();
            Thread.Sleep(5);
        }

        Console.WriteLine("👋 Клиент завершил работу");
    }
}