using NUnit.Framework;
using UnityEngine;

namespace InkSim.Tests
{
    [TestFixture]
    public class TerritoryOverlayTests
    {
        private GameObject _dcsGO;
        private DistrictControlService _dcs;
        private GameObject _overlayGO;
        private TerritoryOverlay _overlay;

        [SetUp]
        public void SetUp()
        {
            FactionRegistry.ClearCache();
            DistrictControlService.ClearInstanceForTests();

            _dcsGO = new GameObject("DCS");
            _dcs = _dcsGO.AddComponent<DistrictControlService>();
            _dcs.InitializeForTests();

            _overlayGO = new GameObject("TerritoryOverlay");
            _overlay = _overlayGO.AddComponent<TerritoryOverlay>();
        }

        [TearDown]
        public void TearDown()
        {
            if (_overlayGO != null) Object.DestroyImmediate(_overlayGO);
            if (_dcsGO != null) Object.DestroyImmediate(_dcsGO);
            DistrictControlService.ClearInstanceForTests();
            FactionRegistry.ClearCache();
        }

        #region Initialization Tests

        [Test]
        public void Initialize_CreatesOverlayRoot()
        {
            _overlay.Initialize(0.5f);

            Assert.IsNotNull(_overlay.OverlayRoot, "Should create an overlay root transform");
        }

        [Test]
        public void Initialize_OverlayStartsHidden()
        {
            _overlay.Initialize(0.5f);

            Assert.IsFalse(_overlay.IsVisible, "Overlay should start hidden");
            Assert.IsFalse(_overlay.OverlayRoot.gameObject.activeSelf,
                "Overlay root should be inactive on start");
        }

        [Test]
        public void Initialize_CreatesOverlayTilesForDistrictBounds()
        {
            _overlay.Initialize(0.5f);

            // Should have created overlay tiles only inside district bounds
            Assert.Greater(_overlay.OverlayTileCount, 0,
                "Should create overlay tiles for district areas");
        }

        [Test]
        public void Initialize_DoesNotCreateTilesOutsideDistricts()
        {
            _overlay.Initialize(0.5f);

            // Total map = 120*70 = 8400. District tiles should be much less.
            // 6 districts at roughly 28*23 = 644 each = ~3864 total
            Assert.Less(_overlay.OverlayTileCount, 120 * 70,
                "Should NOT create tiles for the entire map");
        }

        #endregion

        #region Toggle Tests

        [Test]
        public void Toggle_TurnsOverlayOn()
        {
            _overlay.Initialize(0.5f);

            _overlay.ToggleOverlay();

            Assert.IsTrue(_overlay.IsVisible, "Toggle should turn overlay on");
            Assert.IsTrue(_overlay.OverlayRoot.gameObject.activeSelf,
                "Overlay root should be active after toggle on");
        }

        [Test]
        public void Toggle_TwiceTurnsOverlayOff()
        {
            _overlay.Initialize(0.5f);

            _overlay.ToggleOverlay();
            _overlay.ToggleOverlay();

            Assert.IsFalse(_overlay.IsVisible, "Double toggle should turn overlay off");
            Assert.IsFalse(_overlay.OverlayRoot.gameObject.activeSelf,
                "Overlay root should be inactive after double toggle");
        }

        #endregion

        #region Color Assignment Tests

        [Test]
        public void RefreshColors_AssignsControllingFactionColor()
        {
            // Assign a known color to the controlling faction of the first district
            var states = _dcs.States;
            var factions = _dcs.Factions;
            Assert.Greater(states.Count, 0, "Precondition: need district states");
            Assert.Greater(factions.Count, 0, "Precondition: need factions");

            int ctrlIdx = states[0].ControllingFactionIndex;
            Assert.GreaterOrEqual(ctrlIdx, 0, "First district should have a controlling faction");

            var expected = new Color(0.2f, 0.4f, 0.9f, 0.25f);
            factions[ctrlIdx].color = expected;

            _overlay.Initialize(0.5f);
            _overlay.ToggleOverlay(); // triggers RefreshColors

            // Verify the overlay tiles for district 0 actually have the faction color
            Color actual = _overlay.GetDistrictTint(0);
            Assert.AreEqual(expected, actual,
                "Overlay tiles should be tinted with the controlling faction's color");
        }

        [Test]
        public void GetDominantFaction_ReturnsHighestControlFaction()
        {
            var states = _dcs.States;
            Assert.Greater(states.Count, 0, "Precondition: need district states");

            var state = states[0];
            int dominant = state.ControllingFactionIndex;

            // The owning faction should have 0.7 control (set in Bootstrap)
            Assert.GreaterOrEqual(dominant, 0, "Should have a dominant faction");
            Assert.AreEqual(0.7f, state.control[dominant], 0.01f,
                "Dominant faction should have 0.7 control from bootstrap");
        }

        #endregion

        #region Faction Color Defaults

        [Test]
        public void FactionDefinition_HasColorField()
        {
            var faction = ScriptableObject.CreateInstance<FactionDefinition>();
            Assert.IsNotNull(faction.color, "FactionDefinition should have a color field");
            // Default should be white
            Assert.AreEqual(Color.white, faction.color, "Default color should be white");
            Object.DestroyImmediate(faction);
        }

        [Test]
        public void FactionColor_CanBeAssigned()
        {
            var faction = ScriptableObject.CreateInstance<FactionDefinition>();
            var expected = new Color(0.2f, 0.4f, 0.9f, 0.25f);
            faction.color = expected;

            Assert.AreEqual(expected, faction.color,
                "Should be able to assign custom color to faction");
            Object.DestroyImmediate(faction);
        }

        #endregion

        #region GetDominantFactionId Helper Tests

        [Test]
        public void GetDominantFactionId_ReturnsCorrectFaction()
        {
            var states = _dcs.States;
            var factions = _dcs.Factions;
            Assert.Greater(states.Count, 0);
            Assert.Greater(factions.Count, 0);

            string dominantId = DistrictControlService.GetDominantFactionId(states[0], factions);
            Assert.IsNotNull(dominantId, "Should return a faction id for a district with control");
        }

        [Test]
        public void GetDominantFactionId_StillReturnsFaction_ForNeutralizedDistrict()
        {
            var states = _dcs.States;
            var factions = _dcs.Factions;
            Assert.Greater(states.Count, 0);

            // Neutralize district so all control values are very low (0.05)
            states[0].Neutralize();

            // All factions have 0.05 control — first one wins the tie
            string dominantId = DistrictControlService.GetDominantFactionId(states[0], factions);
            Assert.IsNotNull(dominantId,
                "Even neutralized districts have a technical dominant faction (first with highest control wins)");
        }

        #endregion
    }
}
