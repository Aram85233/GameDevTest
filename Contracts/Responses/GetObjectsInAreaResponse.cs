using MemoryPack;
using TileMap.Contracts.Dto;

namespace TileMap.Contracts.Responses
{
    [MemoryPackable]
    public partial class GetObjectsInAreaResponse
    {
        public List<GameObjectDto> Objects { get; set; } = new();
    }
}
