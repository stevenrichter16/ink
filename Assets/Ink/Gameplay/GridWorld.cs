using UnityEngine;
using System.Collections.Generic;

namespace InkSim
{
    /// <summary>
    /// Manages the game grid - terrain, occupancy, items, and pathfinding.
    /// </summary>
    public class GridWorld : MonoBehaviour
    {
        public static GridWorld Instance { get; private set; }

        [Header("Grid Settings")]
        public int width = 24;
        public int height = 14;
        public float tileSize = 0.5f;

        private bool[,] _walkable;
        private GridEntity[,] _occupancy;
        private Item[,] _items;

        private void Awake()
        {
            Instance = this;
            // Don't initialize arrays here - wait for Initialize() call
        }
        
        /// <summary>
        /// Initialize grid arrays. Call after setting width/height.
        /// </summary>
        public void Initialize()
        {
            Debug.Log($"[GridWorld] Initialize({width}x{height})");
            _walkable = new bool[width, height];
            _occupancy = new GridEntity[width, height];
            _items = new Item[width, height];
            
            for (int x = 0; x < width; x++)
                for (int y = 0; y < height; y++)
                    _walkable[x, y] = true;
        }

public void SetWalkable(int x, int y, bool walkable)
        {
            if (_walkable != null && InBounds(x, y))
                _walkable[x, y] = walkable;
        }

public bool IsWalkable(int x, int y)
        {
            if (_walkable == null || !InBounds(x, y)) return false;
            return _walkable[x, y];
        }

public bool CanEnter(int x, int y)
        {
            if (_walkable == null || _occupancy == null || !InBounds(x, y)) return false;
            if (!_walkable[x, y]) return false;
            if (_occupancy[x, y] != null) return false;
            return true;
        }

public GridEntity GetEntityAt(int x, int y)
        {
            if (_occupancy == null || !InBounds(x, y)) return null;
            return _occupancy[x, y];
        }

public void SetOccupant(int x, int y, GridEntity entity)
        {
            if (_occupancy != null && InBounds(x, y))
                _occupancy[x, y] = entity;
        }

public void ClearOccupant(int x, int y)
        {
            if (_occupancy != null && InBounds(x, y))
                _occupancy[x, y] = null;
        }

        // === Item Methods ===
        
public void SetItem(int x, int y, Item item)
        {
            if (_items != null && InBounds(x, y))
                _items[x, y] = item;
        }

public void ClearItem(int x, int y)
        {
            if (_items != null && InBounds(x, y))
                _items[x, y] = null;
        }

public Item GetItemAt(int x, int y)
        {
            if (_items == null || !InBounds(x, y)) return null;
            return _items[x, y];
        }

        // === Utility ===

        public Vector3 GridToWorld(int x, int y)
        {
            return new Vector3(x * tileSize, y * tileSize, 0);
        }

        public Vector2Int WorldToGrid(Vector3 worldPos)
        {
            return new Vector2Int(
                Mathf.RoundToInt(worldPos.x / tileSize),
                Mathf.RoundToInt(worldPos.y / tileSize)
            );
        }

        public bool InBounds(int x, int y)
        {
            return x >= 0 && x < width && y >= 0 && y < height;
        }

        public static int Distance(int x1, int y1, int x2, int y2)
        {
            return Mathf.Abs(x2 - x1) + Mathf.Abs(y2 - y1);
        }

        public static Vector2Int DirectionToward(int fromX, int fromY, int toX, int toY)
        {
            int dx = toX - fromX;
            int dy = toY - fromY;
            
            if (Mathf.Abs(dx) > Mathf.Abs(dy))
                return new Vector2Int(dx > 0 ? 1 : -1, 0);
            else if (dy != 0)
                return new Vector2Int(0, dy > 0 ? 1 : -1);
            else if (dx != 0)
                return new Vector2Int(dx > 0 ? 1 : -1, 0);
            
            return Vector2Int.zero;
        }
    }
}
