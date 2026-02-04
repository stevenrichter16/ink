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
        private List<FactionDefinition> _factions;

        [SetUp]
        public void SetUp()
        {
            // Create test factions
            _factions = new List<FactionDefinition>();
            var f = ScriptableObject.CreateInstance<FactionDefinition>();
            f.id = "test_faction";
            _factions.Add(f);

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
                new List<PriorityZone> { _marketSquare },
                _factions
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
            foreach (var f in _factions)
                Object.DestroyImmediate(f);
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

            // Reinitialize to pick up priority change
            _service.Initialize(
                new List<DistrictDefinition> { _marketRow, _templeWard },
                new List<PriorityZone> { _marketSquare },
                _factions
            );

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

        #region GetDistrictIdAt (lightweight lookup)

        [Test]
        public void GetDistrictIdAt_ReturnsId_WhenInBounds()
        {
            string id = _service.GetDistrictIdAt(5, 5);
            Assert.AreEqual("market_row", id);
        }

        [Test]
        public void GetDistrictIdAt_ReturnsNull_WhenOutOfBounds()
        {
            string id = _service.GetDistrictIdAt(100, 100);
            Assert.IsNull(id);
        }

        [Test]
        public void GetDistrictIdAt_ReturnsTileOverride_WhenSet()
        {
            _service.SetTileDistrict(5, 5, "temple_ward");
            string id = _service.GetDistrictIdAt(5, 5);
            Assert.AreEqual("temple_ward", id);
        }

        #endregion

        #region HasTileOverride

        [Test]
        public void HasTileOverride_ReturnsFalse_WhenNotSet()
        {
            Assert.IsFalse(_service.HasTileOverride(5, 5));
        }

        [Test]
        public void HasTileOverride_ReturnsTrue_WhenSet()
        {
            _service.SetTileDistrict(5, 5, "temple_ward");
            Assert.IsTrue(_service.HasTileOverride(5, 5));
        }

        #endregion

        #region Sanctuary and Guild Hall zones

        [Test]
        public void IsSanctuaryTile_ReturnsFalse_WhenNoSanctuary()
        {
            Assert.IsFalse(_service.IsSanctuaryTile(10, 10));
        }

        [Test]
        public void IsSanctuaryTile_ReturnsTrue_WhenInSanctuaryZone()
        {
            var sanctuary = ScriptableObject.CreateInstance<PriorityZone>();
            sanctuary.id = "temple_sanctuary";
            sanctuary.minX = 0;
            sanctuary.maxX = 5;
            sanctuary.minY = 0;
            sanctuary.maxY = 5;
            sanctuary.priority = 5;
            sanctuary.isSanctuary = true;

            _service.AddPriorityZone(sanctuary);

            Assert.IsTrue(_service.IsSanctuaryTile(2, 2));

            Object.DestroyImmediate(sanctuary);
        }

        [Test]
        public void IsGuildHallTile_ReturnsFalse_WhenNoGuildHall()
        {
            Assert.IsFalse(_service.IsGuildHallTile(10, 10));
        }

        [Test]
        public void IsGuildHallTile_ReturnsTrue_WhenInGuildHallZone()
        {
            var guildHall = ScriptableObject.CreateInstance<PriorityZone>();
            guildHall.id = "merchants_guild";
            guildHall.minX = 0;
            guildHall.maxX = 5;
            guildHall.minY = 0;
            guildHall.maxY = 5;
            guildHall.priority = 5;
            guildHall.isGuildHall = true;

            _service.AddPriorityZone(guildHall);

            Assert.IsTrue(_service.IsGuildHallTile(2, 2));

            Object.DestroyImmediate(guildHall);
        }

        #endregion

        #region Tax Modifier

        [Test]
        public void GetTaxModifier_ReturnsDefault_OutsideZone()
        {
            float tax = _service.GetTaxModifier(0, 0);
            Assert.AreEqual(0f, tax, 0.01f);
        }

        [Test]
        public void GetTaxModifier_ReturnsZoneTax_InZone()
        {
            var taxZone = ScriptableObject.CreateInstance<PriorityZone>();
            taxZone.id = "high_tax_zone";
            taxZone.minX = 0;
            taxZone.maxX = 5;
            taxZone.minY = 0;
            taxZone.maxY = 5;
            taxZone.priority = 5;
            taxZone.taxModifier = 0.15f;

            _service.AddPriorityZone(taxZone);

            float tax = _service.GetTaxModifier(2, 2);
            Assert.AreEqual(0.15f, tax, 0.01f);

            Object.DestroyImmediate(taxZone);
        }

        #endregion

        #region Remove Priority Zone

        [Test]
        public void RemovePriorityZone_RemovesZone()
        {
            var zone = _service.GetPriorityZoneAt(10, 10);
            Assert.IsNotNull(zone);
            Assert.AreEqual("market_square", zone.id);

            _service.RemovePriorityZone("market_square");

            zone = _service.GetPriorityZoneAt(10, 10);
            Assert.IsNull(zone);
        }

        #endregion
    }
}
