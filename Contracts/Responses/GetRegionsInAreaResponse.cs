using MemoryPack;
using TileMap.Contracts.Dto;

namespace TileMap.Contracts.Responses
{
    [MemoryPackable]
    public partial class GetRegionsInAreaResponse
    {
        public List<RegionDto> Regions { get; set; } = new();
    }
}
