using System;
using System.Collections.Generic;
using UnityEngine;

namespace InkSim
{
    /// <summary>
    /// Stores per-tile district assignments with efficient lookup.
    /// Supports runtime modification and serialization.
    /// </summary>
    [Serializable]
    public class TileDistrictMap
    {
        // Serialized as list of entries for Unity serialization
        [SerializeField] private List<TileEntry> _entries = new List<TileEntry>();

        // Runtime dictionary for O(1) lookup
        [NonSerialized] private Dictionary<Vector2Int, string> _map;
        [NonSerialized] private bool _dirty = true;

        [Serializable]
        public struct TileEntry
        {
            public int x;
            public int y;
            public string districtId;

            public TileEntry(int x, int y, string districtId)
            {
                this.x = x;
                this.y = y;
                this.districtId = districtId;
            }

            public Vector2Int Position => new Vector2Int(x, y);
        }

        /// <summary>
        /// Get district ID at tile, or null if not explicitly assigned.
        /// </summary>
        public string GetAt(int x, int y)
        {
            EnsureBuilt();
            return _map.TryGetValue(new Vector2Int(x, y), out var id) ? id : null;
        }

        /// <summary>
        /// Set district ID for a specific tile.
        /// </summary>
        public void SetAt(int x, int y, string districtId)
        {
            EnsureBuilt();
            var pos = new Vector2Int(x, y);

            if (string.IsNullOrEmpty(districtId))
            {
                // Remove override
                _map.Remove(pos);
                _entries.RemoveAll(e => e.x == x && e.y == y);
            }
            else
            {
                _map[pos] = districtId;

                // Update or add entry
                int idx = _entries.FindIndex(e => e.x == x && e.y == y);
                if (idx >= 0)
                    _entries[idx] = new TileEntry(x, y, districtId);
                else
                    _entries.Add(new TileEntry(x, y, districtId));
            }
        }

        /// <summary>
        /// Remove all overrides for a district (e.g., when district is deleted).
        /// </summary>
        public void ClearDistrict(string districtId)
        {
            EnsureBuilt();
            var toRemove = new List<Vector2Int>();
            foreach (var kvp in _map)
            {
                if (kvp.Value == districtId)
                    toRemove.Add(kvp.Key);
            }
            foreach (var pos in toRemove)
                _map.Remove(pos);

            _entries.RemoveAll(e => e.districtId == districtId);
        }

        /// <summary>
        /// Get all tiles assigned to a specific district.
        /// </summary>
        public IEnumerable<Vector2Int> GetTilesForDistrict(string districtId)
        {
            EnsureBuilt();
            foreach (var kvp in _map)
            {
                if (kvp.Value == districtId)
                    yield return kvp.Key;
            }
        }

        /// <summary>
        /// Count of explicit tile overrides.
        /// </summary>
        public int Count
        {
            get
            {
                EnsureBuilt();
                return _map.Count;
            }
        }

        /// <summary>
        /// Check if a tile has an explicit override.
        /// </summary>
        public bool HasOverride(int x, int y)
        {
            EnsureBuilt();
            return _map.ContainsKey(new Vector2Int(x, y));
        }

        private void EnsureBuilt()
        {
            if (_map == null || _dirty)
            {
                _map = new Dictionary<Vector2Int, string>();
                foreach (var entry in _entries)
                {
                    _map[entry.Position] = entry.districtId;
                }
                _dirty = false;
            }
        }

        /// <summary>
        /// Force rebuild on next access (call after deserialization).
        /// </summary>
        public void MarkDirty()
        {
            _dirty = true;
        }

        /// <summary>
        /// Clear all overrides.
        /// </summary>
        public void Clear()
        {
            _entries.Clear();
            _map?.Clear();
        }

        /// <summary>
        /// Get internal entries list for serialization.
        /// </summary>
        public List<TileEntry> GetEntries() => _entries;
    }
}
