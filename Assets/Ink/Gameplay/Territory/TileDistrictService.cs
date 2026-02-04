using System.Collections.Generic;
using UnityEngine;

namespace InkSim
{
    /// <summary>
    /// Unified district lookup service supporting tile overrides, priority zones, and AABB districts.
    /// Uses hybrid lookup: Tile Override > Priority Zone parent > AABB District.
    /// </summary>
    public class TileDistrictService : MonoBehaviour
    {
        public static TileDistrictService Instance { get; private set; }

        [SerializeField] private TileDistrictMap _tileOverrides = new TileDistrictMap();

        private List<DistrictDefinition> _districts = new List<DistrictDefinition>();
        private List<PriorityZone> _priorityZones = new List<PriorityZone>();
        private Dictionary<string, DistrictState> _stateCache = new Dictionary<string, DistrictState>();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        /// <summary>
        /// Initialize with districts and zones. Called by DistrictControlService or tests.
        /// </summary>
        public void Initialize(
            List<DistrictDefinition> districts,
            List<PriorityZone> zones = null,
            List<FactionDefinition> factions = null)
        {
            _districts = districts ?? new List<DistrictDefinition>();
            _priorityZones = zones ?? new List<PriorityZone>();

            // Sort districts by priority (descending) for correct overlap resolution
            _districts.Sort((a, b) => b.priority.CompareTo(a.priority));

            // Sort zones by priority (descending)
            _priorityZones.Sort((a, b) => b.priority.CompareTo(a.priority));

            // Create district states
            _stateCache.Clear();
            int factionCount = factions?.Count ?? 1;
            foreach (var district in _districts)
            {
                var state = new DistrictState(district, factionCount);
                _stateCache[district.id] = state;
            }
        }

        /// <summary>
        /// Link to DistrictControlService for state lookups.
        /// </summary>
        public void LinkStates(IReadOnlyList<DistrictState> states)
        {
            _stateCache.Clear();
            foreach (var state in states)
            {
                _stateCache[state.Id] = state;
            }
        }

        #region Core Lookup

        /// <summary>
        /// Get district state at position using hybrid lookup.
        /// Priority: Tile Override > Priority Zone parent > AABB District
        /// </summary>
        public DistrictState GetDistrictAt(int x, int y)
        {
            // 1. Check tile override
            string overrideId = _tileOverrides.GetAt(x, y);
            if (!string.IsNullOrEmpty(overrideId))
            {
                return GetStateById(overrideId);
            }

            // 2. Check priority zones (already sorted by priority)
            foreach (var zone in _priorityZones)
            {
                if (zone.Contains(x, y))
                {
                    // Return the parent district
                    return GetStateById(zone.parentDistrictId);
                }
            }

            // 3. Check AABB districts (already sorted by priority)
            foreach (var district in _districts)
            {
                if (district.Contains(x, y))
                {
                    return GetStateById(district.id);
                }
            }

            return null;
        }

        /// <summary>
        /// Get priority zone at position, if any.
        /// </summary>
        public PriorityZone GetPriorityZoneAt(int x, int y)
        {
            foreach (var zone in _priorityZones)
            {
                if (zone.Contains(x, y))
                    return zone;
            }
            return null;
        }

        /// <summary>
        /// Get district ID at position (lighter weight than full state).
        /// </summary>
        public string GetDistrictIdAt(int x, int y)
        {
            // 1. Tile override
            string overrideId = _tileOverrides.GetAt(x, y);
            if (!string.IsNullOrEmpty(overrideId))
                return overrideId;

            // 2. Priority zone
            foreach (var zone in _priorityZones)
            {
                if (zone.Contains(x, y))
                    return zone.parentDistrictId;
            }

            // 3. AABB
            foreach (var district in _districts)
            {
                if (district.Contains(x, y))
                    return district.id;
            }

            return null;
        }

        #endregion

        #region Tile Overrides

        /// <summary>
        /// Set explicit district for a tile, overriding all other sources.
        /// </summary>
        public void SetTileDistrict(int x, int y, string districtId)
        {
            _tileOverrides.SetAt(x, y, districtId);
        }

        /// <summary>
        /// Clear tile override, falling back to zone/AABB.
        /// </summary>
        public void ClearTileDistrict(int x, int y)
        {
            _tileOverrides.SetAt(x, y, null);
        }

        /// <summary>
        /// Set district for rectangular region.
        /// </summary>
        public void SetDistrictForRegion(int minX, int minY, int width, int height, string districtId)
        {
            for (int x = minX; x < minX + width; x++)
            {
                for (int y = minY; y < minY + height; y++)
                {
                    _tileOverrides.SetAt(x, y, districtId);
                }
            }
        }

        /// <summary>
        /// Clear all tile overrides in region.
        /// </summary>
        public void ClearDistrictForRegion(int minX, int minY, int width, int height)
        {
            for (int x = minX; x < minX + width; x++)
            {
                for (int y = minY; y < minY + height; y++)
                {
                    _tileOverrides.SetAt(x, y, null);
                }
            }
        }

        /// <summary>
        /// Check if tile has explicit override.
        /// </summary>
        public bool HasTileOverride(int x, int y)
        {
            return _tileOverrides.HasOverride(x, y);
        }

        #endregion

        #region Priority Zones

        /// <summary>
        /// Add a priority zone at runtime.
        /// </summary>
        public void AddPriorityZone(PriorityZone zone)
        {
            _priorityZones.Add(zone);
            _priorityZones.Sort((a, b) => b.priority.CompareTo(a.priority));
        }

        /// <summary>
        /// Remove a priority zone.
        /// </summary>
        public void RemovePriorityZone(string zoneId)
        {
            _priorityZones.RemoveAll(z => z.id == zoneId);
        }

        #endregion

        #region Economic Helpers

        /// <summary>
        /// Get economic multiplier at position (from priority zone or default 1.0).
        /// </summary>
        public float GetEconomicModifier(int x, int y)
        {
            var zone = GetPriorityZoneAt(x, y);
            return zone?.economicMultiplier ?? 1.0f;
        }

        /// <summary>
        /// Get tax modifier at position (from priority zone or default 0).
        /// </summary>
        public float GetTaxModifier(int x, int y)
        {
            var zone = GetPriorityZoneAt(x, y);
            return zone?.taxModifier ?? 0f;
        }

        /// <summary>
        /// Check if position is in a market zone.
        /// </summary>
        public bool IsMarketTile(int x, int y)
        {
            var zone = GetPriorityZoneAt(x, y);
            return zone != null && zone.isMarket;
        }

        /// <summary>
        /// Check if position is in a sanctuary zone.
        /// </summary>
        public bool IsSanctuaryTile(int x, int y)
        {
            var zone = GetPriorityZoneAt(x, y);
            return zone != null && zone.isSanctuary;
        }

        /// <summary>
        /// Check if position is in a guild hall zone.
        /// </summary>
        public bool IsGuildHallTile(int x, int y)
        {
            var zone = GetPriorityZoneAt(x, y);
            return zone != null && zone.isGuildHall;
        }

        #endregion

        #region Helpers

        private DistrictState GetStateById(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            return _stateCache.TryGetValue(id, out var state) ? state : null;
        }

        #endregion

        #region Serialization

        /// <summary>
        /// Get tile overrides for saving.
        /// </summary>
        public TileDistrictMap GetTileOverrides() => _tileOverrides;

        /// <summary>
        /// Load tile overrides from save data.
        /// </summary>
        public void LoadTileOverrides(TileDistrictMap saved)
        {
            _tileOverrides = saved ?? new TileDistrictMap();
            _tileOverrides.MarkDirty();
        }

        #endregion
    }
}
