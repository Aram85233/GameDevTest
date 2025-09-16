using System.Text;

namespace TileMap.ConsoleApp
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.OutputEncoding = Encoding.UTF8;

            Console.WriteLine("=== Тестирование SurfaceLayer ===");

            var surface = new SurfaceLayer(30, 15);

            surface.FillArea(0, 0, 2, 2, TileType.Mountain);

            surface.SetTile(12, 8, TileType.Mountain);
            surface.SetTile(11, 7, TileType.Mountain);

            PrintMap(surface);

            Console.WriteLine();
            bool canPlace = surface.CanPlaceObjectsInArea(5, 1, 13, 3);
            if (canPlace)
            {
                surface.FillArea(5, 1, 13, 3, TileType.Mountain);
                PrintMap(surface);
            }
            Console.WriteLine();
            bool canPlaceAll = surface.CanPlaceObjectsInArea(0, 0, 20, 10);
            Console.WriteLine(canPlaceAll ? "Можно строить" : "Нельзя строить");

            Console.WriteLine("\n=== Конец теста ===");
        }

        static void PrintMap(SurfaceLayer surface)
        {
            for (int y = 0; y < surface.Height; y++)
            {
                for (int x = 0; x < surface.Width; x++)
                {
                    var tile = surface.GetTile(x, y);
                    char symbol = tile == TileType.Plain ? '.' : '^';
                    Console.Write(symbol);
                }
                Console.WriteLine();
            }
        }
    }
}