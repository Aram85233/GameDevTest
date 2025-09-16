using StackExchange.Redis;
using System.Text;
using TileMap.Objects;
using TileMap.Surface;

namespace TileMap.ConsoleApp
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.OutputEncoding = Encoding.UTF8;

            //Console.WriteLine("=== Тестирование SurfaceLayer ===");

            //var surface = new SurfaceLayer(30, 15);

            //surface.FillArea(0, 0, 2, 2, TileType.Mountain);

            //surface.SetTile(12, 8, TileType.Mountain);
            //surface.SetTile(11, 7, TileType.Mountain);

            //surface.Print();

            //Console.WriteLine();
            //bool canPlace = surface.CanPlaceObjectsInArea(5, 1, 13, 3);
            //if (canPlace)
            //{
            //    surface.FillArea(5, 1, 13, 3, TileType.Mountain);
            //    surface.Print();
            //}
            //Console.WriteLine();
            //bool canPlaceAll = surface.CanPlaceObjectsInArea(0, 0, 20, 10);
            //Console.WriteLine(canPlaceAll ? "Можно строить" : "Нельзя строить");

            //Console.WriteLine("\n=== Конец теста ===");



            // Подключение к Redis (замените на ваше)
            var redis = ConnectionMultiplexer.Connect("redis:6379").GetDatabase();

            // Создаём поверхность и менеджер карты
            var surface = new SurfaceLayer(10, 10, TileType.Plain);
            var mapManager = new MapLayerManager(surface, redis);

            // Подписка на события
            mapManager.Objects.ObjectCreated += obj => Console.WriteLine($"Создан объект: {obj.Id}");
            mapManager.Objects.ObjectUpdated += obj => Console.WriteLine($"Обновлён объект: {obj.Id}");
            mapManager.Objects.ObjectDeleted += id => Console.WriteLine($"Удалён объект: {id}");

            // Сценарий 1: корректное размещение
            var house = new GameObject("house_1", 2, 2, 3, 3);
            Console.WriteLine(mapManager.TryPlaceObject(house, TileType.Mountain)
                ? "Дом размещён!" : "Невозможно разместить дом");

            // Сценарий 2: объект выходит за границы карты
            var bigBuilding = new GameObject("big_building", 8, 8, 5, 5);
            Console.WriteLine(mapManager.TryPlaceObject(bigBuilding, TileType.Plain)
                ? "Здание размещено!" : "Невозможно разместить здание — выходит за границы карты");

            // Сценарий 3: объект пересекается с уже существующим объектом
            var smallHouse = new GameObject("small_house", 3, 3, 2, 2);
            Console.WriteLine(mapManager.TryPlaceObject(smallHouse, TileType.Plain)
                ? "Малый дом размещён!" : "Невозможно разместить малый дом — пересекается с другим объектом");

            // Сценарий 4: объект на запрещённом тайле
            // Сначала заливаем участок водой
            surface.FillArea(0, 0, 1, 1, TileType.Water);
            var boat = new GameObject("boat", 0, 0, 1, 1);
            Console.WriteLine(mapManager.TryPlaceObject(boat, TileType.Plain)
                ? "Лодка размещена!" : "Невозможно разместить лодку — запрещённый тип тайла");

            // Вывод карты с объектами
            Console.WriteLine("Карта с объектами:");
            mapManager.PrintMapWithObjects();
        }
    }
}