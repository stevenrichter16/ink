using System.Collections.Generic;
using UnityEngine;

namespace InkSim
{
    /// <summary>
    /// Registry for looking up FactionDefinitions by ID or name.
    /// Loads all factions from Resources/Factions on first access.
    /// </summary>
    public static class FactionRegistry
    {
        private static Dictionary<string, FactionDefinition> _byId;
        private static Dictionary<string, FactionDefinition> _byName;
        private static bool _initialized;

        private static void EnsureInitialized()
        {
            if (_initialized) return;
            _initialized = true;

            _byId = new Dictionary<string, FactionDefinition>();
            _byName = new Dictionary<string, FactionDefinition>();

            var factions = Resources.LoadAll<FactionDefinition>("Factions");
            foreach (var faction in factions)
            {
                if (faction == null) continue;

                if (!string.IsNullOrEmpty(faction.id) && !_byId.ContainsKey(faction.id))
                    _byId[faction.id] = faction;

                if (!string.IsNullOrEmpty(faction.displayName) && !_byName.ContainsKey(faction.displayName))
                    _byName[faction.displayName] = faction;

                // Also map by asset name (case insensitive)
                string assetName = faction.name;
                if (!string.IsNullOrEmpty(assetName))
                {
                    if (!_byName.ContainsKey(assetName))
                        _byName[assetName] = faction;
                    if (!_byName.ContainsKey(assetName.ToLowerInvariant()))
                        _byName[assetName.ToLowerInvariant()] = faction;
                }
            }

            Debug.Log($"[FactionRegistry] Loaded {factions.Length} factions.");
        }

        /// <summary>Get faction by its ID field.</summary>
        public static FactionDefinition GetById(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            EnsureInitialized();
            return _byId.TryGetValue(id, out var faction) ? faction : null;
        }

        /// <summary>Get faction by display name or asset name.</summary>
        public static FactionDefinition GetByName(string name)
        {
            if (string.IsNullOrEmpty(name)) return null;
            EnsureInitialized();
            
            // Try exact match first
            if (_byName.TryGetValue(name, out var faction))
                return faction;
            
            // Try lowercase match
            return _byName.TryGetValue(name.ToLowerInvariant(), out faction) ? faction : null;
        }

        /// <summary>Get all loaded factions.</summary>
        public static IEnumerable<FactionDefinition> GetAll()
        {
            EnsureInitialized();
            return _byId.Values;
        }

        /// <summary>Get faction by name, or create a runtime faction if none exists.</summary>
        public static FactionDefinition GetOrCreate(string name)
        {
            if (string.IsNullOrEmpty(name)) return null;
            EnsureInitialized();

            // Try to find existing faction
            var existing = GetByName(name);
            if (existing != null)
                return existing;

            // Create runtime faction
            var faction = ScriptableObject.CreateInstance<FactionDefinition>();
            faction.id = name;
            faction.displayName = name;
            faction.defaultReputation = 0;
            faction.name = name; // Asset name for consistency

            // Register in dictionaries
            _byId[name] = faction;
            _byName[name] = faction;
            _byName[name.ToLowerInvariant()] = faction;

            Debug.Log($"[FactionRegistry] Created runtime faction '{name}'");
            return faction;
        }

        
/// <summary>Clear cache (for editor/tests).</summary>
        public static void ClearCache()
        {
            _initialized = false;
            _byId = null;
            _byName = null;
        }
    }
}
