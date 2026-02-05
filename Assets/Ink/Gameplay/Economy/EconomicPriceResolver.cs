using System;
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
            if (!IsTradeAllowed(itemId, profile, position)) return 0;
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

            // Trade relation modifier
            priceMult *= GetTradeModifier(itemId, profile, position);

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
            if (!IsTradeAllowed(itemId, profile, position))
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

            bd.priceMultiplier *= GetTradeModifier(itemId, profile, position);

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
            if (!IsTradeAllowed(itemId, profile, position)) return 0;
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
            priceMult *= GetTradeModifier(itemId, profile, position);
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

        /// <summary>
        /// Check whether trade is allowed based on trade relations and palimpsest rules.
        /// </summary>
        public static bool IsTradeAllowed(string itemId, MerchantProfile profile, Vector2Int? position)
        {
            if (string.IsNullOrEmpty(itemId)) return true;
            if (profile == null) return false;

            string merchantFaction = profile.factionId;
            if (string.IsNullOrEmpty(merchantFaction)) return true;

            OverlayResolver.PalimpsestRules rules = default;
            DistrictState state = null;
            if (position.HasValue)
            {
                var pos = position.Value;
                rules = OverlayResolver.GetRulesAt(pos.x, pos.y);
                state = DistrictControlService.Instance?.GetStateByPosition(pos.x, pos.y);
            }

            if (rules.tradeBlocked) return false;
            if (rules.tradeBannedFactions != null)
            {
                var merchantKey = merchantFaction.ToLowerInvariant();
                if (rules.tradeBannedFactions.Contains(merchantKey))
                    return false;
                var districtFaction = GetControllingFactionId(state);
                if (!string.IsNullOrEmpty(districtFaction) && rules.tradeBannedFactions.Contains(districtFaction.ToLowerInvariant()))
                    return false;
            }

            string districtFactionId = GetControllingFactionId(state);
            if (string.IsNullOrEmpty(districtFactionId) ||
                string.Equals(districtFactionId, merchantFaction, StringComparison.OrdinalIgnoreCase))
                return true;

            var relation = TradeRelationRegistry.GetRelation(merchantFaction, districtFactionId);
            if (relation == null) return true;
            if (relation.status == TradeStatus.Embargo) return false;
            if (relation.bannedItems != null && relation.bannedItems.Contains(itemId)) return false;
            if (relation.status == TradeStatus.Exclusive &&
                relation.exclusiveItems != null &&
                relation.exclusiveItems.Count > 0 &&
                !relation.exclusiveItems.Contains(itemId))
                return false;

            return true;
        }

        private static float GetTradeModifier(string itemId, MerchantProfile profile, Vector2Int? position)
        {
            if (!IsTradeAllowed(itemId, profile, position)) return 0f;
            if (profile == null) return 1f;

            string merchantFaction = profile.factionId;
            if (string.IsNullOrEmpty(merchantFaction)) return 1f;

            DistrictState state = null;
            if (position.HasValue)
            {
                var pos = position.Value;
                state = DistrictControlService.Instance?.GetStateByPosition(pos.x, pos.y);
            }
            string districtFactionId = GetControllingFactionId(state);
            if (string.IsNullOrEmpty(districtFactionId) ||
                string.Equals(districtFactionId, merchantFaction, StringComparison.OrdinalIgnoreCase))
                return 1f;

            var relation = TradeRelationRegistry.GetRelation(merchantFaction, districtFactionId);
            if (relation == null) return 1f;

            float rate = Mathf.Max(0f, relation.tariffRate);
            switch (relation.status)
            {
                case TradeStatus.Alliance:
                case TradeStatus.Exclusive:
                    return Mathf.Clamp(1f - rate, 0.1f, 2f);
                case TradeStatus.Restricted:
                case TradeStatus.Open:
                default:
                    return 1f + rate;
            }
        }

        private static string GetControllingFactionId(DistrictState state)
        {
            if (state == null) return null;
            var dcs = DistrictControlService.Instance;
            if (dcs == null || dcs.Factions == null) return null;
            int idx = state.ControllingFactionIndex;
            if (idx < 0 || idx >= dcs.Factions.Count) return null;
            return dcs.Factions[idx].id;
        }
    }
}
