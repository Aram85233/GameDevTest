using TileMap.Objects;
using TileMap.Regions;

namespace TileMap.Networking
{
    public interface IMapQueryProvider
    {
        IEnumerable<GameObject> GetObjectsInArea(int x1, int y1, int x2, int y2);
        IEnumerable<Region> GetRegionsInArea(int x1, int y1, int x2, int y2);
    }
}
