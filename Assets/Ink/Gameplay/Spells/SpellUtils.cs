using UnityEngine;
using System.Collections.Generic;

namespace InkSim
{
    /// <summary>
    /// Utility functions for spell casting, including line-of-sight checks.
    /// </summary>
    public static class SpellUtils
    {
        /// <summary>
        /// Traces a line from start to end and returns the first impassable tile hit.
        /// Uses Bresenham's line algorithm.
        /// Returns null if path is clear.
        /// </summary>
        public static Vector2Int? GetFirstBlockingTile(int startX, int startY, int endX, int endY, GridWorld gridWorld)
        {
            if (gridWorld == null) return null;
            
            // Get all tiles along the line
            var tiles = GetLineTiles(startX, startY, endX, endY);
            
            // Skip the first tile (caster's position)
            for (int i = 1; i < tiles.Count; i++)
            {
                var tile = tiles[i];
                
                // Check if this tile is impassable
                if (!gridWorld.IsWalkable(tile.x, tile.y))
                {
                    return tile;
                }
            }
            
            return null; // Path is clear
        }
        
        /// <summary>
        /// Traces a line and returns the last passable tile before hitting an obstacle.
        /// If the path is clear, returns the original target.
        /// </summary>
        public static Vector2Int GetAdjustedTarget(int startX, int startY, int endX, int endY, GridWorld gridWorld)
        {
            if (gridWorld == null) return new Vector2Int(endX, endY);
            
            var tiles = GetLineTiles(startX, startY, endX, endY);
            
            Vector2Int lastPassable = new Vector2Int(startX, startY);
            
            // Skip the first tile (caster's position)
            for (int i = 1; i < tiles.Count; i++)
            {
                var tile = tiles[i];
                
                if (!gridWorld.IsWalkable(tile.x, tile.y))
                {
                    // Hit a wall - return the last passable tile
                    return lastPassable;
                }
                
                lastPassable = tile;
            }
            
            // Path is clear, return original target
            return new Vector2Int(endX, endY);
        }
        
        /// <summary>
        /// Checks if there's a clear line of sight between two tiles.
        /// </summary>
        public static bool HasLineOfSight(int startX, int startY, int endX, int endY, GridWorld gridWorld)
        {
            return GetFirstBlockingTile(startX, startY, endX, endY, gridWorld) == null;
        }
        
        /// <summary>
        /// Gets all tiles along a line using Bresenham's algorithm.
        /// </summary>
        public static List<Vector2Int> GetLineTiles(int x0, int y0, int x1, int y1)
        {
            var tiles = new List<Vector2Int>();
            
            int dx = Mathf.Abs(x1 - x0);
            int dy = Mathf.Abs(y1 - y0);
            int sx = x0 < x1 ? 1 : -1;
            int sy = y0 < y1 ? 1 : -1;
            int err = dx - dy;
            
            int x = x0;
            int y = y0;
            
            while (true)
            {
                tiles.Add(new Vector2Int(x, y));
                
                if (x == x1 && y == y1)
                    break;
                
                int e2 = 2 * err;
                
                if (e2 > -dy)
                {
                    err -= dy;
                    x += sx;
                }
                
                if (e2 < dx)
                {
                    err += dx;
                    y += sy;
                }
            }
            
            return tiles;
        }
        
        /// <summary>
        /// Converts grid position to world position (center of tile).
        /// </summary>
        public static Vector3 GridToWorld(int gridX, int gridY)
        {
            return new Vector3(gridX + 0.5f, gridY + 0.5f, 0f);
        }
        
        /// <summary>
        /// Converts world position to grid position.
        /// </summary>
        public static Vector2Int WorldToGrid(Vector3 worldPos)
        {
            return new Vector2Int(Mathf.FloorToInt(worldPos.x), Mathf.FloorToInt(worldPos.y));
        }
    }
}
