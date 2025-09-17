using StackExchange.Redis;
using System.Text;
using TileMap.Contracts.Events;
using TileMap.Networking;
using TileMap.Objects;
using TileMap.Regions;
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
            var redis = ConnectionMultiplexer.Connect("localhost").GetDatabase();

            // Создаём поверхность и менеджер карты
            var surface = new SurfaceLayer(10, 10, TileType.Plain);
            var mapManager = new MapLayerManager(surface, redis);

            var regions = new RegionLayer(10, 10, 4);

            var provider = new MapQueryProvider(mapManager, regions);
            var udpServer = new MapUdpServer(provider, port: 9050);

            // Подписка на события
            mapManager.Objects.ObjectCreated += obj =>
            {
                var ev = new ObjectEventMessage { Id = obj.Id, X = obj.X, Y = obj.Y, Width = obj.Width, Height = obj.Height };
                udpServer.BroadcastObjectEvent(NetMessageType.ObjectAdded, ev);
            };
            mapManager.Objects.ObjectUpdated += obj =>
            {
                var ev = new ObjectEventMessage { Id = obj.Id, X = obj.X, Y = obj.Y, Width = obj.Width, Height = obj.Height };
                udpServer.BroadcastObjectEvent(NetMessageType.ObjectUpdated, ev);
            };
            mapManager.Objects.ObjectDeleted += id =>
            {
                var ev = new ObjectEventMessage { Id = id, X = 0, Y = 0, Width = 0, Height = 0 };
                udpServer.BroadcastObjectEvent(NetMessageType.ObjectDeleted, ev);
            };

            // Запуск polling loop в отдельном потоке
            var cts = new CancellationTokenSource();
            Task.Run(() => udpServer.RunPollLoop(cts.Token));

            Console.WriteLine("Сервер запущен на порту 9050");

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

            Console.WriteLine();
            Console.WriteLine("Проверка регионов:");
            Console.WriteLine($"Тайл (2,2) принадлежит региону {regions.GetRegionId(2, 2)}");
            Console.WriteLine($"Тайл (9,9) принадлежит региону {regions.GetRegionId(9, 9)}");

            Console.WriteLine();
            Console.WriteLine("🔹 Регионы в области (0,0)-(8,2):");
            foreach (var reg in regions.GetRegionsInArea(0, 0, 8, 2))
                Console.WriteLine($" - {reg.Id}: {reg.Name}");


            mapManager.Objects.RemoveObject(house.Id);
            mapManager.Objects.RemoveObject(smallHouse.Id);

            cts.Cancel(); 
            udpServer.Dispose();

            Console.ReadLine();
        }
    }
}