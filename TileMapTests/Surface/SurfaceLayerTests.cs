using TileMap.Surface;

namespace TileMap.Tests.Surface
{
    public class SurfaceLayerTests
    {
        [Fact]
        public void Constructor_ShouldInitializeCorrectly()
        {
            var layer = new SurfaceLayer(5, 3);
            Assert.Equal(5, layer.Width);
            Assert.Equal(3, layer.Height);
            Assert.Equal(15, layer.Count);

            // Default tiles are Plain
            for (int y = 0; y < layer.Height; y++)
                for (int x = 0; x < layer.Width; x++)
                    Assert.Equal(TileType.Plain, layer.GetTile(x, y));
        }

        [Fact]
        public void Constructor_WithDefaultType_ShouldFillCorrectly()
        {
            var layer = new SurfaceLayer(2, 2, TileType.Mountain);
            for (int y = 0; y < layer.Height; y++)
                for (int x = 0; x < layer.Width; x++)
                    Assert.Equal(TileType.Mountain, layer.GetTile(x, y));
        }

        [Fact]
        public void GetTile_ShouldThrowForInvalidCoordinates()
        {
            var layer = new SurfaceLayer(2, 2);
            Assert.Throws<ArgumentOutOfRangeException>(() => layer.GetTile(-1, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => layer.GetTile(0, -1));
            Assert.Throws<ArgumentOutOfRangeException>(() => layer.GetTile(2, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => layer.GetTile(0, 2));
        }

        [Fact]
        public void TryGetTile_ShouldReturnFalseForInvalidCoordinates()
        {
            var layer = new SurfaceLayer(2, 2);
            Assert.False(layer.TryGetTile(-1, 0, out _));
            Assert.False(layer.TryGetTile(0, 2, out _));
        }

        [Fact]
        public void SetTile_ShouldUpdateTile()
        {
            var layer = new SurfaceLayer(2, 2);
            layer.SetTile(1, 1, TileType.Water);
            Assert.Equal(TileType.Water, layer.GetTile(1, 1));
        }

        [Fact]
        public void FromArray_ShouldCreateLayerFromTileArray()
        {
            var source = new TileType[]
            {
            TileType.Plain, TileType.Water,
            TileType.Mountain, TileType.Plain
            };
            var layer = SurfaceLayer.FromArray(2, 2, source);

            Assert.Equal(TileType.Plain, layer.GetTile(0, 0));
            Assert.Equal(TileType.Water, layer.GetTile(1, 0));
            Assert.Equal(TileType.Mountain, layer.GetTile(0, 1));
            Assert.Equal(TileType.Plain, layer.GetTile(1, 1));
        }

        [Fact]
        public void FillArea_ShouldFillCorrectTiles()
        {
            var layer = new SurfaceLayer(4, 4, TileType.Plain);
            layer.FillArea(1, 1, 2, 2, TileType.Mountain);

            for (int y = 0; y < 4; y++)
                for (int x = 0; x < 4; x++)
                {
                    if (x >= 1 && x <= 2 && y >= 1 && y <= 2)
                        Assert.Equal(TileType.Mountain, layer.GetTile(x, y));
                    else
                        Assert.Equal(TileType.Plain, layer.GetTile(x, y));
                }
        }

        [Fact]
        public void CanPlaceObjectsInArea_ShouldReturnCorrectly()
        {
            var layer = new SurfaceLayer(3, 3, TileType.Plain);
            layer.SetTile(1, 1, TileType.Mountain); // Assume Mountain cannot place objects

            Assert.True(layer.CanPlaceObjectsInArea(0, 0, 0, 0)); // Plain
            Assert.False(layer.CanPlaceObjectsInArea(0, 0, 2, 2)); // Contains Mountain
        }

        [Fact]
        public void EstimatedMemoryBytes_ShouldReturnCorrectSize()
        {
            var layer = new SurfaceLayer(5, 4);
            Assert.Equal(20, layer.EstimatedMemoryBytes());
        }
    }
}
