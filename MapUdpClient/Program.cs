using System.Text;
using TileMap.MapUdpClient;
class Program
{
    static void Main()
    {
        Console.OutputEncoding = Encoding.UTF8;

        var client = new MapUdpClient();

        client.Connect("127.0.0.1", 9050);

        Console.WriteLine("Нажмите Enter для выхода...");
        while (!Console.KeyAvailable)
        {
            client.PollEvents();
            Thread.Sleep(5); // лёгкая пауза, чтобы не перегружать CPU
        }

        Console.WriteLine("👋 Клиент завершил работу");
    }
}