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

        public bool Contains(int x, int y)
        {
            return x >= minX && x <= maxX && y >= minY && y <= maxY;
        }
    }
}
