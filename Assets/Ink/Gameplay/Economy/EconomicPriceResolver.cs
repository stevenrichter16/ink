using UnityEngine;

namespace InkSim
{
    /// <summary>
    /// Stub economic price resolver to satisfy TDD prerequisites.
    /// Future work will implement full modifier pipeline.
    /// </summary>
    public static class EconomicPriceResolver
    {
        /// <summary>
        /// Compute buy price (what the player pays). Currently returns base value * profile multiplier.
        /// </summary>
        public static int ResolveBuyPrice(string itemId, MerchantProfile profile, Vector2Int? position = null)
        {
            var data = ItemDatabase.Get(itemId);
            if (data == null || profile == null) return 0;
            float price = data.value * profile.buyMultiplier;

            // Apply palimpsest modifiers if position is known
            float priceMult = 1f;
            float tax = 0f;
            if (position.HasValue)
            {
                var rules = OverlayResolver.GetRulesAt(position.Value.x, position.Value.y);
                priceMult *= (rules.priceMultiplier == 0f ? 1f : rules.priceMultiplier);
                tax += rules.taxModifier;

                var state = DistrictControlService.Instance?.GetStateByPosition(position.Value.x, position.Value.y);
                if (state != null) priceMult *= Mathf.Max(0.01f, state.prosperity);

                tax += TaxRegistry.GetTax(state?.Id);
            }

            // Faction reputation modifier (friendlier = cheaper)
            priceMult *= GetReputationModifier(profile.factionId);

            // Supply/demand placeholder (returns 1 until implemented)
            priceMult *= GetSupplyModifier(position, itemId);

            price *= priceMult;
            price *= (1f + tax);
            return Mathf.Max(1, Mathf.RoundToInt(price));
        }

        /// <summary>
        /// Convenience overload for non-nullable positions.
        /// </summary>
        public static int ResolveBuyPrice(string itemId, MerchantProfile profile, Vector2Int position)
        {
            return ResolveBuyPrice(itemId, profile, (Vector2Int?)position);
        }

        /// <summary>
        /// Compute sell price (what player receives). Currently returns base value * profile multiplier.
        /// </summary>
        public static int ResolveSellPrice(string itemId, MerchantProfile profile, Vector2Int? position = null)
        {
            var data = ItemDatabase.Get(itemId);
            if (data == null || profile == null) return 0;
            float price = data.value * profile.sellMultiplier;

            float priceMult = 1f;
            if (position.HasValue)
            {
                var rules = OverlayResolver.GetRulesAt(position.Value.x, position.Value.y);
                priceMult *= (rules.priceMultiplier == 0f ? 1f : rules.priceMultiplier);

                var state = DistrictControlService.Instance?.GetStateByPosition(position.Value.x, position.Value.y);
                if (state != null) priceMult *= Mathf.Max(0.01f, state.prosperity);
            }

            priceMult *= GetReputationModifier(profile.factionId);
            priceMult *= GetSupplyModifier(position, itemId);

            price *= priceMult;
            return Mathf.Max(1, Mathf.RoundToInt(price));
        }

        /// <summary>
        /// Convenience overload for non-nullable positions.
        /// </summary>
        public static int ResolveSellPrice(string itemId, MerchantProfile profile, Vector2Int position)
        {
            return ResolveSellPrice(itemId, profile, (Vector2Int?)position);
        }

        private static float GetReputationModifier(string factionId)
        {
            if (string.IsNullOrEmpty(factionId)) return 1f;
            int rep = ReputationSystem.GetRep(factionId); // -100..100
            float t = Mathf.InverseLerp(-100, 100, rep); // 0..1
            return Mathf.Lerp(1.3f, 0.7f, t); // hostile 30% markup, friendly 30% discount
        }

        private static float GetSupplyModifier(Vector2Int? position, string itemId)
        {
            return SupplyService.GetSupply(position, itemId);
        }
    }
}
