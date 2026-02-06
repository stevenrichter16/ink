using NUnit.Framework;
using UnityEngine;

namespace InkSim.Tests
{
    /// <summary>
    /// Tests for EconomicTickService daily economic simulation.
    /// </summary>
    public class EconomicTickServiceTests
    {
        private GameObject _dcsGO;

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
            SupplyService.Clear();
            EconomicEventService.Clear();
        }

        [TearDown]
        public void TearDown()
        {
            if (_dcsGO != null)
            {
                GameObject.DestroyImmediate(_dcsGO);
                DistrictControlService.ClearInstanceForTests();
            }
            TaxRegistry.Clear();
            SupplyService.Clear();
            EconomicEventService.Clear();
        }

        [Test]
        public void AdvanceDay_TicksAllSubsystems()
        {
            var dcs = DistrictControlService.Instance;
            if (dcs == null || dcs.States == null || dcs.States.Count == 0)
                Assert.Inconclusive("No DistrictControlService states available.");

            int dayBefore = dcs.CurrentDay;

            // Add a tax policy with 1 turn remaining
            TaxRegistry.AddPolicy(new TaxPolicy
            {
                id = "test_expiring_tax",
                type = TaxType.Sales,
                rate = 0.10f,
                jurisdictionId = dcs.States[0].Id,
                turnsRemaining = 1
            });

            // Add a demand event with 1 day remaining
            EconomicEventService.TriggerEvent(new DemandEvent
            {
                id = "test_expiring_demand",
                itemId = "potion",
                demandMultiplier = 2f,
                durationDays = 1
            });

            // Tick
            EconomicTickService.AdvanceEconomicDay();

            // Day should have advanced
            Assert.AreEqual(dayBefore + 1, dcs.CurrentDay);

            // Tax policy should have been removed (turnsRemaining was 1, now 0)
            var policies = TaxRegistry.GetPoliciesFor(dcs.States[0].Id);
            Assert.AreEqual(0, policies.Count, "Expiring tax policy should be removed after tick");

            // Demand event should have been removed
            var events = EconomicEventService.GetAllEvents();
            Assert.AreEqual(0, events.Count, "Expiring demand event should be removed after tick");
        }

        [Test]
        public void Supply_IncreasesForProducedGoods()
        {
            var dcs = DistrictControlService.Instance;
            if (dcs == null || dcs.States == null || dcs.States.Count == 0)
                Assert.Inconclusive("No DistrictControlService states available.");

            var state = dcs.States[0];
            var def = state.Definition;
            if (def.producedGoods == null || def.producedGoods.Count == 0)
                Assert.Inconclusive("First district has no produced goods configured.");

            string producedItem = def.producedGoods[0];
            float supplyBefore = 1f; // baseline
            if (state.itemSupply != null && state.itemSupply.TryGetValue(producedItem, out var existingLevel))
                supplyBefore = existingLevel;

            EconomicTickService.AdvanceEconomicDay();

            float supplyAfter = 1f;
            if (state.itemSupply != null && state.itemSupply.TryGetValue(producedItem, out var newLevel))
                supplyAfter = newLevel;

            Assert.Greater(supplyAfter, supplyBefore,
                $"Supply of produced item '{producedItem}' should increase after economic day");
        }

        [Test]
        public void Supply_DecreasesForConsumedGoods()
        {
            var dcs = DistrictControlService.Instance;
            if (dcs == null || dcs.States == null || dcs.States.Count == 0)
                Assert.Inconclusive("No DistrictControlService states available.");

            var state = dcs.States[0];
            var def = state.Definition;
            if (def.consumedGoods == null || def.consumedGoods.Count == 0)
                Assert.Inconclusive("First district has no consumed goods configured.");

            string consumedItem = def.consumedGoods[0];

            // Set a known starting supply above minimum
            if (state.itemSupply == null)
                state.itemSupply = new System.Collections.Generic.Dictionary<string, float>();
            state.itemSupply[consumedItem] = 1.5f;

            EconomicTickService.AdvanceEconomicDay();

            float supplyAfter = state.itemSupply.TryGetValue(consumedItem, out var level) ? level : 1f;
            Assert.Less(supplyAfter, 1.5f,
                $"Supply of consumed item '{consumedItem}' should decrease after economic day");
        }

        [Test]
        public void Prosperity_ConvergesToHealthScore()
        {
            var dcs = DistrictControlService.Instance;
            if (dcs == null || dcs.States == null || dcs.States.Count == 0)
                Assert.Inconclusive("No DistrictControlService states available.");

            var state = dcs.States[0];
            // Set prosperity to an extreme value to verify it lerps toward health
            state.prosperity = 2f;

            // Run several days
            for (int i = 0; i < 10; i++)
                EconomicTickService.AdvanceEconomicDay();

            // Prosperity should have moved closer to the health score (which is < 2.0 typically)
            Assert.Less(state.prosperity, 2f,
                "Prosperity should converge toward health score over multiple days");
            Assert.Greater(state.prosperity, 0.1f,
                "Prosperity should stay above minimum clamp");
        }

        [Test]
        public void TaxRevenue_AccumulatesInTreasury()
        {
            var dcs = DistrictControlService.Instance;
            if (dcs == null || dcs.States == null || dcs.States.Count == 0)
                Assert.Inconclusive("No DistrictControlService states available.");

            var state = dcs.States[0];
            state.treasury = 0f;
            state.corruption = 0f; // no corruption means full revenue

            EconomicTickService.AdvanceEconomicDay();

            Assert.Greater(state.treasury, 0f,
                "Treasury should increase after economic day with non-zero economic value");
        }
    }
}
