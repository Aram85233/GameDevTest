using StackExchange.Redis;
using TileMap.Surface;
using TileMap.Utils;

namespace TileMap.Objects;

public class ObjectLayer
{
    private readonly IDatabase _redis;
    private readonly SurfaceLayer _surface; // reference to surface

    private const string GeoKey = "game:objects";

    public event Action<GameObject>? ObjectCreated;
    public event Action<GameObject>? ObjectUpdated;
    public event Action<string>? ObjectDeleted;

    public ObjectLayer(IDatabase redis, SurfaceLayer surface)
    {
        _redis = redis;
        _surface = surface;
    }

    public bool CanPlaceObject(GameObject obj)
    {
        // Make sure object fits on map
        if (obj.X < 0 || obj.Y < 0 || obj.X + obj.Width > _surface.Width || obj.Y + obj.Height > _surface.Height)
            return false;

        // Check if surface allows object placement
        return _surface.CanPlaceObjectsInArea(obj.X, obj.Y, obj.X + obj.Width - 1, obj.Y + obj.Height - 1);
    }

    public void AddObject(GameObject obj)
    {
        if (!CanPlaceObject(obj))
            throw new InvalidOperationException("Cannot place object on this terrain.");

        // Convert to Redis coordinates
        (double lon, double lat) = CoordinateConverter.ToRedisCoordinates(obj.X, obj.Y);

        _redis.GeoAdd(GeoKey, lon, lat, obj.Id);
        _redis.StringSet($"game:object:{obj.Id}", System.Text.Json.JsonSerializer.Serialize(obj));

        ObjectCreated?.Invoke(obj);
    }

    public void PlaceObjectOnSurface(GameObject obj, TileType type = TileType.Mountain)
    {
        _surface.FillArea(obj.X, obj.Y, obj.X + obj.Width - 1, obj.Y + obj.Height - 1, type);
    }

    public GameObject? GetObject(string id)
    {
        var data = _redis.StringGet($"game:object:{id}");
        if (data.IsNullOrEmpty) return null;
        return System.Text.Json.JsonSerializer.Deserialize<GameObject>(data!);
    }

    public void RemoveObject(string id)
    {
        _redis.KeyDelete($"game:object:{id}");
        _redis.SortedSetRemove(GeoKey, id);

        ObjectDeleted?.Invoke(id);
    }

    public List<GameObject> GetObjectsInArea(int x1, int y1, int x2, int y2)
    {
        var results = new List<GameObject>();

        // Находим центр области
        int centerX = (x1 + x2) / 2;
        int centerY = (y1 + y2) / 2;

        (double lon, double lat) = CoordinateConverter.ToRedisCoordinates(centerX, centerY);

        // Радиус поиска в тайлах (берём максимальную сторону)
        int radius = Math.Max(Math.Abs(x2 - x1), Math.Abs(y2 - y1)) / 2;

        var nearby = _redis.GeoRadius(GeoKey, lon, lat, radius, GeoUnit.Kilometers);

        foreach (var entry in nearby)
        {
            var obj = GetObject(entry.Member!);
            if (obj != null &&
                obj.X + obj.Width >= x1 && obj.X <= x2 &&
                obj.Y + obj.Height >= y1 && obj.Y <= y2)
            {
                results.Add(obj);
            }
        }

        return results;
    }
}
