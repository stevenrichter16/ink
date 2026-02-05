using System.Collections.Generic;
using UnityEngine;

namespace InkSim
{
    /// <summary>
    /// Registry for looking up OfficialDefinitions by ID or jurisdiction.
    /// Loads all officials from Resources/Officials on first access.
    /// </summary>
    public static class OfficialRegistry
    {
        private static Dictionary<string, OfficialDefinition> _byId;
        private static List<OfficialDefinition> _all;
        private static bool _initialized;

        private static void EnsureInitialized()
        {
            if (_initialized) return;
            _initialized = true;

            _byId = new Dictionary<string, OfficialDefinition>();
            _all = new List<OfficialDefinition>();

            var officials = Resources.LoadAll<OfficialDefinition>("Officials");
            foreach (var official in officials)
            {
                if (official == null) continue;
                _all.Add(official);

                if (!string.IsNullOrEmpty(official.id) && !_byId.ContainsKey(official.id))
                    _byId[official.id] = official;
            }

            Debug.Log($"[OfficialRegistry] Loaded {officials.Length} officials.");
        }

        public static OfficialDefinition GetById(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            EnsureInitialized();
            return _byId.TryGetValue(id, out var official) ? official : null;
        }

        public static List<OfficialDefinition> GetInDistrict(string districtId)
        {
            EnsureInitialized();
            var list = new List<OfficialDefinition>();
            if (string.IsNullOrEmpty(districtId)) return list;

            for (int i = 0; i < _all.Count; i++)
            {
                var o = _all[i];
                if (o != null && o.jurisdiction == OfficialJurisdiction.District &&
                    string.Equals(o.districtId, districtId, System.StringComparison.OrdinalIgnoreCase))
                {
                    list.Add(o);
                }
            }
            return list;
        }

        public static List<OfficialDefinition> GetByFaction(string factionId)
        {
            EnsureInitialized();
            var list = new List<OfficialDefinition>();
            if (string.IsNullOrEmpty(factionId)) return list;

            for (int i = 0; i < _all.Count; i++)
            {
                var o = _all[i];
                if (o != null && o.jurisdiction == OfficialJurisdiction.Faction &&
                    string.Equals(o.factionId, factionId, System.StringComparison.OrdinalIgnoreCase))
                {
                    list.Add(o);
                }
            }
            return list;
        }

        public static IEnumerable<OfficialDefinition> GetAll()
        {
            EnsureInitialized();
            return _all;
        }

        /// <summary>Clear cache (for editor/tests).</summary>
        public static void ClearCache()
        {
            _initialized = false;
            _byId = null;
            _all = null;
        }
    }
}
