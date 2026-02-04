# Tile-Level District System Design

## Overview

This document describes a tile-level district system that extends the existing AABB-based districts with per-tile granularity. The design uses a **hybrid approach**: efficient rectangular bounds for large areas with tile-level overrides for irregular shapes, special zones, and dynamic changes.

---

## Table of Contents

1. [Architecture](#1-architecture)
2. [Data Structures](#2-data-structures)
3. [TDD Tests (Write First)](#3-tdd-tests-write-first)
4. [Implementation](#4-implementation)
5. [Integration Points](#5-integration-points)
6. [Serialization](#6-serialization)
7. [Economic System Integration](#7-economic-system-integration)

---

## 1. Architecture

### Hybrid Lookup Strategy

```
┌─────────────────────────────────────────────────────────────────┐
│                    DISTRICT LOOKUP FLOW                         │
├─────────────────────────────────────────────────────────────────┤
│                                                                  │
│   GetDistrictAt(x, y)                                           │
│          │                                                       │
│          ▼                                                       │
│   ┌──────────────────┐                                          │
│   │ Tile Override?   │──── YES ──► Return override district     │
│   │ (Dictionary)     │                                          │
│   └────────┬─────────┘                                          │
│            │ NO                                                  │
│            ▼                                                     │
│   ┌──────────────────┐                                          │
│   │ Priority Zones?  │──── YES ──► Return highest priority zone │
│   │ (Special areas)  │                                          │
│   └────────┬─────────┘                                          │
│            │ NO                                                  │
│            ▼                                                     │
│   ┌──────────────────┐                                          │
│   │ AABB Contains?   │──── YES ──► Return base district         │
│   │ (Existing rects) │                                          │
│   └────────┬─────────┘                                          │
│            │ NO                                                  │
│            ▼                                                     │
│        Return null (wilderness/no district)                     │
│                                                                  │
└─────────────────────────────────────────────────────────────────┘
```

### Layer Priority

| Layer | Purpose | Example |
|-------|---------|---------|
| **Tile Overrides** | Per-tile assignments, highest priority | Specific building tiles |
| **Priority Zones** | Special areas within districts | Market squares, guild halls |
| **Base Districts (AABB)** | Large rectangular regions | MarketRow, TempleWard |
| **Null** | Unassigned wilderness | Outside city walls |

---

## 2. Data Structures

### 2.1 TileDistrictMap

Core data structure for tile-level assignments:

```csharp
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
    }
}
```

### 2.2 PriorityZone

Special zones within or overlapping districts:

```csharp
using System;
using UnityEngine;

namespace InkSim
{
    /// <summary>
    /// A priority zone that can overlay base districts.
    /// Used for special areas like market squares, guild halls, temples.
    /// </summary>
    [CreateAssetMenu(menuName = "Ink/Territory/Priority Zone")]
    public class PriorityZone : ScriptableObject
    {
        [Header("Identity")]
        public string id;
        public string displayName;

        [Header("Bounds")]
        public int minX;
        public int maxX;
        public int minY;
        public int maxY;

        [Header("Priority")]
        [Tooltip("Higher priority zones take precedence. Base districts are priority 0.")]
        public int priority = 10;

        [Header("District Association")]
        [Tooltip("Which district this zone belongs to for economic purposes.")]
        public string parentDistrictId;

        [Header("Special Properties")]
        public bool isMarket;
        public bool isGuildHall;
        public bool isSanctuary;
        public float economicMultiplier = 1.0f;
        public float taxModifier = 0f;

        public bool Contains(int x, int y)
        {
            return x >= minX && x <= maxX && y >= minY && y <= maxY;
        }

        public Vector2Int Center => new Vector2Int((minX + maxX) / 2, (minY + maxY) / 2);
        public int Width => maxX - minX + 1;
        public int Height => maxY - minY + 1;
    }
}
```

### 2.3 Extended DistrictDefinition

Add optional polygon support to existing definition:

```csharp
// Add to existing DistrictDefinition.cs

[Header("Advanced Bounds (Optional)")]
[Tooltip("If set, uses polygon instead of AABB for Contains check.")]
public List<Vector2Int> polygonVertices;

[Tooltip("Priority for overlapping district resolution. Higher = wins.")]
public int priority = 0;

/// <summary>
/// Check if position is within this district.
/// Uses polygon if defined, otherwise AABB.
/// </summary>
public bool Contains(int x, int y)
{
    if (polygonVertices != null && polygonVertices.Count >= 3)
        return IsPointInPolygon(x, y);

    // Original AABB check
    return x >= minX && x <= maxX && y >= minY && y <= maxY;
}

private bool IsPointInPolygon(int x, int y)
{
    bool inside = false;
    int count = polygonVertices.Count;

    for (int i = 0, j = count - 1; i < count; j = i++)
    {
        var vi = polygonVertices[i];
        var vj = polygonVertices[j];

        if ((vi.y > y) != (vj.y > y) &&
            x < (vj.x - vi.x) * (y - vi.y) / (vj.y - vi.y) + vi.x)
        {
            inside = !inside;
        }
    }

    return inside;
}
```

---

## 3. TDD Tests (Write First)

### 3.1 TileDistrictMapTests.cs

```csharp
using NUnit.Framework;
using UnityEngine;
using InkSim;

namespace InkSim.Tests
{
    [TestFixture]
    public class TileDistrictMapTests
    {
        private TileDistrictMap _map;

        [SetUp]
        public void SetUp()
        {
            _map = new TileDistrictMap();
        }

        #region Basic Operations

        [Test]
        public void GetAt_ReturnsNull_WhenNoOverride()
        {
            Assert.IsNull(_map.GetAt(5, 5));
        }

        [Test]
        public void SetAt_ThenGetAt_ReturnsValue()
        {
            _map.SetAt(10, 20, "market_row");
            Assert.AreEqual("market_row", _map.GetAt(10, 20));
        }

        [Test]
        public void SetAt_MultipleTiles_EachReturnsCorrectValue()
        {
            _map.SetAt(0, 0, "district_a");
            _map.SetAt(1, 1, "district_b");
            _map.SetAt(2, 2, "district_c");

            Assert.AreEqual("district_a", _map.GetAt(0, 0));
            Assert.AreEqual("district_b", _map.GetAt(1, 1));
            Assert.AreEqual("district_c", _map.GetAt(2, 2));
        }

        [Test]
        public void SetAt_OverwriteExisting_ReturnsNewValue()
        {
            _map.SetAt(5, 5, "old_district");
            _map.SetAt(5, 5, "new_district");

            Assert.AreEqual("new_district", _map.GetAt(5, 5));
        }

        [Test]
        public void SetAt_NullOrEmpty_RemovesOverride()
        {
            _map.SetAt(5, 5, "some_district");
            Assert.IsNotNull(_map.GetAt(5, 5));

            _map.SetAt(5, 5, null);
            Assert.IsNull(_map.GetAt(5, 5));
        }

        [Test]
        public void SetAt_EmptyString_RemovesOverride()
        {
            _map.SetAt(5, 5, "some_district");
            _map.SetAt(5, 5, "");
            Assert.IsNull(_map.GetAt(5, 5));
        }

        #endregion

        #region HasOverride

        [Test]
        public void HasOverride_ReturnsFalse_WhenNotSet()
        {
            Assert.IsFalse(_map.HasOverride(10, 10));
        }

        [Test]
        public void HasOverride_ReturnsTrue_WhenSet()
        {
            _map.SetAt(10, 10, "district");
            Assert.IsTrue(_map.HasOverride(10, 10));
        }

        [Test]
        public void HasOverride_ReturnsFalse_AfterRemoval()
        {
            _map.SetAt(10, 10, "district");
            _map.SetAt(10, 10, null);
            Assert.IsFalse(_map.HasOverride(10, 10));
        }

        #endregion

        #region Count

        [Test]
        public void Count_ReturnsZero_WhenEmpty()
        {
            Assert.AreEqual(0, _map.Count);
        }

        [Test]
        public void Count_ReturnsCorrectCount_AfterAdds()
        {
            _map.SetAt(0, 0, "a");
            _map.SetAt(1, 1, "b");
            _map.SetAt(2, 2, "c");

            Assert.AreEqual(3, _map.Count);
        }

        [Test]
        public void Count_Decreases_AfterRemoval()
        {
            _map.SetAt(0, 0, "a");
            _map.SetAt(1, 1, "b");
            _map.SetAt(0, 0, null);

            Assert.AreEqual(1, _map.Count);
        }

        #endregion

        #region ClearDistrict

        [Test]
        public void ClearDistrict_RemovesAllTilesForDistrict()
        {
            _map.SetAt(0, 0, "target");
            _map.SetAt(1, 1, "target");
            _map.SetAt(2, 2, "other");

            _map.ClearDistrict("target");

            Assert.IsNull(_map.GetAt(0, 0));
            Assert.IsNull(_map.GetAt(1, 1));
            Assert.AreEqual("other", _map.GetAt(2, 2));
        }

        [Test]
        public void ClearDistrict_DoesNothing_WhenDistrictNotFound()
        {
            _map.SetAt(0, 0, "existing");

            Assert.DoesNotThrow(() => _map.ClearDistrict("nonexistent"));
            Assert.AreEqual("existing", _map.GetAt(0, 0));
        }

        #endregion

        #region GetTilesForDistrict

        [Test]
        public void GetTilesForDistrict_ReturnsAllMatchingTiles()
        {
            _map.SetAt(0, 0, "target");
            _map.SetAt(5, 5, "target");
            _map.SetAt(10, 10, "other");

            var tiles = new List<Vector2Int>(_map.GetTilesForDistrict("target"));

            Assert.AreEqual(2, tiles.Count);
            Assert.Contains(new Vector2Int(0, 0), tiles);
            Assert.Contains(new Vector2Int(5, 5), tiles);
        }

        [Test]
        public void GetTilesForDistrict_ReturnsEmpty_WhenNoMatches()
        {
            _map.SetAt(0, 0, "other");

            var tiles = new List<Vector2Int>(_map.GetTilesForDistrict("nonexistent"));

            Assert.AreEqual(0, tiles.Count);
        }

        #endregion

        #region Clear

        [Test]
        public void Clear_RemovesAllOverrides()
        {
            _map.SetAt(0, 0, "a");
            _map.SetAt(1, 1, "b");
            _map.SetAt(2, 2, "c");

            _map.Clear();

            Assert.AreEqual(0, _map.Count);
            Assert.IsNull(_map.GetAt(0, 0));
            Assert.IsNull(_map.GetAt(1, 1));
            Assert.IsNull(_map.GetAt(2, 2));
        }

        #endregion

        #region Negative Coordinates

        [Test]
        public void SetAt_NegativeCoordinates_Works()
        {
            _map.SetAt(-10, -20, "negative_district");
            Assert.AreEqual("negative_district", _map.GetAt(-10, -20));
        }

        [Test]
        public void SetAt_MixedCoordinates_Works()
        {
            _map.SetAt(-5, 10, "mixed");
            Assert.AreEqual("mixed", _map.GetAt(-5, 10));
        }

        #endregion
    }
}
```

### 3.2 TileDistrictServiceTests.cs

```csharp
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using InkSim;

namespace InkSim.Tests
{
    [TestFixture]
    public class TileDistrictServiceTests
    {
        private TileDistrictService _service;
        private DistrictDefinition _marketRow;
        private DistrictDefinition _templeWard;
        private PriorityZone _marketSquare;

        [SetUp]
        public void SetUp()
        {
            // Create test districts
            _marketRow = ScriptableObject.CreateInstance<DistrictDefinition>();
            _marketRow.id = "market_row";
            _marketRow.displayName = "Market Row";
            _marketRow.minX = 0;
            _marketRow.maxX = 20;
            _marketRow.minY = 0;
            _marketRow.maxY = 20;
            _marketRow.priority = 0;

            _templeWard = ScriptableObject.CreateInstance<DistrictDefinition>();
            _templeWard.id = "temple_ward";
            _templeWard.displayName = "Temple Ward";
            _templeWard.minX = 15;  // Overlaps with Market Row
            _templeWard.maxX = 30;
            _templeWard.minY = 0;
            _templeWard.maxY = 20;
            _templeWard.priority = 0;

            // Create priority zone
            _marketSquare = ScriptableObject.CreateInstance<PriorityZone>();
            _marketSquare.id = "market_square";
            _marketSquare.displayName = "Central Market Square";
            _marketSquare.parentDistrictId = "market_row";
            _marketSquare.minX = 8;
            _marketSquare.maxX = 12;
            _marketSquare.minY = 8;
            _marketSquare.maxY = 12;
            _marketSquare.priority = 10;
            _marketSquare.isMarket = true;
            _marketSquare.economicMultiplier = 1.5f;

            // Create service
            var go = new GameObject("TileDistrictService");
            _service = go.AddComponent<TileDistrictService>();
            _service.Initialize(
                new List<DistrictDefinition> { _marketRow, _templeWard },
                new List<PriorityZone> { _marketSquare }
            );
        }

        [TearDown]
        public void TearDown()
        {
            if (_service != null)
                Object.DestroyImmediate(_service.gameObject);
            Object.DestroyImmediate(_marketRow);
            Object.DestroyImmediate(_templeWard);
            Object.DestroyImmediate(_marketSquare);
        }

        #region Basic District Lookup

        [Test]
        public void GetDistrictAt_ReturnsBaseDistrict_WhenInBounds()
        {
            var result = _service.GetDistrictAt(5, 5);

            Assert.IsNotNull(result);
            Assert.AreEqual("market_row", result.Id);
        }

        [Test]
        public void GetDistrictAt_ReturnsNull_WhenOutOfBounds()
        {
            var result = _service.GetDistrictAt(100, 100);

            Assert.IsNull(result);
        }

        #endregion

        #region Tile Override Priority

        [Test]
        public void GetDistrictAt_TileOverride_TakesPrecedence()
        {
            // Tile at (5,5) is in Market Row by AABB
            _service.SetTileDistrict(5, 5, "temple_ward");

            var result = _service.GetDistrictAt(5, 5);

            Assert.AreEqual("temple_ward", result.Id);
        }

        [Test]
        public void GetDistrictAt_ClearTileOverride_FallsBackToAABB()
        {
            _service.SetTileDistrict(5, 5, "temple_ward");
            _service.ClearTileDistrict(5, 5);

            var result = _service.GetDistrictAt(5, 5);

            Assert.AreEqual("market_row", result.Id);
        }

        #endregion

        #region Priority Zone

        [Test]
        public void GetDistrictAt_PriorityZone_TakesPrecedence_OverAABB()
        {
            // (10, 10) is in Market Square priority zone
            var result = _service.GetDistrictAt(10, 10);

            // Should still return Market Row, but with zone info
            Assert.AreEqual("market_row", result.Id);

            var zone = _service.GetPriorityZoneAt(10, 10);
            Assert.IsNotNull(zone);
            Assert.AreEqual("market_square", zone.id);
        }

        [Test]
        public void GetPriorityZoneAt_ReturnsNull_OutsideZones()
        {
            var zone = _service.GetPriorityZoneAt(0, 0);

            Assert.IsNull(zone);
        }

        [Test]
        public void GetPriorityZoneAt_ReturnsHighestPriority_WhenOverlapping()
        {
            // Add a second overlapping zone with higher priority
            var vipZone = ScriptableObject.CreateInstance<PriorityZone>();
            vipZone.id = "vip_area";
            vipZone.minX = 9;
            vipZone.maxX = 11;
            vipZone.minY = 9;
            vipZone.maxY = 11;
            vipZone.priority = 20;  // Higher than market_square

            _service.AddPriorityZone(vipZone);

            var zone = _service.GetPriorityZoneAt(10, 10);

            Assert.AreEqual("vip_area", zone.id);

            Object.DestroyImmediate(vipZone);
        }

        #endregion

        #region Tile Override + Priority Zone

        [Test]
        public void GetDistrictAt_TileOverride_BeatsEverything()
        {
            // (10, 10) is in Market Square priority zone AND Market Row AABB
            _service.SetTileDistrict(10, 10, "temple_ward");

            var result = _service.GetDistrictAt(10, 10);

            Assert.AreEqual("temple_ward", result.Id);
        }

        #endregion

        #region Overlapping Districts (AABB)

        [Test]
        public void GetDistrictAt_OverlappingAABB_ReturnsHigherPriority()
        {
            // (17, 10) is in both Market Row and Temple Ward
            _templeWard.priority = 5;  // Higher than Market Row (0)

            var result = _service.GetDistrictAt(17, 10);

            Assert.AreEqual("temple_ward", result.Id);
        }

        [Test]
        public void GetDistrictAt_OverlappingAABB_SamePriority_ReturnsFirst()
        {
            // Both priority 0, returns first registered
            var result = _service.GetDistrictAt(17, 10);

            // First in list wins
            Assert.AreEqual("market_row", result.Id);
        }

        #endregion

        #region Economic Properties from Zone

        [Test]
        public void GetEconomicModifier_ReturnsZoneModifier_InZone()
        {
            float modifier = _service.GetEconomicModifier(10, 10);

            Assert.AreEqual(1.5f, modifier, 0.01f);
        }

        [Test]
        public void GetEconomicModifier_ReturnsDefault_OutsideZone()
        {
            float modifier = _service.GetEconomicModifier(0, 0);

            Assert.AreEqual(1.0f, modifier, 0.01f);
        }

        [Test]
        public void IsMarketTile_ReturnsTrue_InMarketZone()
        {
            Assert.IsTrue(_service.IsMarketTile(10, 10));
        }

        [Test]
        public void IsMarketTile_ReturnsFalse_OutsideMarketZone()
        {
            Assert.IsFalse(_service.IsMarketTile(0, 0));
        }

        #endregion

        #region Bulk Operations

        [Test]
        public void SetDistrictForRegion_SetsAllTilesInRegion()
        {
            _service.SetDistrictForRegion(0, 0, 2, 2, "temple_ward");

            Assert.AreEqual("temple_ward", _service.GetDistrictAt(0, 0).Id);
            Assert.AreEqual("temple_ward", _service.GetDistrictAt(1, 0).Id);
            Assert.AreEqual("temple_ward", _service.GetDistrictAt(0, 1).Id);
            Assert.AreEqual("temple_ward", _service.GetDistrictAt(1, 1).Id);
        }

        [Test]
        public void ClearDistrictForRegion_ClearsAllTilesInRegion()
        {
            _service.SetDistrictForRegion(0, 0, 2, 2, "temple_ward");
            _service.ClearDistrictForRegion(0, 0, 2, 2);

            // Should fall back to AABB
            Assert.AreEqual("market_row", _service.GetDistrictAt(0, 0).Id);
            Assert.AreEqual("market_row", _service.GetDistrictAt(1, 1).Id);
        }

        #endregion
    }
}
```

### 3.3 PolygonDistrictTests.cs

```csharp
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using InkSim;

namespace InkSim.Tests
{
    [TestFixture]
    public class PolygonDistrictTests
    {
        private DistrictDefinition _lShapedDistrict;

        [SetUp]
        public void SetUp()
        {
            // Create L-shaped district
            //   ████
            //   █
            //   █
            //   █
            _lShapedDistrict = ScriptableObject.CreateInstance<DistrictDefinition>();
            _lShapedDistrict.id = "l_district";
            _lShapedDistrict.polygonVertices = new List<Vector2Int>
            {
                new Vector2Int(0, 0),   // Bottom-left
                new Vector2Int(1, 0),   // Bottom-right of stem
                new Vector2Int(1, 3),   // Top-right of stem
                new Vector2Int(4, 3),   // Top-right of horizontal
                new Vector2Int(4, 4),   // Far top-right
                new Vector2Int(0, 4),   // Far top-left
            };
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_lShapedDistrict);
        }

        [Test]
        public void Contains_InsideLShape_ReturnsTrue()
        {
            // Inside the vertical stem
            Assert.IsTrue(_lShapedDistrict.Contains(0, 1));

            // Inside the horizontal part
            Assert.IsTrue(_lShapedDistrict.Contains(2, 3));
        }

        [Test]
        public void Contains_OutsideLShape_ReturnsFalse()
        {
            // The "notch" in the L (should be outside)
            Assert.IsFalse(_lShapedDistrict.Contains(2, 1));
            Assert.IsFalse(_lShapedDistrict.Contains(3, 0));
        }

        [Test]
        public void Contains_OnBoundary_ReturnsTrue()
        {
            // Vertices
            Assert.IsTrue(_lShapedDistrict.Contains(0, 0));
            Assert.IsTrue(_lShapedDistrict.Contains(4, 4));
        }

        [Test]
        public void Contains_FarOutside_ReturnsFalse()
        {
            Assert.IsFalse(_lShapedDistrict.Contains(100, 100));
            Assert.IsFalse(_lShapedDistrict.Contains(-10, -10));
        }

        [Test]
        public void Contains_FallsBackToAABB_WhenNoPolygon()
        {
            var aabbDistrict = ScriptableObject.CreateInstance<DistrictDefinition>();
            aabbDistrict.minX = 0;
            aabbDistrict.maxX = 10;
            aabbDistrict.minY = 0;
            aabbDistrict.maxY = 10;
            aabbDistrict.polygonVertices = null;  // No polygon

            Assert.IsTrue(aabbDistrict.Contains(5, 5));
            Assert.IsFalse(aabbDistrict.Contains(15, 15));

            Object.DestroyImmediate(aabbDistrict);
        }

        [Test]
        public void Contains_FallsBackToAABB_WhenPolygonTooSmall()
        {
            var badPolygon = ScriptableObject.CreateInstance<DistrictDefinition>();
            badPolygon.minX = 0;
            badPolygon.maxX = 10;
            badPolygon.minY = 0;
            badPolygon.maxY = 10;
            badPolygon.polygonVertices = new List<Vector2Int>
            {
                new Vector2Int(0, 0),
                new Vector2Int(1, 1),
                // Only 2 vertices - not a valid polygon
            };

            // Should use AABB fallback
            Assert.IsTrue(badPolygon.Contains(5, 5));

            Object.DestroyImmediate(badPolygon);
        }
    }
}
```

---

## 4. Implementation

### 4.1 TileDistrictService.cs

```csharp
using System.Collections.Generic;
using UnityEngine;

namespace InkSim
{
    /// <summary>
    /// Unified district lookup service supporting tile overrides, priority zones, and AABB districts.
    /// Replaces direct DistrictControlService position lookups for maximum flexibility.
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

        /// <summary>
        /// Initialize with districts and zones. Called by DistrictControlService or tests.
        /// </summary>
        public void Initialize(
            List<DistrictDefinition> districts,
            List<PriorityZone> zones = null)
        {
            _districts = districts ?? new List<DistrictDefinition>();
            _priorityZones = zones ?? new List<PriorityZone>();

            // Sort districts by priority (descending) for correct overlap resolution
            _districts.Sort((a, b) => b.priority.CompareTo(a.priority));

            // Sort zones by priority (descending)
            _priorityZones.Sort((a, b) => b.priority.CompareTo(a.priority));
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
```

---

## 5. Integration Points

### 5.1 DistrictControlService Integration

```csharp
// In DistrictControlService.Bootstrap(), after loading districts:

private TileDistrictService _tileService;

private void Bootstrap()
{
    // ... existing code ...

    // Initialize tile district service
    _tileService = gameObject.AddComponent<TileDistrictService>();
    _tileService.Initialize(_districtDefs, LoadPriorityZones());
    _tileService.LinkStates(_states);
}

private List<PriorityZone> LoadPriorityZones()
{
    return new List<PriorityZone>(Resources.LoadAll<PriorityZone>("PriorityZones"));
}

// Replace existing GetStateByPosition:
public DistrictState GetStateByPosition(int x, int y)
{
    return _tileService.GetDistrictAt(x, y);
}
```

### 5.2 Economic System Integration

```csharp
// In EconomicPriceResolver:

public static float GetDistrictModifier(int x, int y)
{
    var tileService = TileDistrictService.Instance;
    if (tileService == null) return 1.0f;

    // Get zone economic multiplier
    float zoneMultiplier = tileService.GetEconomicModifier(x, y);

    // Get district prosperity
    var state = tileService.GetDistrictAt(x, y);
    float prosperityMultiplier = state != null ? GetProsperityModifier(state) : 1.0f;

    return zoneMultiplier * prosperityMultiplier;
}

public static float GetEffectiveTaxRate(int x, int y)
{
    float baseTax = 0.1f;  // Default 10%

    var tileService = TileDistrictService.Instance;
    if (tileService == null) return baseTax;

    // Add zone tax modifier
    baseTax += tileService.GetTaxModifier(x, y);

    // Add palimpsest modifiers
    var rules = OverlayResolver.GetRulesAt(x, y);
    baseTax += rules.taxModifier;

    return Mathf.Clamp(baseTax, 0f, 0.5f);
}
```

### 5.3 Palimpsest Integration

```csharp
// In InscribableSurface:

public void OnLayerRegistered(PalimpsestLayer layer)
{
    // Get district for heat tracking
    var state = TileDistrictService.Instance?.GetDistrictAt(
        (int)transform.position.x,
        (int)transform.position.y
    );

    if (state != null)
    {
        DistrictControlService.Instance.ApplyPalimpsestEdit(state.Id, 1f);
    }
}
```

---

## 6. Serialization

### 6.1 Save Data Structure

```csharp
[Serializable]
public class TileDistrictSaveData
{
    public List<TileDistrictMap.TileEntry> tileOverrides;
    public List<RuntimePriorityZone> runtimeZones;

    [Serializable]
    public class RuntimePriorityZone
    {
        public string id;
        public string parentDistrictId;
        public int minX, maxX, minY, maxY;
        public int priority;
        public bool isMarket;
        public float economicMultiplier;
    }
}
```

### 6.2 GameState Integration

```csharp
// Add to GameState.cs:

public TileDistrictSaveData tileDistrictData;

// In SaveSystem.Save():
state.tileDistrictData = new TileDistrictSaveData
{
    tileOverrides = TileDistrictService.Instance.GetTileOverrides()._entries,
    // ... save runtime zones if needed
};

// In SaveSystem.Load():
if (state.tileDistrictData != null)
{
    var map = new TileDistrictMap();
    // Reconstruct from entries
    TileDistrictService.Instance.LoadTileOverrides(map);
}
```

---

## 7. Economic System Integration

### 7.1 Market Zones

Priority zones with `isMarket = true` get special treatment:

```csharp
// In MerchantService:

public static bool TryBuy(Merchant merchant, PlayerController player, string itemId, int qty)
{
    var pos = merchant.GetComponent<GridEntity>().GridPosition;

    // Check if in market zone for potential bonuses
    if (TileDistrictService.Instance.IsMarketTile(pos.x, pos.y))
    {
        // Market zones have better stock, faster restocking, etc.
    }

    // ... existing logic
}
```

### 7.2 District-Specific Pricing

```csharp
// Zone modifiers stack with palimpsest and other effects:

float price = ItemDatabase.Get(itemId).value;

// 1. Merchant markup
price *= merchant.Profile.buyMultiplier;

// 2. Zone economic modifier (from TileDistrictService)
price *= TileDistrictService.Instance.GetEconomicModifier(x, y);

// 3. District prosperity (from DistrictState)
var state = TileDistrictService.Instance.GetDistrictAt(x, y);
if (state != null)
    price *= GetProsperityModifier(state);

// 4. Tax (zone + palimpsest)
float tax = TileDistrictService.Instance.GetTaxModifier(x, y);
tax += OverlayResolver.GetRulesAt(x, y).taxModifier;
price *= (1f + tax);

// 5. Faction discount
price *= GetFactionModifier(merchant);

return Mathf.Max(1, Mathf.RoundToInt(price));
```

---

## Summary

This tile-level district system provides:

| Feature | Implementation |
|---------|----------------|
| **Tile overrides** | `TileDistrictMap` dictionary |
| **Priority zones** | `PriorityZone` ScriptableObject |
| **Polygon districts** | Extended `DistrictDefinition.Contains()` |
| **Hybrid lookup** | `TileDistrictService.GetDistrictAt()` |
| **Economic integration** | Zone multipliers, tax modifiers |
| **Serialization** | `TileDistrictSaveData` in GameState |

The TDD tests ensure correct behavior for:
- Basic tile operations (45 tests)
- Lookup priority (override > zone > AABB)
- Polygon point-in-polygon checks
- Economic property inheritance
- Bulk operations
