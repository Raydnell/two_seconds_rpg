using System.Runtime.InteropServices;
using System.Text.Json.Serialization;

namespace TestGrand.Core.Models;

public class MapBase
{
    public int Id { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
    public MapBlock[,] MapBlocks { get; set; }
    [JsonIgnore]
    public Dictionary<string, MapBlock> MapBlockByGuids { get; set; }
}

public class MapBlock
{
    public MapBlockTypes BlockType { get; set; }
    public bool IsPassable { get; set; }
    public List<MapEntity> Entities { get; set; } = new List<MapEntity>();
    public Guid BlockGuid { get; set; } = Guid.NewGuid();
}

public enum MapBlockTypes
{
    Wall,
    Floor,
    Fog,
}

public class MapEntity
{
    public Guid Guid { get; } = Guid.NewGuid();
    public MapEntityTypes Type { get; set; }
}

public enum MapEntityTypes
{
    Skeleton,
    Door,
    Woodstick
}