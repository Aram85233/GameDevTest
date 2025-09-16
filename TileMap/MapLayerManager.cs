using StackExchange.Redis;
using TileMap.Objects;
using TileMap.Surface;

namespace TileMap
{
    public class MapLayerManager
    {
        public SurfaceLayer Surface { get; }
        public ObjectLayer Objects { get; }

        public MapLayerManager(SurfaceLayer surface, IDatabase redis)
        {
            Surface = surface;
            Objects = new ObjectLayer(redis, surface);
        }


        public bool TryPlaceObject(GameObject obj, TileType? occupyTile = null)
        {
            if (!Surface.CanPlaceObjectsInArea(obj.X, obj.Y, obj.X + obj.Width - 1, obj.Y + obj.Height - 1))
                return false;

            var overlappingObjects = Objects.GetObjectsInArea(obj.X, obj.Y, obj.X + obj.Width - 1, obj.Y + obj.Height - 1);
            if (overlappingObjects.Count > 0) return false;

            Objects.AddObject(obj);

            if (occupyTile.HasValue)
                Surface.FillArea(obj.X, obj.Y, obj.X + obj.Width - 1, obj.Y + obj.Height - 1, occupyTile.Value);

            return true;
        }

        public bool CanPlaceObjectInArea(int x1, int y1, int x2, int y2)
        {
            if (!Surface.CanPlaceObjectsInArea(x1, y1, x2, y2)) return false;
            var objects = Objects.GetObjectsInArea(x1, y1, x2, y2);
            return objects.Count == 0;
        }

        public List<GameObject> GetObjectsInArea(int x1, int y1, int x2, int y2)
            => Objects.GetObjectsInArea(x1, y1, x2, y2);

        public void PrintMapWithObjects()
        {
            for (int y = 0; y < Surface.Height; y++)
            {
                for (int x = 0; x < Surface.Width; x++)
                {
                    var objHere = Objects.GetObjectsInArea(x, y, x, y).FirstOrDefault();
                    if (objHere != null)
                        Console.Write('O'); // object symbol
                    else
                        Console.Write(Surface.GetTile(x, y) switch
                        {
                            TileType.Plain => '.',
                            TileType.Mountain => '^',
                            TileType.Water => '~',
                            _ => '?'
                        });
                }
                Console.WriteLine();
            }
        }
    }

}
