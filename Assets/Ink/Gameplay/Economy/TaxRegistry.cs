using System.Collections.Generic;

namespace InkSim
{
    /// <summary>
    /// Per-jurisdiction tax policy registry. Returns 0 by default.
    /// </summary>
    public static class TaxRegistry
    {
        private const string DefaultPolicyId = "base_tax";
        private static readonly Dictionary<string, List<TaxPolicy>> _policiesByJurisdiction = new Dictionary<string, List<TaxPolicy>>();

        /// <summary>
        /// Legacy helper: set a single base sales tax for a district.
        /// </summary>
        public static void SetTax(string districtId, float taxRate)
        {
            if (string.IsNullOrEmpty(districtId)) return;
            RemovePolicy(DefaultPolicyId, districtId);
            AddPolicy(new TaxPolicy
            {
                id = DefaultPolicyId,
                type = TaxType.Sales,
                rate = taxRate,
                jurisdictionId = districtId,
                turnsRemaining = -1,
                exemptFactions = new List<string>(),
                exemptItems = new List<string>(),
                targetItems = new List<string>()
            });
        }

        /// <summary>
        /// Get total tax rate for a district, optionally filtered by faction or item.
        /// </summary>
        public static float GetTax(string districtId, string factionId = null, string itemId = null)
        {
            if (string.IsNullOrEmpty(districtId)) return 0f;
            float tax = 0f;
            var policies = GetPoliciesFor(districtId, factionId);
            for (int i = 0; i < policies.Count; i++)
            {
                var p = policies[i];
                if (p.turnsRemaining == 0) continue;
                if (!string.IsNullOrEmpty(factionId) && p.exemptFactions != null && p.exemptFactions.Contains(factionId))
                    continue;
                if (!string.IsNullOrEmpty(itemId))
                {
                    if (p.exemptItems != null && p.exemptItems.Contains(itemId))
                        continue;
                    if (p.targetItems != null && p.targetItems.Count > 0 && !p.targetItems.Contains(itemId))
                        continue;
                }
                tax += p.rate;
            }
            return tax;
        }

        /// <summary>
        /// Retrieve policies for a district, optionally including faction-level policies.
        /// </summary>
        public static List<TaxPolicy> GetPoliciesFor(string districtId, string factionId = null)
        {
            var result = new List<TaxPolicy>();
            if (!string.IsNullOrEmpty(districtId) && _policiesByJurisdiction.TryGetValue(districtId, out var districtPolicies))
            {
                for (int i = 0; i < districtPolicies.Count; i++)
                {
                    var p = districtPolicies[i];
                    if (p.turnsRemaining == 0) continue;
                    if (!string.IsNullOrEmpty(factionId) && p.exemptFactions != null && p.exemptFactions.Contains(factionId))
                        continue;
                    result.Add(p);
                }
            }

            if (!string.IsNullOrEmpty(factionId) && _policiesByJurisdiction.TryGetValue(factionId, out var factionPolicies))
            {
                for (int i = 0; i < factionPolicies.Count; i++)
                {
                    var p = factionPolicies[i];
                    if (p.turnsRemaining == 0) continue;
                    if (p.exemptFactions != null && p.exemptFactions.Contains(factionId))
                        continue;
                    result.Add(p);
                }
            }

            return result;
        }

        public static void AddPolicy(TaxPolicy policy)
        {
            if (string.IsNullOrEmpty(policy.jurisdictionId)) return;
            if (!_policiesByJurisdiction.TryGetValue(policy.jurisdictionId, out var list))
            {
                list = new List<TaxPolicy>();
                _policiesByJurisdiction[policy.jurisdictionId] = list;
            }
            list.Add(policy);
        }

        public static bool RemovePolicy(string policyId, string jurisdictionId = null)
        {
            if (string.IsNullOrEmpty(policyId)) return false;
            bool removed = false;

            if (!string.IsNullOrEmpty(jurisdictionId))
            {
                if (_policiesByJurisdiction.TryGetValue(jurisdictionId, out var list))
                {
                    int count = list.RemoveAll(p => p.id == policyId);
                    removed = count > 0;
                    if (list.Count == 0)
                        _policiesByJurisdiction.Remove(jurisdictionId);
                }
                return removed;
            }

            var keys = new List<string>(_policiesByJurisdiction.Keys);
            foreach (var key in keys)
            {
                var list = _policiesByJurisdiction[key];
                int count = list.RemoveAll(p => p.id == policyId);
                if (count > 0)
                {
                    removed = true;
                    if (list.Count == 0)
                        _policiesByJurisdiction.Remove(key);
                }
            }

            return removed;
        }

        public static void TickDecay()
        {
            var keys = new List<string>(_policiesByJurisdiction.Keys);
            foreach (var key in keys)
            {
                var list = _policiesByJurisdiction[key];
                for (int i = list.Count - 1; i >= 0; i--)
                {
                    var p = list[i];
                    if (p.turnsRemaining > 0)
                    {
                        p.turnsRemaining--;
                        if (p.turnsRemaining == 0)
                        {
                            list.RemoveAt(i);
                            continue;
                        }
                        list[i] = p;
                    }
                }
                if (list.Count == 0)
                    _policiesByJurisdiction.Remove(key);
            }
        }

        public static List<TaxPolicy> GetAllPolicies()
        {
            var result = new List<TaxPolicy>();
            foreach (var kvp in _policiesByJurisdiction)
            {
                result.AddRange(kvp.Value);
            }
            return result;
        }

        public static void Clear()
        {
            _policiesByJurisdiction.Clear();
        }
    }
}
