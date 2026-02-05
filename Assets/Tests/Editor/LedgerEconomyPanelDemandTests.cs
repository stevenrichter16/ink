using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace InkSim.Tests
{
    public class LedgerEconomyPanelDemandTests
    {
        private GameObject _dcsGO;
        private GameObject _panelGO;
        private LedgerEconomyPanel _panel;

        [SetUp]
        public void SetUp()
        {
            ItemDatabase.Initialize();
            if (DistrictControlService.Instance == null)
            {
                _dcsGO = new GameObject("DistrictControlService");
                var dcs = _dcsGO.AddComponent<DistrictControlService>();
                var awake = typeof(DistrictControlService).GetMethod("Awake", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                awake?.Invoke(dcs, null);
            }

            EconomicEventService.Clear();

            _panelGO = new GameObject("LedgerEconomyPanel");
            _panel = _panelGO.AddComponent<LedgerEconomyPanel>();
            _panel.Initialize(null);
            _panel.SelectDistrict(0);
        }

        [TearDown]
        public void TearDown()
        {
            if (_panelGO != null)
                GameObject.DestroyImmediate(_panelGO);
            if (_dcsGO != null)
                GameObject.DestroyImmediate(_dcsGO);

            EconomicEventService.Clear();
        }

        [Test]
        public void InscribeDemandForSelectedDistrict_CreatesDemandEvent()
        {
            var dcs = DistrictControlService.Instance;
            var state = dcs?.States != null && dcs.States.Count > 0 ? dcs.States[0] : null;
            if (state == null)
                Assert.Inconclusive("No district state available.");

            _panel.InscribeDemandForSelectedDistrict("potion", 2f, 3);

            var events = EconomicEventService.GetAllEvents();
            Assert.AreEqual(1, events.Count);
            Assert.AreEqual("potion", events[0].itemId);
            Assert.AreEqual(state.Id, events[0].districtId);
        }

        [Test]
        public void DetailPane_ShowsActiveDemandEvents_LocalAndGlobal()
        {
            var dcs = DistrictControlService.Instance;
            var state = dcs?.States != null && dcs.States.Count > 0 ? dcs.States[0] : null;
            if (state == null)
                Assert.Inconclusive("No district state available.");

            EconomicEventService.TriggerEvent(new DemandEvent
            {
                id = "local",
                itemId = "potion",
                demandMultiplier = 2f,
                durationDays = 3,
                districtId = state.Id,
                description = "Local demand"
            });

            EconomicEventService.TriggerEvent(new DemandEvent
            {
                id = "global",
                itemId = "gem",
                demandMultiplier = 1.5f,
                durationDays = 5,
                districtId = null,
                description = "Global demand"
            });

            _panel.SelectDistrict(0);

            var texts = _panelGO.GetComponentsInChildren<UnityEngine.UI.Text>(true)
                .Select(t => t.text)
                .ToArray();
            var joined = string.Join("|", texts);

            StringAssert.Contains("potion", joined);
            StringAssert.Contains("x2.00", joined);
            StringAssert.Contains("local", joined.ToLowerInvariant());
            StringAssert.Contains("gem", joined);
            StringAssert.Contains("x1.50", joined);
            StringAssert.Contains("global", joined.ToLowerInvariant());
        }
    }
}
