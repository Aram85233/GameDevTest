namespace TileMap.Tests
{
    public class SurfaceLayerTests
    {
        [Fact]
        public void Create_Map_Has_Correct_Size_And_Defaults()
        {
            var map = new SurfaceLayer(1000, 1000);
            Assert.Equal(1000, map.Width);
            Assert.Equal(1000, map.Height);
            Assert.Equal(1000 * 1000, map.Count);
            Assert.Equal(TileType.Plain, map.GetTile(0, 0));
        }

        [Fact]
        public void Get_Set_Work_Ok_And_OOB_Throws()
        {
            var map = new SurfaceLayer(10, 10);
            map.SetTile(3, 4, TileType.Mountain);
            Assert.Equal(TileType.Mountain, map.GetTile(3, 4));
            Assert.Throws<ArgumentOutOfRangeException>(() => map.GetTile(-1, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => map.SetTile(0, 10, TileType.Plain));

            Assert.True(map.TryGetTile(3, 4, out var t));
            Assert.Equal(TileType.Mountain, t);
            Assert.False(map.TryGetTile(100, 100, out _));
        }

        [Fact]
        public void FillArea_Fills_Correctly()
        {
            var map = new SurfaceLayer(5, 5);
            map.FillArea(1, 1, 3, 3, TileType.Mountain);
            for (int y = 0; y < 5; y++)
                for (int x = 0; x < 5; x++)
                {
                    var expected = (x >= 1 && x <= 3 && y >= 1 && y <= 3) ? TileType.Mountain : TileType.Plain;
                    Assert.Equal(expected, map.GetTile(x, y));
                }
        }

        [Fact]
        public void FromArray_Creates_Map()
        {
            var src = new TileType[6] { TileType.Plain, TileType.Mountain, TileType.Plain, TileType.Mountain, TileType.Plain, TileType.Plain };
            var map = SurfaceLayer.FromArray(3, 2, src);
            Assert.Equal(TileType.Mountain, map.GetTile(1, 0));
            Assert.Equal(TileType.Plain, map.GetTile(1, 1));
        }

        [Fact]
        public void CanPlaceObjectsInArea_Returns_Correctly()
        {
            var map = new SurfaceLayer(4, 4);
            map.SetTile(1, 1, TileType.Mountain);
            Assert.False(map.CanPlaceObjectsInArea(0, 0, 2, 2));
            Assert.True(map.CanPlaceObjectsInArea(2, 2, 3, 3));
            Assert.False(map.CanPlaceObjectsInArea(-1, 0, 1, 1));
        }

        [Fact]
        public void Memory_Usage_Is_Under_8MB_For_1000x1000()
        {
            var map = new SurfaceLayer(1000, 1000);
            long bytes = map.EstimatedMemoryBytes();
            Assert.InRange(bytes, 1_000_000L, 8_388_608L);
        }
    }
}
