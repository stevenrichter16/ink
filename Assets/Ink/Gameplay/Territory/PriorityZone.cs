using UnityEngine;

namespace InkSim
{
    /// <summary>
    /// A priority zone that can overlay base districts.
    /// Used for special areas like market squares, guild halls, temples.
    /// </summary>
    [CreateAssetMenu(menuName = "Ink/Territory/Priority Zone")]
    public class PriorityZone : ScriptableObject
    {
        [Header("Identity")]
        public string id;
        public string displayName;

        [Header("Bounds")]
        public int minX;
        public int maxX;
        public int minY;
        public int maxY;

        [Header("Priority")]
        [Tooltip("Higher priority zones take precedence. Base districts are priority 0.")]
        public int priority = 10;

        [Header("District Association")]
        [Tooltip("Which district this zone belongs to for economic purposes.")]
        public string parentDistrictId;

        [Header("Special Properties")]
        public bool isMarket;
        public bool isGuildHall;
        public bool isSanctuary;

        [Header("Economic Properties")]
        [Tooltip("Multiplier for economic transactions in this zone. 1.0 = normal, >1 = more expensive, <1 = cheaper.")]
        public float economicMultiplier = 1.0f;

        [Tooltip("Additional tax rate modifier for this zone. Added to base tax rate.")]
        public float taxModifier = 0f;

        /// <summary>
        /// Check if position is within this zone's bounds.
        /// </summary>
        public bool Contains(int x, int y)
        {
            return x >= minX && x <= maxX && y >= minY && y <= maxY;
        }

        /// <summary>
        /// Get the center of this zone.
        /// </summary>
        public Vector2Int Center => new Vector2Int((minX + maxX) / 2, (minY + maxY) / 2);

        /// <summary>
        /// Get the width of this zone.
        /// </summary>
        public int Width => maxX - minX + 1;

        /// <summary>
        /// Get the height of this zone.
        /// </summary>
        public int Height => maxY - minY + 1;
    }
}
