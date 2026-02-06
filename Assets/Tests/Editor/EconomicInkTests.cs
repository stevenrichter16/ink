using NUnit.Framework;
using UnityEngine;

namespace InkSim.Tests
{
    public class EconomicInkTests
    {
        private GameObject _dcsGO;
        private GameObject _panelGO;
        private GameObject _playerGO;
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
            if (_playerGO != null)
                GameObject.DestroyImmediate(_playerGO);
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
        public void TaxCostBreakdown_UsesExpectedFormula()
        {
            var cost = EconomicInkCostCalculator.CalculateTaxBreakdown(0.25f, 12, 3);
            Assert.AreEqual(5, cost.baseCost);
            Assert.AreEqual(5, cost.magnitudeCost);
            Assert.AreEqual(6, cost.durationCost);
            Assert.AreEqual(6, cost.radiusCost);
            Assert.AreEqual(1f, cost.complexityMultiplier, 0.0001f);
            Assert.AreEqual(22, cost.totalCost);
        }

        [Test]
        public void DemandCostBreakdown_AppliesItemTargetMultiplier()
        {
            var cost = EconomicInkCostCalculator.CalculateDemandBreakdown(2f, 10);
            Assert.AreEqual(5, cost.baseCost);
            Assert.AreEqual(20, cost.magnitudeCost);
            Assert.AreEqual(5, cost.durationCost);
            Assert.AreEqual(0, cost.radiusCost);
            Assert.AreEqual(1.2f, cost.complexityMultiplier, 0.0001f);
            Assert.AreEqual(36, cost.totalCost);
        }

        [Test]
        public void TryInscribeTaxForSelectedDistrict_BlocksWithoutInk()
        {
            var dcs = DistrictControlService.Instance;
            var state = dcs?.States != null && dcs.States.Count > 0 ? dcs.States[0] : null;
            if (state == null)
                Assert.Inconclusive("No district state available.");

            bool ok = _panel.TryInscribeTaxForSelectedDistrict(0.2f, 6, 3);
            Assert.IsFalse(ok);
            Assert.AreEqual(0, TaxRegistry.GetPoliciesFor(state.Id).Count);
        }

        [Test]
        public void TryInscribeTaxForSelectedDistrict_SpendsInkAndCreatesPolicy()
        {
            var player = CreatePlayerWithInk(30);
            Assert.IsNotNull(player);

            int before = EconomicInkService.GetInkBalance();
            var cost = EconomicInkCostCalculator.CalculateTaxBreakdown(0.2f, 6, 3);

            bool ok = _panel.TryInscribeTaxForSelectedDistrict(0.2f, 6, 3);
            Assert.IsTrue(ok);
            Assert.AreEqual(before - cost.totalCost, EconomicInkService.GetInkBalance());

            var dcs = DistrictControlService.Instance;
            var state = dcs?.States != null && dcs.States.Count > 0 ? dcs.States[0] : null;
            if (state == null)
                Assert.Inconclusive("No district state available.");
            Assert.AreEqual(1, TaxRegistry.GetPoliciesFor(state.Id).Count);
        }

        [Test]
        public void TryInscribeDemandForSelectedDistrict_SpendsInkAndCreatesEvent()
        {
            var player = CreatePlayerWithInk(50);
            Assert.IsNotNull(player);

            int before = EconomicInkService.GetInkBalance();
            var cost = EconomicInkCostCalculator.CalculateDemandBreakdown(2f, 6);

            bool ok = _panel.TryInscribeDemandForSelectedDistrict("potion", 2f, 6);
            Assert.IsTrue(ok);
            Assert.AreEqual(before - cost.totalCost, EconomicInkService.GetInkBalance());

            var events = EconomicEventService.GetAllEvents();
            Assert.AreEqual(1, events.Count);
            Assert.AreEqual("potion", events[0].itemId);
        }

        private PlayerController CreatePlayerWithInk(int inkAmount)
        {
            _playerGO = new GameObject("Player");
            var player = _playerGO.AddComponent<PlayerController>();
            var awake = typeof(PlayerController).GetMethod("Awake", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            awake?.Invoke(player, null);

            if (inkAmount > 0)
                player.inventory.AddItem("ink", inkAmount);

            return player;
        }
    }
}
