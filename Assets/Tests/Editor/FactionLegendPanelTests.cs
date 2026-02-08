using NUnit.Framework;
using UnityEngine;

namespace InkSim.Tests
{
    [TestFixture]
    public class FactionLegendPanelTests
    {
        private GameObject _dcsGO;
        private DistrictControlService _dcs;
        private GameObject _legendGO;
        private FactionLegendPanel _legend;
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
            _overlay.Initialize(0.5f);

            _legendGO = new GameObject("FactionLegendPanel");
            _legend = _legendGO.AddComponent<FactionLegendPanel>();
            _legend.Initialize(_overlay);
        }

        [TearDown]
        public void TearDown()
        {
            if (_legendGO != null) Object.DestroyImmediate(_legendGO);
            if (_overlayGO != null) Object.DestroyImmediate(_overlayGO);
            if (_dcsGO != null) Object.DestroyImmediate(_dcsGO);
            DistrictControlService.ClearInstanceForTests();
            FactionRegistry.ClearCache();
        }

        [Test]
        public void BuildLegendData_Returns6Entries()
        {
            var data = FactionLegendPanel.BuildLegendData();

            // 6 districts, each with a different controlling faction
            Assert.AreEqual(6, data.Count,
                $"Should have 6 legend entries (one per district-owning faction), got {data.Count}");
        }

        [Test]
        public void BuildLegendData_ContainsCorrectColors()
        {
            // Assign a known color to the first district's controlling faction
            var states = _dcs.States;
            var factions = _dcs.Factions;
            int ctrlIdx = states[0].ControllingFactionIndex;
            var expected = new Color(0.2f, 0.4f, 0.9f, 0.25f);
            factions[ctrlIdx].color = expected;

            var data = FactionLegendPanel.BuildLegendData();

            bool found = false;
            foreach (var entry in data)
            {
                if (entry.color == expected)
                {
                    found = true;
                    break;
                }
            }
            Assert.IsTrue(found, "Legend data should contain the assigned faction color");
        }

        [Test]
        public void Show_SetsRootActive()
        {
            _legend.Show();

            Assert.IsTrue(_legend.IsVisible, "Show should make legend visible");
        }

        [Test]
        public void Hide_SetsRootInactive()
        {
            _legend.Show();
            _legend.Hide();

            Assert.IsFalse(_legend.IsVisible, "Hide should make legend invisible");
        }

        [Test]
        public void Initialize_StartsHidden()
        {
            Assert.IsFalse(_legend.IsVisible,
                "Legend should start hidden after Initialize");
        }
    }
}
