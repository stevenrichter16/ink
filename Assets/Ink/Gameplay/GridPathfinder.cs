using System.Collections.Generic;
using UnityEngine;

namespace InkSim
{
    /// <summary>
    /// Lightweight BFS pathfinder for grid-based AI movement.
    /// Returns the first-step direction toward a target, navigating around terrain obstacles.
    /// Uses reusable static collections to avoid per-call GC allocations.
    /// </summary>
    public static class GridPathfinder
    {
        private static readonly Queue<Vector2Int> _queue = new Queue<Vector2Int>(256);
        private static readonly Dictionary<long, long> _cameFrom = new Dictionary<long, long>(512);

        private static readonly Vector2Int[] _dirs =
        {
            Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right
        };

        /// <summary>
        /// BFS from (fromX,fromY) toward (toX,toY).
        /// Returns the direction of the first step on the shortest path,
        /// or Vector2Int.zero if no path exists within maxRadius tiles.
        /// Checks terrain walkability only (ignores entity occupancy so paths
        /// aren't blocked by other NPCs/enemies that will move).
        /// </summary>
        public static Vector2Int GetNextStep(int fromX, int fromY, int toX, int toY,
            GridWorld world, int maxRadius = 30)
        {
            if (world == null) return Vector2Int.zero;
            if (fromX == toX && fromY == toY) return Vector2Int.zero;

            // Quick Manhattan check — skip BFS if target is way out of range
            if (GridWorld.Distance(fromX, fromY, toX, toY) > maxRadius)
                return Vector2Int.zero;

            _queue.Clear();
            _cameFrom.Clear();

            long startKey = PackKey(fromX, fromY);
            long goalKey = PackKey(toX, toY);

            _queue.Enqueue(new Vector2Int(fromX, fromY));
            _cameFrom[startKey] = -1; // sentinel: start has no parent

            while (_queue.Count > 0)
            {
                Vector2Int current = _queue.Dequeue();
                long curKey = PackKey(current.x, current.y);

                if (curKey == goalKey)
                    return ReconstructFirstStep(goalKey, startKey);

                for (int i = 0; i < _dirs.Length; i++)
                {
                    int nx = current.x + _dirs[i].x;
                    int ny = current.y + _dirs[i].y;

                    // Bound check — stay within maxRadius of start
                    if (Mathf.Abs(nx - fromX) + Mathf.Abs(ny - fromY) > maxRadius)
                        continue;

                    long nKey = PackKey(nx, ny);
                    if (_cameFrom.ContainsKey(nKey))
                        continue; // already visited

                    // Target tile is always reachable (even if occupied by the target entity)
                    bool walkable = (nx == toX && ny == toY) || world.IsWalkable(nx, ny);
                    if (!walkable)
                        continue;

                    _cameFrom[nKey] = curKey;
                    _queue.Enqueue(new Vector2Int(nx, ny));
                }
            }

            // No path found
            return Vector2Int.zero;
        }

        /// <summary>
        /// Walk the cameFrom chain back from goal to start to find the first step direction.
        /// </summary>
        private static Vector2Int ReconstructFirstStep(long goalKey, long startKey)
        {
            long current = goalKey;
            long parent = _cameFrom[current];

            // Walk back until parent is start
            while (parent != startKey)
            {
                current = parent;
                parent = _cameFrom[current];
            }

            // current is now the first step from start
            int cx = UnpackX(current);
            int cy = UnpackY(current);
            int sx = UnpackX(startKey);
            int sy = UnpackY(startKey);

            return new Vector2Int(cx - sx, cy - sy);
        }

        /// <summary>Pack (x,y) into a single long for dictionary keys (avoids Vector2Int hashing overhead).</summary>
        private static long PackKey(int x, int y)
        {
            return ((long)x << 32) | (uint)y;
        }

        private static int UnpackX(long key) => (int)(key >> 32);
        private static int UnpackY(long key) => (int)(key & 0xFFFFFFFF);
    }
}
