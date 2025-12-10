using System.Linq.Expressions;
using TestGrand.Core.Models;

namespace TestGrand.Core.Services;

public interface IMapService
{
    public void CreateMap();
    public MapBase GetMap();
    public void MoveEntity(int x, int y, int newX, int newY, int entityId);
    public void AddEntity(int x, int y, MapEntity entity);
    public MapBlock GetMapBlock(int x, int y);
    public bool IsMapCreated();
    public void RemoveEntity(Guid guid);
}

public class MapService : IMapService
{
    private MapBase _map { get; set; } = new MapBase();
    private bool _isMapCreated = false;

    public void CreateMap()
    {
        string[] mapLayout =
        {
            "WWWWWWWWWWWWWWWWW",
            "WWW     W     WWW",
            "WWW     W     WWW",
            "W               W",
            "W       W       W",
            "W       W       W",
            "WWWW WWWWWWW WWWW",
            "W       W       W",
            "W               W",
            "W               W",
            "WWW     W     WWW",
            "WWW     W     WWW",
            "WWWWWWWWWWWWWWWWW"
        };

        var map = new MapBase
        {
            Id = 1,
            Width = mapLayout[0].Length,
            Height = mapLayout.Length,
            MapBlocks = new MapBlock[mapLayout.Length][]
        };

        for (int y = 0; y < map.Height; y++)
        {
            map.MapBlocks[y] = new MapBlock[map.Width];
            for (int x = 0; x < map.Width; x++)
            {
                char c = mapLayout[y][x];
                var newBlock = new MapBlock();

                switch (c)
                {
                    case ' ':
                        newBlock = new MapBlock
                        {
                            BlockType = MapBlockTypes.Floor,
                            IsPassable = true
                        };
                        break;

                    case 'W':
                        newBlock = new MapBlock
                        {
                            BlockType = MapBlockTypes.Wall,
                            IsPassable = false
                        };
                        break;

                    default:
                        newBlock = new MapBlock
                        {
                            BlockType = MapBlockTypes.Wall,
                            IsPassable = false
                        };
                        break;
                }

                map.MapBlocks[y][x] = newBlock;
            }
        }

        _map = map;
        _isMapCreated = true;
    }

    public MapBase GetMap()
    {
        return _map;
    }

    public bool IsMapCreated()
    {
        return _isMapCreated;
    }

    public void AddEntity(int x, int y, MapEntity entity)
    {
        var block = _map.MapBlocks[x][y];

        if (block.IsPassable)
            block.Entities.Add(entity);
    }

    public void MoveEntity(int x, int y, int newX, int newY, int entityId)
    {
        // var moveToBlock = _map.MapBlocks[newX][newY];

        // if (moveToBlock.IsPassable)
        // {
        //     var currentEntityBlock = _map.MapBlocks[x][y];

        //     var entity = currentEntityBlock.Entities.Where(x => x.Id == entityId).FirstOrDefault();

        //     if (entity != null)
        //     {
        //         currentEntityBlock.Entities.Remove(entity);
        //         moveToBlock.Entities.Add(entity);
        //     }
        // }
    }

    public MapBlock GetMapBlock(int x, int y)
    {
        return _map.MapBlocks[y][x];
    }

    public void RemoveEntity(Guid guid)
    {
        foreach (var blocksLine in _map.MapBlocks)
        {
            foreach (var block in blocksLine)
            {
                var entity = block.Entities.FirstOrDefault(x => x.Guid == guid);

                if (entity == null)
                    continue;

                block.Entities.Remove(entity);
            }
        }
    }
}
