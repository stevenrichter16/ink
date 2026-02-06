using NUnit.Framework;
using UnityEngine;

namespace InkSim.Tests
{
    public class LedgerEconomyPanelActionTests
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
                dcs.InitializeForTests();
            }

            TaxRegistry.Clear();
            TradeRelationRegistry.Clear();
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
            {
                GameObject.DestroyImmediate(_dcsGO);
                DistrictControlService.ClearInstanceForTests();
            }

            TaxRegistry.Clear();
            TradeRelationRegistry.Clear();
            EconomicEventService.Clear();
        }

        [Test]
        public void EditTaxPolicyForSelectedDistrict_UpdatesRateAndDuration()
        {
            var dcs = DistrictControlService.Instance;
            var state = dcs?.States != null && dcs.States.Count > 0 ? dcs.States[0] : null;
            if (state == null)
                Assert.Inconclusive("No district state available.");

            TaxRegistry.AddPolicy(new TaxPolicy
            {
                id = "tax_edit_test",
                type = TaxType.Sales,
                rate = 0.10f,
                jurisdictionId = state.Id,
                turnsRemaining = 8
            });

            bool ok = _panel.EditTaxPolicyForSelectedDistrict("tax_edit_test", 0.22f, 5);
            Assert.IsTrue(ok);

            var policies = TaxRegistry.GetPoliciesFor(state.Id);
            var edited = policies.Find(p => p.id == "tax_edit_test");
            Assert.AreEqual(0.22f, edited.rate, 0.0001f);
            Assert.AreEqual(5, edited.turnsRemaining);
        }

        [Test]
        public void SetTradeRelationForSelectedDistrict_UpdatesRelation()
        {
            var dcs = DistrictControlService.Instance;
            var state = dcs?.States != null && dcs.States.Count > 0 ? dcs.States[0] : null;
            if (state == null)
                Assert.Inconclusive("No district state available.");
            if (dcs.Factions == null || dcs.Factions.Count < 2)
                Assert.Inconclusive("Need at least 2 factions for trade relation test.");

            int controllerIdx = state.ControllingFactionIndex;
            if (controllerIdx < 0 || controllerIdx >= dcs.Factions.Count)
                Assert.Inconclusive("No controlling faction for district.");

            string controllerFaction = dcs.Factions[controllerIdx].id;
            int otherIdx = controllerIdx == 0 ? 1 : 0;
            string targetFaction = dcs.Factions[otherIdx].id;

            bool ok = _panel.SetTradeRelationForSelectedDistrict(targetFaction, TradeStatus.Embargo, 0.35f);
            Assert.IsTrue(ok);

            var relation = TradeRelationRegistry.GetRelation(controllerFaction, targetFaction);
            Assert.AreEqual(TradeStatus.Embargo, relation.status);
            Assert.AreEqual(0.35f, relation.tariffRate, 0.0001f);
        }

        [Test]
        public void RemoveActiveEdicts_RemovesTaxPolicyAndDemandEvent()
        {
            var dcs = DistrictControlService.Instance;
            var state = dcs?.States != null && dcs.States.Count > 0 ? dcs.States[0] : null;
            if (state == null)
                Assert.Inconclusive("No district state available.");

            TaxRegistry.AddPolicy(new TaxPolicy
            {
                id = "tax_remove_test",
                type = TaxType.Sales,
                rate = 0.1f,
                jurisdictionId = state.Id,
                turnsRemaining = 6
            });

            EconomicEventService.TriggerEvent(new DemandEvent
            {
                id = "demand_remove_test",
                itemId = "potion",
                demandMultiplier = 2f,
                durationDays = 3,
                districtId = state.Id,
                description = "Test remove"
            });

            Assert.IsTrue(_panel.RemoveTaxPolicyById("tax_remove_test"));
            Assert.IsTrue(_panel.RemoveDemandEventById("demand_remove_test"));

            var policies = TaxRegistry.GetPoliciesFor(state.Id);
            Assert.IsFalse(policies.Exists(p => p.id == "tax_remove_test"));

            var events = EconomicEventService.GetAllEvents();
            Assert.IsFalse(events.Exists(e => e != null && e.id == "demand_remove_test"));
        }
    }
}
