using TileMap.Regions;

namespace TileMap.Tests.Region
{
    public class RegionLayerTests
    {
        [Fact]
        public void TileBelongsToRegion()
        {
            var regions = new RegionLayer(20, 20, 4);
            var id = regions.GetRegionId(5, 5);
            Assert.True(id > 0);
            Assert.True(regions.IsTileInRegion(5, 5, id));
        }

        [Fact]
        public void RegionsInArea_ReturnsUnique()
        {
            var regions = new RegionLayer(20, 20, 4);
            var list = regions.GetRegionsInArea(0, 0, 20, 20).ToList();
            Assert.Equal(4, list.Count);
        }

        [Fact]
        public void GetRegionById_Works()
        {
            var regions = new RegionLayer(20, 20, 4);
            var region = regions.GetRegionById(1);
            Assert.NotNull(region);
            Assert.Equal((ushort)1, region.Id);
        }
    }
}
