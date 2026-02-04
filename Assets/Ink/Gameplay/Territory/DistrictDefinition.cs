using System.Collections.Generic;
using UnityEngine;

namespace InkSim
{
    /// <summary>
    /// Static data for a city district. Supports AABB bounds and optional polygon vertices.
    /// </summary>
    [CreateAssetMenu(menuName = "Ink/Territory/District Definition", fileName = "DistrictDefinition")]
    public class DistrictDefinition : ScriptableObject
    {
        public string id = "district_id";
        public string displayName = "District";
        [Range(0f, 1f)] public float population = 0.5f;
        [Range(0f, 1f)] public float economicValue = 0.5f;

        [Header("Grid Bounds (AABB, inclusive)")]
        public int minX;
        public int maxX = 10;
        public int minY;
        public int maxY = 10;

        [Header("Advanced Bounds (Optional)")]
        [Tooltip("If set, uses polygon instead of AABB for Contains check.")]
        public List<Vector2Int> polygonVertices;

        [Tooltip("Priority for overlapping district resolution. Higher = wins.")]
        public int priority = 0;

        /// <summary>
        /// Check if position is within this district.
        /// Uses polygon if defined, otherwise AABB.
        /// </summary>
        public bool Contains(int x, int y)
        {
            if (polygonVertices != null && polygonVertices.Count >= 3)
                return IsPointInPolygon(x, y);

            // Original AABB check
            return x >= minX && x <= maxX && y >= minY && y <= maxY;
        }

        /// <summary>
        /// Ray casting algorithm for point-in-polygon test.
        /// </summary>
        private bool IsPointInPolygon(int x, int y)
        {
            bool inside = false;
            int count = polygonVertices.Count;

            for (int i = 0, j = count - 1; i < count; j = i++)
            {
                var vi = polygonVertices[i];
                var vj = polygonVertices[j];

                if ((vi.y > y) != (vj.y > y) &&
                    x < (vj.x - vi.x) * (y - vi.y) / (vj.y - vi.y) + vi.x)
                {
                    inside = !inside;
                }
            }

            return inside;
        }
    }
}
