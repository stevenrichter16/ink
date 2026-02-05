using System.Collections.Generic;
using UnityEngine;

namespace InkSim
{
    /// <summary>
    /// Simple per-district, per-item supply levels. Defaults to 1.0 (baseline).
    /// </summary>
    public static class SupplyService
    {
        // districtId -> itemId -> supply level (ratio vs baseline)
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
            var dcs = DistrictControlService.Instance;
            if (dcs != null)
            {
                var s = dcs.States;
                if (s != null)
                {
                    for (int i = 0; i < s.Count; i++)
                    {
                        if (s[i].Id == districtId)
                        {
                            var stateSupply = s[i].itemSupply;
                            if (stateSupply != null && stateSupply.TryGetValue(itemId, out var levelFromState))
                                return Mathf.Max(0.01f, levelFromState);
                            break;
                        }
                    }
                }
            }
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

            var dcs = DistrictControlService.Instance;
            if (dcs != null)
            {
                var states = dcs.States;
                if (states != null)
                {
                    for (int i = 0; i < states.Count; i++)
                    {
                        if (states[i].Id == districtId)
                        {
                            if (states[i].itemSupply == null)
                                states[i].itemSupply = new Dictionary<string, float>();
                            states[i].itemSupply[itemId] = items[itemId];
                            break;
                        }
                    }
                }
            }
        }

        public static void UpdateSupply(string districtId, string itemId, int delta)
        {
            if (string.IsNullOrEmpty(districtId) || string.IsNullOrEmpty(itemId)) return;
            float current = GetSupplyByDistrict(districtId, itemId);
            SetSupply(districtId, itemId, Mathf.Max(0.01f, current + delta));
        }

        /// <summary>
        /// Convert a supply ratio into a price modifier (0.5..2.0).
        /// </summary>
        public static float GetPriceModifierFromSupply(float supplyRatio)
        {
            if (supplyRatio < 0.25f) return 2.0f;
            if (supplyRatio < 0.5f) return 1.5f;
            if (supplyRatio < 0.75f) return 1.2f;
            if (supplyRatio < 1.25f) return 1.0f;
            if (supplyRatio < 1.5f) return 0.9f;
            if (supplyRatio < 2.0f) return 0.75f;
            return 0.5f;
        }

        public static void Clear()
        {
            _supply.Clear();
            var dcs = DistrictControlService.Instance;
            if (dcs != null && dcs.States != null)
            {
                for (int i = 0; i < dcs.States.Count; i++)
                {
                    if (dcs.States[i].itemSupply != null)
                        dcs.States[i].itemSupply.Clear();
                }
            }
        }
    }
}
