using Moq;
using StackExchange.Redis;
using TileMap.Objects;
using TileMap.Surface;

namespace TileMap.Tests.Map
{
    public class MapLayerManagerTests
    {
        private IDatabase GetMockRedis()
        {
            var mockDb = new Mock<IDatabase>();
            mockDb.Setup(db => db.StringSet(It.IsAny<RedisKey>(), It.IsAny<RedisValue>(), null, When.Always, CommandFlags.None))
                  .Returns(true);
            mockDb.Setup(db => db.GeoAdd(It.IsAny<RedisKey>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<RedisValue>(), CommandFlags.None))
                  .Returns(true);
            mockDb.Setup(db => db.StringGet(It.IsAny<RedisKey>(), CommandFlags.None))
                  .Returns(RedisValue.Null);
            mockDb.Setup(db => db.KeyDelete(It.IsAny<RedisKey>(), CommandFlags.None))
                  .Returns(true);
            mockDb.Setup(db => db.SortedSetRemove(It.IsAny<RedisKey>(), It.IsAny<RedisValue>(), CommandFlags.None))
                  .Returns(true);
            return mockDb.Object;
        }

        [Fact]
        public void PlaceObject_Success()
        {
            var redis = GetMockRedis();
            var surface = new SurfaceLayer(10, 10, TileType.Plain);
            var manager = new MapLayerManager(surface, redis);

            var house = new GameObject("house_1", 2, 2, 3, 3);
            bool placed = manager.TryPlaceObject(house, TileType.Mountain);

            Assert.True(placed);
        }

        [Fact]
        public void PlaceObject_OutOfBounds_Fails()
        {
            var redis = GetMockRedis();
            var surface = new SurfaceLayer(10, 10, TileType.Plain);
            var manager = new MapLayerManager(surface, redis);

            var bigBuilding = new GameObject("big_building", 8, 8, 5, 5);
            bool placed = manager.TryPlaceObject(bigBuilding);

            Assert.False(placed);
        }

        [Fact]
        public void RemoveObject_Works()
        {
            var redis = GetMockRedis();
            var surface = new SurfaceLayer(10, 10, TileType.Plain);
            var manager = new MapLayerManager(surface, redis);

            var house = new GameObject("house_1", 2, 2, 3, 3);
            manager.TryPlaceObject(house);

            manager.Objects.RemoveObject("house_1");
            var objs = manager.GetObjectsInArea(2, 2, 4, 4);
            Assert.DoesNotContain(objs, o => o.Id == "house_1");
        }
    }
}
