using System.Collections.Generic;
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
                    continue;
                }

                var token = t.Trim().ToUpperInvariant();
                if (token == "TRUCE")
                {
                    layer.truce = true;
                }
                else if (token.StartsWith("ALLY:"))
                {
                    layer.allyFactionId = token.Substring(5).ToLowerInvariant(); // store lower for ids
                }
                else if (token.StartsWith("HUNT:"))
                {
                    layer.huntFactionId = token.Substring(5).ToLowerInvariant();
                }
                else if (token.StartsWith("TAX:"))
                {
                    // TAX:+0.05 or TAX:-0.10
                    if (float.TryParse(token.Substring(4), out var delta))
                        layer.taxDelta += delta;
                }
                else if (token.StartsWith("PRICE:"))
                {
                    // PRICE:0.9 or PRICE:x0.9
                    var raw = token.Substring(6).TrimStart('X');
                    if (float.TryParse(raw, out var mult) && mult > 0f)
                        layer.priceMultiplier *= mult;
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
