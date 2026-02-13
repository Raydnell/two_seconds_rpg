using System;
using TestGrand.Core.Models;

namespace TestGrand.Core.Services;

public class PlayerViewService(IMapService mapService)
{
    public void CreatePlayerView(int playerX, int playerY)
    {
        var baseRangeView = 9;
        var playerViewMap = new MapBase
        {
            Width = baseRangeView,
            Height = baseRangeView,
            MapBlocks = new MapBlock[baseRangeView, baseRangeView],
        };


        playerViewMap.MapBlocks[3, 3] = mapService.GetMapBlock(playerX - 1, playerY - 1);
        playerViewMap.MapBlocks[3, 4] = mapService.GetMapBlock(playerX - 1, playerY);
        playerViewMap.MapBlocks[3, 5] = mapService.GetMapBlock(playerX - 1, playerY + 1);

        playerViewMap.MapBlocks[4, 3] = mapService.GetMapBlock(playerX, playerY - 1);
        playerViewMap.MapBlocks[4, 4] = mapService.GetMapBlock(playerX, playerY);
        playerViewMap.MapBlocks[4, 5] = mapService.GetMapBlock(playerX, playerY + 1);

        playerViewMap.MapBlocks[5, 3] = mapService.GetMapBlock(playerX + 1, playerY - 1);
        playerViewMap.MapBlocks[5, 4] = mapService.GetMapBlock(playerX + 1, playerY);
        playerViewMap.MapBlocks[5, 5] = mapService.GetMapBlock(playerX + 1, playerY + 1);

        var fogBlock = new MapBlock { BlockType = MapBlockTypes.Fog };

        playerViewMap.MapBlocks[4, 6] = mapService.GetMapBlock(playerX, playerY + 2);

        //horizont up
        if (playerViewMap.MapBlocks[4, 6].BlockType == MapBlockTypes.Wall)
        {
            playerViewMap.MapBlocks[4, 7] = fogBlock;
            playerViewMap.MapBlocks[4, 8] = fogBlock;
            playerViewMap.MapBlocks[3, 8] = fogBlock;
            playerViewMap.MapBlocks[5, 8] = fogBlock;
        }
        else
        {
            playerViewMap.MapBlocks[4, 7] = mapService.GetMapBlock(playerX, playerY + 3);

            if (playerViewMap.MapBlocks[4, 7].BlockType == MapBlockTypes.Wall)
            {
                playerViewMap.MapBlocks[4, 8] = fogBlock;
                playerViewMap.MapBlocks[3, 8] = fogBlock;
                playerViewMap.MapBlocks[5, 8] = fogBlock;
            }
            else
            {
                playerViewMap.MapBlocks[4, 8] = mapService.GetMapBlock(playerX, playerY + 4);
                playerViewMap.MapBlocks[3, 8] = mapService.GetMapBlock(playerX - 1, playerY + 4);
                playerViewMap.MapBlocks[5, 8] = mapService.GetMapBlock(playerX + 1, playerY + 4);
            }
        }

        //diagonal right up
        playerViewMap.MapBlocks[6, 2] = mapService.GetMapBlock(playerX + 2, playerY - 2);

        if (playerViewMap.MapBlocks[6, 2].BlockType == MapBlockTypes.Wall)
        {
            playerViewMap.MapBlocks[6, 1] = fogBlock;
            playerViewMap.MapBlocks[7, 2] = fogBlock;
            playerViewMap.MapBlocks[7, 1] = fogBlock;
            playerViewMap.MapBlocks[7, 0] = fogBlock;
            playerViewMap.MapBlocks[8, 0] = fogBlock;
            playerViewMap.MapBlocks[8, 1] = fogBlock;
        }
        else
        {
            playerViewMap.MapBlocks[6, 1] = mapService.GetMapBlock(playerX + 2, playerY - 3);
            playerViewMap.MapBlocks[7, 2] = mapService.GetMapBlock(playerX + 3, playerY - 2);
            playerViewMap.MapBlocks[7, 1] = mapService.GetMapBlock(playerX + 3, playerY - 3);

            if (playerViewMap.MapBlocks[7, 1].BlockType == MapBlockTypes.Wall)
            {
                playerViewMap.MapBlocks[7, 0] = fogBlock;
                playerViewMap.MapBlocks[8, 0] = fogBlock;
                playerViewMap.MapBlocks[8, 1] = fogBlock;
            }
            else
            {
                playerViewMap.MapBlocks[7, 0] = mapService.GetMapBlock(playerX + 3, playerY - 4);
                playerViewMap.MapBlocks[8, 0] = mapService.GetMapBlock(playerX + 4, playerY - 4);
                playerViewMap.MapBlocks[8, 1] = mapService.GetMapBlock(playerX + 4, playerY - 3);
            }
        }

        //22 degree, 1 am

        playerViewMap.MapBlocks[5, 2] = mapService.GetMapBlock(playerX + 1, playerY - 2);
        if (playerViewMap.MapBlocks[5, 2].BlockType == MapBlockTypes.Wall)
        {
            playerViewMap.MapBlocks[5, 1] = fogBlock;
            playerViewMap.MapBlocks[6, 0] = fogBlock;
        }
        else
        {
            playerViewMap.MapBlocks[5, 1] = mapService.GetMapBlock(playerX + 1, playerY - 3);

            if (playerViewMap.MapBlocks[5, 1].BlockType == MapBlockTypes.Wall)
            {
                playerViewMap.MapBlocks[6, 0] = fogBlock;
            }
            else
            {
                playerViewMap.MapBlocks[6, 0] = mapService.GetMapBlock(playerX + 2, playerY - 4);
            }
        }
    }
}

public class VisionCalculator
{
    private readonly IMapService _mapService;
    private readonly int _visionRadius = 4; // Радиус обзора (для 9x9 это 4 клетки от центра)
    
    public VisionCalculator(IMapService mapService)
    {
        _mapService = mapService;
    }
    
    public MapBase CreatePlayerView(int playerX, int playerY)
    {
        var size = _visionRadius * 2 + 1;
        var viewMap = new MapBase
        {
            Width = size,
            Height = size,
            MapBlocks = new MapBlock[size, size]
        };
        
        // Сначала заполняем туманом
        InitializeWithFog(viewMap);
        
        // Центр и ближайшие клетки всегда видимы
        SetCenterArea(viewMap, playerX, playerY);
        
        // Запускаем лучи во всех направлениях
        CastRays(viewMap, playerX, playerY);
        
        return viewMap;
    }
    
    private void InitializeWithFog(MapBase viewMap)
    {
        var fogBlock = new MapBlock { BlockType = MapBlockTypes.Fog };
        for (int x = 0; x < viewMap.Width; x++)
            for (int y = 0; y < viewMap.Height; y++)
                viewMap.MapBlocks[x, y] = fogBlock;
    }
    
    private void SetCenterArea(MapBase viewMap, int playerX, int playerY)
    {
        // Центральные 3x3 клетки всегда видимы
        for (int dx = -1; dx <= 1; dx++)
        {
            for (int dy = -1; dy <= 1; dy++)
            {
                var worldX = playerX + dx;
                var worldY = playerY + dy;
                var viewX = _visionRadius + dx;
                var viewY = _visionRadius + dy;
                
                if (IsValidCoordinate(worldX, worldY))
                {
                    viewMap.MapBlocks[viewX, viewY] = _mapService.GetMapBlock(worldX, worldY);
                }
            }
        }
    }
    
    private void CastRays(MapBase viewMap, int playerX, int playerY)
    {
        // Все направления для лучей
        var directions = new[]
        {
            // Прямые направления
            new Vector(0, 1), new Vector(0, -1), new Vector(1, 0), new Vector(-1, 0),
            // Диагонали
            new Vector(1, 1), new Vector(1, -1), new Vector(-1, 1), new Vector(-1, -1),
            // Промежуточные (22.5 градусов)
            new Vector(1, 2), new Vector(2, 1), new Vector(-1, 2), new Vector(-2, 1),
            new Vector(1, -2), new Vector(2, -1), new Vector(-1, -2), new Vector(-2, -1)
        };
        
        foreach (var dir in directions)
        {
            CastRay(viewMap, playerX, playerY, dir, normalizeDirection(dir));
        }
    }
    
    private void CastRay(MapBase viewMap, int startX, int startY, Vector direction, Vector step)
    {
        bool wallFound = false;
        
        for (int stepNum = 1; stepNum <= _visionRadius; stepNum++)
        {
            int worldX = startX + direction.X * stepNum;
            int worldY = startY + direction.Y * stepNum;
            
            if (!IsValidCoordinate(worldX, worldY))
                break;
            
            int viewX = _visionRadius + direction.X * stepNum;
            int viewY = _visionRadius + direction.Y * stepNum;
            
            if (!IsInViewBounds(viewX, viewY, viewMap))
                continue;
            
            if (wallFound)
            {
                viewMap.MapBlocks[viewX, viewY] = new MapBlock { BlockType = MapBlockTypes.Fog };
                continue;
            }
            
            var block = _mapService.GetMapBlock(worldX, worldY);
            viewMap.MapBlocks[viewX, viewY] = block;
            
            if (block.BlockType == MapBlockTypes.Wall)
            {
                wallFound = true;
                
                // Блокируем соседние клетки на этом шаге
                BlockAdjacentCells(viewMap, viewX, viewY, direction, step, stepNum);
            }
        }
    }
    
    private void BlockAdjacentCells(MapBase viewMap, int centerX, int centerY, Vector direction, Vector step, int stepNum)
    {
        // Блокируем соседние клетки в зависимости от направления
        var adjacentOffsets = GetAdjacentOffsets(direction, step);
        
        foreach (var offset in adjacentOffsets)
        {
            int adjX = centerX + offset.X;
            int adjY = centerY + offset.Y;
            
            if (IsInViewBounds(adjX, adjY, viewMap))
            {
                viewMap.MapBlocks[adjX, adjY] = new MapBlock { BlockType = MapBlockTypes.Fog };
            }
        }
    }
    
    private Vector normalizeDirection(Vector dir)
    {
        // Упрощаем направление для шагов (например, (2,1) -> (1,0) для горизонтали)
        return new Vector(
            dir.X == 0 ? 0 : dir.X / Math.Abs(dir.X),
            dir.Y == 0 ? 0 : dir.Y / Math.Abs(dir.Y)
        );
    }
    
    private List<Vector> GetAdjacentOffsets(Vector direction, Vector step)
    {
        // Возвращает соседние клетки для блокировки в зависимости от направления
        if (step.X != 0 && step.Y != 0) // Диагональ
        {
            return new List<Vector>
            {
                new Vector(step.X, 0),
                new Vector(0, step.Y)
            };
        }
        else if (step.X != 0) // Горизонталь
        {
            return new List<Vector>
            {
                new Vector(0, 1),
                new Vector(0, -1)
            };
        }
        else // Вертикаль
        {
            return new List<Vector>
            {
                new Vector(1, 0),
                new Vector(-1, 0)
            };
        }
    }
    
    private bool IsValidCoordinate(int x, int y)
    {
        return x >= 0 && x < _mapService.Width && y >= 0 && y < _mapService.Height;
    }
    
    private bool IsInViewBounds(int x, int y, MapBase viewMap)
    {
        return x >= 0 && x < viewMap.Width && y >= 0 && y < viewMap.Height;
    }
}

public struct Vector
{
    public int X { get; }
    public int Y { get; }
    
    public Vector(int x, int y)
    {
        X = x;
        Y = y;
    }
}