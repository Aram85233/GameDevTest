using MemoryPack;

namespace TileMap.Contracts.Dto
{
    [MemoryPackable]
    public partial class RegionDto
    {
        public ushort Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }
}
