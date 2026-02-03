using System.Collections.Generic;
using UnityEngine;

namespace InkSim
{
    /// <summary>
    /// Simple per-district, per-item supply multiplier. Defaults to 1.0.
    /// </summary>
    public static class SupplyService
    {
        // districtId -> itemId -> supply multiplier
        private static readonly Dictionary<string, Dictionary<string, float>> _supply =
            new Dictionary<string, Dictionary<string, float>>();

        public static float GetSupply(Vector2Int? position, string itemId)
        {
            if (string.IsNullOrEmpty(itemId)) return 1f;
            string districtId = null;
            if (position.HasValue)
            {
                var state = DistrictControlService.Instance?.GetStateByPosition(position.Value.x, position.Value.y);
                districtId = state?.Id;
            }
            return GetSupplyByDistrict(districtId, itemId);
        }

        public static float GetSupplyByDistrict(string districtId, string itemId)
        {
            if (string.IsNullOrEmpty(districtId) || string.IsNullOrEmpty(itemId))
                return 1f;
            if (_supply.TryGetValue(districtId, out var items))
            {
                if (items.TryGetValue(itemId, out var level))
                    return Mathf.Max(0.01f, level);
            }
            return 1f;
        }

        public static void SetSupply(string districtId, string itemId, float level)
        {
            if (string.IsNullOrEmpty(districtId) || string.IsNullOrEmpty(itemId)) return;
            if (!_supply.TryGetValue(districtId, out var items))
            {
                items = new Dictionary<string, float>();
                _supply[districtId] = items;
            }
            items[itemId] = Mathf.Max(0.01f, level);
        }

        public static void Clear()
        {
            _supply.Clear();
        }
    }
}
