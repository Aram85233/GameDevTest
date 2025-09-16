using Moq;
using StackExchange.Redis;
using TileMap.Objects;
using TileMap.Surface;

namespace TileMap.Tests.Objects
{
    public class ObjectLayerTests
    {
        private readonly Mock<IDatabase> _redisMock;
        private readonly SurfaceLayer _surface;
        private readonly ObjectLayer _layer;

        public ObjectLayerTests()
        {
            _redisMock = new Mock<IDatabase>();

            // Simple surface 20x20, all tiles allow placement
            _surface = new SurfaceLayer(20, 20, TileType.Plain);
            _layer = new ObjectLayer(_redisMock.Object, _surface);
        }

        [Fact]
        public void CanPlaceObject_ReturnsTrue_WhenObjectFits()
        {
            var obj = new GameObject("house1", 1, 1, 3, 3);
            bool canPlace = _layer.CanPlaceObject(obj);
            Assert.True(canPlace);
        }

        [Fact]
        public void CanPlaceObject_ReturnsFalse_WhenObjectOutOfBounds()
        {
            var obj = new GameObject("house2", 18, 18, 5, 5); // exceeds 20x20
            bool canPlace = _layer.CanPlaceObject(obj);
            Assert.False(canPlace);
        }

        [Fact]
        public void AddObject_StoresObjectInRedis_AndRaisesEvent()
        {
            var obj = new GameObject("house3", 2, 2, 3, 3);
            GameObject? createdObj = null;

            _layer.ObjectCreated += o => createdObj = o;

            _layer.AddObject(obj);

            _redisMock.Verify(r => r.GeoAdd(It.IsAny<RedisKey>(), It.IsAny<double>(), It.IsAny<double>(), obj.Id, It.IsAny<CommandFlags>()), Times.Once);

            Assert.Equal(obj, createdObj);
        }

        [Fact]
        public void GetObject_ReturnsObject_WhenExists()
        {
            var obj = new GameObject("house4", 3, 3, 2, 2);
            string json = System.Text.Json.JsonSerializer.Serialize(obj);

            _redisMock.Setup(r => r.StringGet($"game:object:{obj.Id}", It.IsAny<CommandFlags>())).Returns(json);

            var fetched = _layer.GetObject(obj.Id);

            Assert.NotNull(fetched);
            Assert.Equal(obj.Id, fetched!.Id);
            Assert.Equal(obj.X, fetched.X);
        }

        [Fact]
        public void RemoveObject_DeletesFromRedis_AndRaisesEvent()
        {
            var objId = "house5";
            string? deletedId = null;
            _layer.ObjectDeleted += id => deletedId = id;

            _layer.RemoveObject(objId);

            _redisMock.Verify(r => r.KeyDelete($"game:object:{objId}", It.IsAny<CommandFlags>()), Times.Once);
            _redisMock.Verify(r => r.SortedSetRemove(It.IsAny<RedisKey>(), objId, It.IsAny<CommandFlags>()), Times.Once);

            Assert.Equal(objId, deletedId);
        }

        [Fact]
        public void PlaceObjectOnSurface_FillsArea()
        {
            var obj = new GameObject("mountain1", 0, 0, 3, 3);
            _layer.PlaceObjectOnSurface(obj, TileType.Mountain);

            for (int x = 0; x < 3; x++)
                for (int y = 0; y < 3; y++)
                    Assert.Equal(TileType.Mountain, _surface.GetTile(x, y));
        }
    }
}
