using System.Collections.Generic;
using UnityEngine;

namespace InkSim
{
    /// <summary>
    /// Static data for a city district. Minimal fields for the lean control loop.
    /// </summary>
    [CreateAssetMenu(menuName = "Ink/Territory/District Definition", fileName = "DistrictDefinition")]
    public class DistrictDefinition : ScriptableObject
    {
        public string id = "district_id";
        public string displayName = "District";
        [Range(0f, 1f)] public float population = 0.5f;
        [Range(0f, 1f)] public float economicValue = 0.5f;

        [Header("Optional grid bounds for lookup (inclusive)")]
        public int minX;
        public int maxX = 10;
        public int minY;
        public int maxY = 10;

        [Header("Economic Configuration")]
        [Tooltip("Items naturally produced by this district's economy")]
        public List<string> producedGoods = new List<string>();

        [Tooltip("Items consumed daily by population")]
        public List<string> consumedGoods = new List<string>();

        [Tooltip("Daily supply generation rate for produced goods (0-1)")]
        [Range(0f, 1f)] public float productionRate = 0.1f;

        [Tooltip("Daily consumption rate for consumed goods (0-1)")]
        [Range(0f, 1f)] public float consumptionRate = 0.05f;

        public bool Contains(int x, int y)
        {
            return x >= minX && x <= maxX && y >= minY && y <= maxY;
        }
    }
}
