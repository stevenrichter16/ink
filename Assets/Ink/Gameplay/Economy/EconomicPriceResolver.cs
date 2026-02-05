using UnityEngine;

namespace InkSim
{
    /// <summary>
    /// Stub economic price resolver to satisfy TDD prerequisites.
    /// Future work will implement full modifier pipeline.
    /// </summary>
    public static class EconomicPriceResolver
    {
        public struct PriceBreakdown
        {
            public int baseValue;
            public float merchantMultiplier;
            public float priceMultiplier;
            public float tax;
            public float reputationMultiplier;
            public float supplyMultiplier;
            public float prosperityMultiplier;
            public int finalPrice;
        }

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
                if (!rules.taxEnforcementDisabled)
                    tax += rules.taxModifier;

                var state = DistrictControlService.Instance?.GetStateByPosition(position.Value.x, position.Value.y);
                if (state != null) priceMult *= Mathf.Max(0.01f, state.prosperity);

                if (!rules.taxEnforcementDisabled)
                {
                    var factionKey = string.IsNullOrEmpty(profile?.factionId) ? null : profile.factionId.ToLowerInvariant();
                    float baseTax = TaxRegistry.GetTax(state?.Id, profile?.factionId, itemId);
                    if (rules.taxExemptFactions != null && !string.IsNullOrEmpty(factionKey) &&
                        rules.taxExemptFactions.Contains(factionKey))
                        baseTax = 0f;
                    if (rules.taxDoubleFactions != null && !string.IsNullOrEmpty(factionKey) &&
                        rules.taxDoubleFactions.Contains(factionKey))
                        baseTax *= 2f;
                    tax += baseTax;
                }
            }

            // Faction reputation modifier (friendlier = cheaper)
            priceMult *= GetReputationModifier(profile.factionId);

            // Supply/demand modifier
            priceMult *= GetSupplyModifier(position, itemId);

            price *= priceMult;
            price *= (1f + tax);
            return Mathf.Max(1, Mathf.RoundToInt(price));
        }

        public static PriceBreakdown GetBuyBreakdown(string itemId, MerchantProfile profile, Vector2Int position)
        {
            var data = ItemDatabase.Get(itemId);
            PriceBreakdown bd = new PriceBreakdown
            {
                baseValue = data?.value ?? 0,
                merchantMultiplier = profile?.buyMultiplier ?? 0f,
                priceMultiplier = 1f,
                tax = 0f,
                reputationMultiplier = 1f,
                supplyMultiplier = 1f,
                prosperityMultiplier = 1f
            };

            if (data == null || profile == null)
            {
                bd.finalPrice = 0;
                return bd;
            }

            var rules = OverlayResolver.GetRulesAt(position.x, position.y);
            bd.priceMultiplier *= (rules.priceMultiplier == 0f ? 1f : rules.priceMultiplier);
            if (!rules.taxEnforcementDisabled)
                bd.tax += rules.taxModifier;

            var state = DistrictControlService.Instance?.GetStateByPosition(position.x, position.y);
            if (state != null)
            {
                bd.prosperityMultiplier = Mathf.Max(0.01f, state.prosperity);
                bd.priceMultiplier *= bd.prosperityMultiplier;
                if (!rules.taxEnforcementDisabled)
                {
                    var factionKey = string.IsNullOrEmpty(profile?.factionId) ? null : profile.factionId.ToLowerInvariant();
                    float baseTax = TaxRegistry.GetTax(state.Id, profile?.factionId, itemId);
                    if (rules.taxExemptFactions != null && !string.IsNullOrEmpty(factionKey) &&
                        rules.taxExemptFactions.Contains(factionKey))
                        baseTax = 0f;
                    if (rules.taxDoubleFactions != null && !string.IsNullOrEmpty(factionKey) &&
                        rules.taxDoubleFactions.Contains(factionKey))
                        baseTax *= 2f;
                    bd.tax += baseTax;
                }
            }

            bd.reputationMultiplier = GetReputationModifier(profile.factionId);
            bd.priceMultiplier *= bd.reputationMultiplier;

            bd.supplyMultiplier = GetSupplyModifier(position, itemId);
            bd.priceMultiplier *= bd.supplyMultiplier;

            float price = bd.baseValue;
            price *= bd.merchantMultiplier;
            price *= bd.priceMultiplier;
            price *= (1f + bd.tax);
            bd.finalPrice = Mathf.Max(1, Mathf.RoundToInt(price));
            return bd;
        }

        public static string FormatBreakdown(PriceBreakdown bd)
        {
            return $"Base:{bd.baseValue} Merchant:{bd.merchantMultiplier:0.00}x Mult:{bd.priceMultiplier:0.00}x Tax:{bd.tax*100:+0;-0;0}% Final:{bd.finalPrice}";
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
            float supplyRatio = 1f;
            float supplyModifier = 1f;
            float demandModifier = 1f;
            string districtId = null;

            if (position.HasValue)
            {
                var pos = position.Value;
                var rules = OverlayResolver.GetRulesAt(pos.x, pos.y);
                supplyModifier = rules.supplyModifier == 0f ? 1f : rules.supplyModifier;
                demandModifier = rules.demandModifier == 0f ? 1f : rules.demandModifier;

                var state = DistrictControlService.Instance?.GetStateByPosition(pos.x, pos.y);
                districtId = state?.Id;
            }

            supplyRatio = SupplyService.GetSupplyByDistrict(districtId, itemId);
            supplyRatio = Mathf.Max(0.01f, supplyRatio * supplyModifier);
            float supplyPrice = SupplyService.GetPriceModifierFromSupply(supplyRatio);
            float demandPrice = EconomicEventService.GetDemandMultiplier(itemId, districtId);

            return supplyPrice * demandModifier * demandPrice;
        }
    }
}
