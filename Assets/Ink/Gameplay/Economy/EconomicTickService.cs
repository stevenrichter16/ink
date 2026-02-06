using UnityEngine;

namespace InkSim
{
    /// <summary>
    /// Orchestrates the daily economic simulation: territory control, tax decay,
    /// demand event decay, supply/demand dynamics, prosperity updates, and tax revenue.
    /// Called by TurnManager every turnsPerDay turns.
    /// </summary>
    public static class EconomicTickService
    {
        /// <summary>Advance one in-game economic day for all systems.</summary>
        public static void AdvanceEconomicDay()
        {
            var dcs = DistrictControlService.Instance;
            if (dcs == null) return;

            // 1. Territory control tick (heat/patrol/control)
            dcs.AdvanceDay();

            // 2. Tax policy decay (remove expired policies)
            TaxRegistry.TickDecay();

            // 3. Demand event decay (remove expired events)
            EconomicEventService.TickDay();

            // 4. Per-district economic simulation
            for (int i = 0; i < dcs.States.Count; i++)
            {
                var state = dcs.States[i];
                UpdateSupply(state);
                UpdateProsperity(state, dcs);
                CollectTaxRevenue(state, dcs);
            }

            Debug.Log($"[EconomicTick] Economic day complete. Day={dcs.CurrentDay}");
        }

        /// <summary>
        /// Adjust supply levels: produced goods increase, consumed goods decrease.
        /// </summary>
        private static void UpdateSupply(DistrictState state)
        {
            var def = state.Definition;
            if (def == null) return;

            float pop = def.population;

            // Increase supply for produced goods
            if (def.producedGoods != null)
            {
                for (int i = 0; i < def.producedGoods.Count; i++)
                {
                    string itemId = def.producedGoods[i];
                    if (string.IsNullOrEmpty(itemId)) continue;
                    if (ItemDatabase.Get(itemId) == null) continue; // skip unknown items
                    float current = GetSupplyForState(state, itemId);
                    float delta = def.productionRate * pop;
                    float next = Mathf.Clamp(current + delta, 0.1f, 3f);
                    SetSupplyForState(state, itemId, next);
                }
            }

            // Decrease supply for consumed goods
            if (def.consumedGoods != null)
            {
                for (int i = 0; i < def.consumedGoods.Count; i++)
                {
                    string itemId = def.consumedGoods[i];
                    if (string.IsNullOrEmpty(itemId)) continue;
                    if (ItemDatabase.Get(itemId) == null) continue; // skip unknown items
                    float current = GetSupplyForState(state, itemId);
                    float delta = def.consumptionRate * pop;
                    float next = Mathf.Clamp(current - delta, 0.1f, 3f);
                    SetSupplyForState(state, itemId, next);
                }
            }
        }

        /// <summary>
        /// Update prosperity based on economic health: supply balance, faction control, and chaos.
        /// Lerps toward a health score at 10% per day.
        /// </summary>
        private static void UpdateProsperity(DistrictState state, DistrictControlService dcs)
        {
            // Calculate average supply health (1.0 if in sweet spot [0.5, 1.5])
            float avgSupplyHealth = 1f;
            if (state.itemSupply != null && state.itemSupply.Count > 0)
            {
                float totalHealth = 0f;
                int count = 0;
                foreach (var kvp in state.itemSupply)
                {
                    float s = kvp.Value;
                    float health;
                    if (s >= 0.5f && s <= 1.5f)
                        health = 1f;
                    else if (s < 0.5f)
                        health = Mathf.Max(0.2f, s / 0.5f); // Scale 0->0.5 to 0->1
                    else
                        health = Mathf.Max(0.5f, 1f - (s - 1.5f) / 1.5f); // Scale 1.5->3.0 to 1->0.5
                    totalHealth += health;
                    count++;
                }
                avgSupplyHealth = count > 0 ? totalHealth / count : 1f;
            }

            // Get controlling faction's control level and heat
            int ownerIdx = state.ControllingFactionIndex;
            float control = ownerIdx >= 0 ? state.control[ownerIdx] : 0.1f;
            float heat = ownerIdx >= 0 ? state.heat[ownerIdx] : 0.5f;

            // Composite health score
            float healthScore = (avgSupplyHealth * 0.4f) + (control * 0.3f) + ((1f - heat) * 0.3f);

            // Lerp prosperity toward health score
            state.prosperity = Mathf.Lerp(state.prosperity, healthScore, 0.1f);
            state.prosperity = Mathf.Clamp(state.prosperity, 0.1f, 2f);
        }

        /// <summary>
        /// Collect tax revenue based on district economic value, population, and controlling faction tax rate.
        /// </summary>
        private static void CollectTaxRevenue(DistrictState state, DistrictControlService dcs)
        {
            var def = state.Definition;
            if (def == null) return;

            // Get controlling faction's preferred tax rate
            float baseTaxRate = 0.10f; // default
            int ownerIdx = state.ControllingFactionIndex;
            if (ownerIdx >= 0 && ownerIdx < dcs.Factions.Count)
            {
                var faction = dcs.Factions[ownerIdx];
                if (faction != null && faction.economicPolicy != null)
                    baseTaxRate = faction.economicPolicy.preferredTaxRate;
            }

            // Revenue = economicValue * population * taxRate * (1 - corruption)
            float revenue = def.economicValue * def.population * baseTaxRate * (1f - state.corruption);
            state.treasury += Mathf.Max(0f, revenue);
        }

        private static float GetSupplyForState(DistrictState state, string itemId)
        {
            if (state.itemSupply != null && state.itemSupply.TryGetValue(itemId, out var level))
                return level;
            return 1f; // baseline
        }

        private static void SetSupplyForState(DistrictState state, string itemId, float value)
        {
            if (state.itemSupply == null)
                state.itemSupply = new System.Collections.Generic.Dictionary<string, float>();
            state.itemSupply[itemId] = value;

            // Also sync with SupplyService static storage
            SupplyService.SetSupply(state.Id, itemId, value);
        }
    }
}
