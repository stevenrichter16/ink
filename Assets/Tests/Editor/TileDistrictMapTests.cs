using System.Collections.Generic;
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
