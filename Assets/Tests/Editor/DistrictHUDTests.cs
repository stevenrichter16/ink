using NUnit.Framework;
using UnityEngine;

namespace InkSim.Tests
{
    [TestFixture]
    public class DistrictHUDTests
    {
        private GameObject _dcsGO;
        private DistrictControlService _dcs;
        private GameObject _hudGO;
        private DistrictHUD _hud;

        [SetUp]
        public void SetUp()
        {
            FactionRegistry.ClearCache();
            DistrictControlService.ClearInstanceForTests();

            _dcsGO = new GameObject("DCS");
            _dcs = _dcsGO.AddComponent<DistrictControlService>();
            _dcs.InitializeForTests();

            _hudGO = new GameObject("DistrictHUD");
            _hud = _hudGO.AddComponent<DistrictHUD>();
            _hud.BuildUIForTests();
        }

        [TearDown]
        public void TearDown()
        {
            if (_hudGO != null) Object.DestroyImmediate(_hudGO);
            if (_dcsGO != null) Object.DestroyImmediate(_dcsGO);
            DistrictControlService.ClearInstanceForTests();
            FactionRegistry.ClearCache();
        }

        [Test]
        public void UpdateForPosition_InDistrict_ShowsDistrictName()
        {
            // district_market is at x:3-30, y:44-66
            _hud.UpdateForPosition(15, 55);

            Assert.IsTrue(_hud.DistrictLabelText.Contains("Market"),
                $"District label should contain 'Market', got: '{_hud.DistrictLabelText}'");
        }

        [Test]
        public void UpdateForPosition_InDistrict_ShowsControllingFaction()
        {
            // district_market is controlled by faction_inkbound
            _hud.UpdateForPosition(15, 55);

            Assert.IsNotEmpty(_hud.FactionLabelText,
                "Faction label should not be empty when inside a district");
        }

        [Test]
        public void UpdateForPosition_OutsideDistrict_ShowsWilderness()
        {
            // (35, 35) is in the center gap between all districts
            _hud.UpdateForPosition(35, 35);

            Assert.AreEqual("Wilderness", _hud.DistrictLabelText,
                "Should show 'Wilderness' when outside all districts");
        }

        [Test]
        public void UpdateForPosition_SameDistrict_DoesNotReupdate()
        {
            // First call enters district_market
            _hud.UpdateForPosition(15, 55);
            string firstId = _hud.CurrentDistrictId;

            // Second call — same district, different tile
            _hud.UpdateForPosition(20, 55);
            string secondId = _hud.CurrentDistrictId;

            Assert.AreEqual(firstId, secondId,
                "Should cache district ID and skip redundant updates");
        }
    }
}
