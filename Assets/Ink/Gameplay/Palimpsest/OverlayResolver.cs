using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace InkSim
{
    /// <summary>
    /// Central lookup for active palimpsest layers. MVP: social overrides only.
    /// </summary>
    public static class OverlayResolver
    {
        private static readonly List<PalimpsestLayer> _layers = new List<PalimpsestLayer>();
        private static int _nextId = 1;
        private static PalimpsestTokenRegistry _registry;

        public struct PalimpsestRules
        {
            public bool truce;
            public string allyFactionId;
            public string huntFactionId;
            // Economic fields (TDD stub)
            public float taxModifier;
            public float priceMultiplier;
            public float supplyModifier;
            public float demandModifier;
            public HashSet<string> tradeBannedFactions;
            public HashSet<string> taxExemptFactions;
            public HashSet<string> taxDoubleFactions;
            public bool blackMarketAccess;
            public bool taxEnforcementDisabled;
            public bool tradeBlocked;
        }

        public static int RegisterLayer(PalimpsestLayer layer)
        {
            if (layer == null) return -1;
            layer.id = _nextId++;
            ParseTokens(layer);
            _layers.Add(layer);
            return layer.id;
        }

        public static void UnregisterLayer(int id)
        {
            _layers.RemoveAll(l => l.id == id);
        }

        public static void TickDecay()
        {
            for (int i = _layers.Count - 1; i >= 0; i--)
            {
                var l = _layers[i];
                if (l.turnsRemaining > 0)
                    l.turnsRemaining--;
                if (l.turnsRemaining == 0)
                    _layers.RemoveAt(i);
            }
        }

        public static PalimpsestRules GetRulesAt(int x, int y)
        {
            PalimpsestRules rules = default;
            rules.priceMultiplier = 1f;
            rules.supplyModifier = 1f;
            rules.demandModifier = 1f;
            rules.tradeBannedFactions = new HashSet<string>();
            rules.taxExemptFactions = new HashSet<string>();
            rules.taxDoubleFactions = new HashSet<string>();
            int bestPriority = int.MinValue;

            for (int i = 0; i < _layers.Count; i++)
            {
                var l = _layers[i];
                if (l.turnsRemaining == 0) continue;
                if (Vector2Int.Distance(new Vector2Int(x, y), l.center) > l.radius) continue;

                // Combine: truce is OR; ally/hunt take highest priority layer
                rules.truce |= l.truce;
                rules.taxModifier += l.taxDelta;
                rules.priceMultiplier *= (l.priceMultiplier == 0f ? 1f : l.priceMultiplier);
                rules.supplyModifier *= (l.supplyModifier == 0f ? 1f : l.supplyModifier);
                rules.demandModifier *= (l.demandModifier == 0f ? 1f : l.demandModifier);
                rules.blackMarketAccess |= l.enableBlackMarket;
                rules.taxEnforcementDisabled |= l.disableTaxEnforcement;
                rules.tradeBlocked |= l.blockTrade;

                if (l.tradeBannedFactions != null)
                {
                    for (int j = 0; j < l.tradeBannedFactions.Count; j++)
                        rules.tradeBannedFactions.Add(l.tradeBannedFactions[j]);
                }
                if (l.taxExemptFactions != null)
                {
                    for (int j = 0; j < l.taxExemptFactions.Count; j++)
                        rules.taxExemptFactions.Add(l.taxExemptFactions[j]);
                }
                if (l.taxDoubleFactions != null)
                {
                    for (int j = 0; j < l.taxDoubleFactions.Count; j++)
                        rules.taxDoubleFactions.Add(l.taxDoubleFactions[j]);
                }
                if (l.priority >= bestPriority)
                {
                    if (!string.IsNullOrEmpty(l.allyFactionId))
                        rules.allyFactionId = l.allyFactionId;
                    if (!string.IsNullOrEmpty(l.huntFactionId))
                        rules.huntFactionId = l.huntFactionId;
                    bestPriority = l.priority;
                }
            }

            return rules;
        }

        private static void ParseTokens(PalimpsestLayer layer)
        {
            layer.truce = false;
            layer.allyFactionId = null;
            layer.huntFactionId = null;
            layer.taxDelta = 0f;
            layer.priceMultiplier = 1f;
            layer.supplyModifier = 1f;
            layer.demandModifier = 1f;
            layer.targetItemId = null;
            layer.targetFactionId = null;
            layer.enableBlackMarket = false;
            layer.disableTaxEnforcement = false;
            layer.blockTrade = false;
            if (layer.tradeBannedFactions == null) layer.tradeBannedFactions = new List<string>();
            else layer.tradeBannedFactions.Clear();
            if (layer.taxExemptFactions == null) layer.taxExemptFactions = new List<string>();
            else layer.taxExemptFactions.Clear();
            if (layer.taxDoubleFactions == null) layer.taxDoubleFactions = new List<string>();
            else layer.taxDoubleFactions.Clear();

            EnsureRegistry();

            foreach (var t in layer.tokens)
            {
                if (string.IsNullOrEmpty(t)) continue;

                // Prefer registry; fallback to legacy parsing so we don't break if no asset exists.
                if (_registry != null && _registry.TryGetRule(t, out var rule))
                {
                    layer.truce |= rule.truce;
                    if (!string.IsNullOrEmpty(rule.allyFactionId))
                        layer.allyFactionId = rule.allyFactionId.ToLowerInvariant();
                    if (!string.IsNullOrEmpty(rule.huntFactionId))
                        layer.huntFactionId = rule.huntFactionId.ToLowerInvariant();
                    layer.taxDelta += rule.taxModifier;
                    if (rule.priceMultiplier > 0f)
                        layer.priceMultiplier *= rule.priceMultiplier;
                    if (rule.supplyModifier != 1f)
                        layer.supplyModifier *= rule.supplyModifier;
                    if (rule.demandModifier != 1f)
                        layer.demandModifier *= rule.demandModifier;
                    if (rule.blockTrade)
                        layer.blockTrade = true;
                    if (rule.enableBlackMarket)
                        layer.enableBlackMarket = true;
                    if (rule.disableTaxEnforcement)
                        layer.disableTaxEnforcement = true;
                    if (rule.clearTradeRestrictions)
                    {
                        layer.blockTrade = false;
                        layer.tradeBannedFactions.Clear();
                    }
                    continue;
                }

                var token = t.Trim();
                if (string.IsNullOrEmpty(token)) continue;
                var upper = token.ToUpperInvariant();
                if (upper == "TRUCE")
                {
                    layer.truce = true;
                }
                else if (upper.StartsWith("ALLY:"))
                {
                    layer.allyFactionId = token.Substring(5).ToLowerInvariant(); // store lower for ids
                }
                else if (upper.StartsWith("HUNT:"))
                {
                    layer.huntFactionId = token.Substring(5).ToLowerInvariant();
                }
                else if (upper.StartsWith("TAX:"))
                {
                    // TAX:+0.05 or TAX:-0.10
                    if (float.TryParse(token.Substring(4), NumberStyles.Float, CultureInfo.InvariantCulture, out var delta))
                        layer.taxDelta += delta;
                }
                else if (upper.StartsWith("PRICE:"))
                {
                    // PRICE:0.9 or PRICE:x0.9
                    var raw = token.Substring(6).TrimStart('X', 'x');
                    if (float.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out var mult) && mult > 0f)
                        layer.priceMultiplier *= mult;
                }
                else if (upper.StartsWith("TAX_BREAK:"))
                {
                    if (float.TryParse(token.Substring(10), NumberStyles.Float, CultureInfo.InvariantCulture, out var rate))
                        layer.taxDelta -= rate;
                }
                else if (upper.StartsWith("TAX_EXEMPT:"))
                {
                    var faction = token.Substring(11).ToLowerInvariant();
                    if (!string.IsNullOrEmpty(faction))
                        layer.taxExemptFactions.Add(faction);
                }
                else if (upper.StartsWith("TAX_DOUBLE:"))
                {
                    var faction = token.Substring(11).ToLowerInvariant();
                    if (!string.IsNullOrEmpty(faction))
                        layer.taxDoubleFactions.Add(faction);
                }
                else if (upper.StartsWith("SUBSIDY:"))
                {
                    // SUBSIDY:item:rate
                    var parts = token.Split(':');
                    if (parts.Length >= 3 && float.TryParse(parts[2], NumberStyles.Float, CultureInfo.InvariantCulture, out var rate))
                    {
                        layer.targetItemId = parts[1].ToLowerInvariant();
                        layer.priceMultiplier *= (1f - rate);
                    }
                }
                else if (upper.StartsWith("TARIFF:"))
                {
                    // TARIFF:item:rate
                    var parts = token.Split(':');
                    if (parts.Length >= 3 && float.TryParse(parts[2], NumberStyles.Float, CultureInfo.InvariantCulture, out var rate))
                    {
                        layer.targetItemId = parts[1].ToLowerInvariant();
                        layer.priceMultiplier *= (1f + rate);
                    }
                }
                else if (upper.StartsWith("INFLATE:"))
                {
                    if (float.TryParse(token.Substring(8), NumberStyles.Float, CultureInfo.InvariantCulture, out var rate))
                        layer.priceMultiplier *= (1f + rate);
                }
                else if (upper.StartsWith("DEFLATE:"))
                {
                    if (float.TryParse(token.Substring(8), NumberStyles.Float, CultureInfo.InvariantCulture, out var rate))
                        layer.priceMultiplier *= (1f - rate);
                }
                else if (upper.StartsWith("TRADE_BAN:"))
                {
                    var faction = token.Substring(10).ToLowerInvariant();
                    if (!string.IsNullOrEmpty(faction))
                        layer.tradeBannedFactions.Add(faction);
                }
                else if (upper.StartsWith("TRADE_ONLY:"))
                {
                    layer.targetFactionId = token.Substring(11).ToLowerInvariant();
                }
                else if (upper == "FREE_TRADE")
                {
                    layer.blockTrade = false;
                    layer.tradeBannedFactions.Clear();
                }
                else if (upper == "BLOCKADE")
                {
                    layer.blockTrade = true;
                }
                else if (upper.StartsWith("ABUNDANCE:"))
                {
                    layer.targetItemId = token.Substring(10).ToLowerInvariant();
                    layer.supplyModifier *= 2f;
                }
                else if (upper.StartsWith("SCARCITY:"))
                {
                    layer.targetItemId = token.Substring(9).ToLowerInvariant();
                    layer.supplyModifier *= 0.5f;
                }
                else if (upper.StartsWith("DEMAND_SPIKE:"))
                {
                    layer.targetItemId = token.Substring(13).ToLowerInvariant();
                    layer.demandModifier *= 2f;
                }
                else if (upper == "BLACK_MARKET_ACCESS")
                {
                    layer.enableBlackMarket = true;
                }
                else if (upper == "OFFICIAL_BLIND")
                {
                    layer.disableTaxEnforcement = true;
                }
            }
        }

        private static void EnsureRegistry()
        {
            if (_registry != null) return;
            // Try load a default registry from Resources/Palimpsest/PalimpsestTokenRegistry.asset
            _registry = Resources.Load<PalimpsestTokenRegistry>("Palimpsest/PalimpsestTokenRegistry");
        }

        /// <summary>
        /// Allow tests or bootstrap code to inject a registry explicitly.
        /// </summary>
        public static void SetRegistry(PalimpsestTokenRegistry registry)
        {
            _registry = registry;
        }
    }
}
