using System.Collections.Generic;
using UnityEngine;

namespace InkSim
{
    /// <summary>
    /// Maps tokens (TRUCE, ALLY:faction_id, HUNT:faction_id, etc) to effects.
    /// Designers can extend the list in the inspector.
    /// </summary>
    [CreateAssetMenu(menuName = "Ink/Palimpsest/Token Registry", fileName = "PalimpsestTokenRegistry")]
    public class PalimpsestTokenRegistry : ScriptableObject
    {
        [System.Serializable]
        public class TokenRule
        {
            [Tooltip("Token text, e.g. TRUCE, ALLY:faction_inkbound, HUNT:faction_snake")]
            public string token;
            public bool truce;
            public string allyFactionId;
            public string huntFactionId;
            [Tooltip("Additive tax delta (e.g., 0.05 = +5% tax, -0.02 = -2%)")]
            public float taxModifier;
            [Tooltip("Price multiplier (1 = no change, 0.9 = 10% cheaper)")]
            public float priceMultiplier = 1f;
            [Tooltip("Supply multiplier (1 = no change, 2 = double supply)")]
            public float supplyModifier = 1f;
            [Tooltip("Demand multiplier (1 = no change, 2 = double demand)")]
            public float demandModifier = 1f;
            public bool blockTrade;
            public bool enableBlackMarket;
            public bool disableTaxEnforcement;
            [Tooltip("Clear all trade bans and blockades (FREE_TRADE effect)")]
            public bool clearTradeRestrictions;
        }

        [SerializeField] private List<TokenRule> rules = new List<TokenRule>();
        private Dictionary<string, TokenRule> _cache;

        private void OnEnable()
        {
            BuildCache();
        }

        private void BuildCache()
        {
            _cache = new Dictionary<string, TokenRule>();
            foreach (var r in rules)
            {
                if (r == null || string.IsNullOrWhiteSpace(r.token)) continue;
                var key = Normalize(r.token);
                _cache[key] = r;
            }
        }

        public bool TryGetRule(string token, out TokenRule rule)
        {
            if (_cache == null) BuildCache();
            return _cache.TryGetValue(Normalize(token), out rule);
        }

        // Test helper to inject rules without using inspector.
        public void ClearAndAddRule(TokenRule rule)
        {
            if (rule == null || string.IsNullOrWhiteSpace(rule.token)) return;
            rules.Clear();
            rules.Add(rule);
            BuildCache();
        }

        private static string Normalize(string token)
        {
            return token.Trim().ToUpperInvariant();
        }
    }
}
