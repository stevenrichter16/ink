using NUnit.Framework;
using UnityEngine;

namespace InkSim.Tests
{
    public class EconomicTradeRelationTests
    {
        private MerchantProfile _profile;
        private GameObject _dcsGO;

        [SetUp]
        public void SetUp()
        {
            ItemDatabase.Initialize();
            _profile = ScriptableObject.CreateInstance<MerchantProfile>();
            _profile.buyMultiplier = 1f;
            _profile.sellMultiplier = 1f;
            _profile.factionId = "ghost";

            if (DistrictControlService.Instance == null)
            {
                _dcsGO = new GameObject("DistrictControlService");
                var dcs = _dcsGO.AddComponent<DistrictControlService>();
                var awake = typeof(DistrictControlService).GetMethod("Awake", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                awake?.Invoke(dcs, null);
            }

            TaxRegistry.Clear();
            SupplyService.Clear();
            TradeRelationRegistry.Clear();
            EconomicEventService.Clear();
            OverlayResolver.SetRegistry(null);
            ReputationSystem.ClearForTests();
        }

        [TearDown]
        public void TearDown()
        {
            if (_profile != null)
                ScriptableObject.DestroyImmediate(_profile);
            if (_dcsGO != null)
                GameObject.DestroyImmediate(_dcsGO);

            TaxRegistry.Clear();
            SupplyService.Clear();
            TradeRelationRegistry.Clear();
            EconomicEventService.Clear();
            OverlayResolver.SetRegistry(null);
            ReputationSystem.ClearForTests();
        }

        [Test]
        public void RestrictedTrade_AddsTariffToBuyPrice()
        {
            var dcs = DistrictControlService.Instance;
            var state = dcs?.States != null && dcs.States.Count > 0 ? dcs.States[0] : null;
            if (state == null)
                Assert.Inconclusive("No district state available.");

            var pos = new Vector2Int(state.Definition.minX, state.Definition.minY);
            int ownerIdx = state.ControllingFactionIndex;
            if (ownerIdx < 0 || ownerIdx >= dcs.Factions.Count)
                Assert.Inconclusive("No controlling faction available.");

            string districtFactionId = dcs.Factions[ownerIdx].id;
            TradeRelationRegistry.SetRelation(new FactionTradeRelation
            {
                sourceFactionId = _profile.factionId,
                targetFactionId = districtFactionId,
                status = TradeStatus.Restricted,
                tariffRate = 0.20f
            });

            // base potion value 15; +20% tariff -> 18
            int price = EconomicPriceResolver.ResolveBuyPrice("potion", _profile, pos);
            Assert.AreEqual(18, price);
        }

        [Test]
        public void Embargo_BlocksTrade()
        {
            var dcs = DistrictControlService.Instance;
            var state = dcs?.States != null && dcs.States.Count > 0 ? dcs.States[0] : null;
            if (state == null)
                Assert.Inconclusive("No district state available.");

            var pos = new Vector2Int(state.Definition.minX, state.Definition.minY);
            int ownerIdx = state.ControllingFactionIndex;
            if (ownerIdx < 0 || ownerIdx >= dcs.Factions.Count)
                Assert.Inconclusive("No controlling faction available.");

            string districtFactionId = dcs.Factions[ownerIdx].id;
            TradeRelationRegistry.SetRelation(new FactionTradeRelation
            {
                sourceFactionId = _profile.factionId,
                targetFactionId = districtFactionId,
                status = TradeStatus.Embargo,
                tariffRate = 0.20f
            });

            Assert.IsFalse(EconomicPriceResolver.IsTradeAllowed("potion", _profile, pos));
        }

        [Test]
        public void ExclusiveTrade_RequiresExclusiveItem()
        {
            var dcs = DistrictControlService.Instance;
            var state = dcs?.States != null && dcs.States.Count > 0 ? dcs.States[0] : null;
            if (state == null)
                Assert.Inconclusive("No district state available.");

            var pos = new Vector2Int(state.Definition.minX, state.Definition.minY);
            int ownerIdx = state.ControllingFactionIndex;
            if (ownerIdx < 0 || ownerIdx >= dcs.Factions.Count)
                Assert.Inconclusive("No controlling faction available.");

            string districtFactionId = dcs.Factions[ownerIdx].id;
            TradeRelationRegistry.SetRelation(new FactionTradeRelation
            {
                sourceFactionId = _profile.factionId,
                targetFactionId = districtFactionId,
                status = TradeStatus.Exclusive,
                tariffRate = 0.10f,
                exclusiveItems = new System.Collections.Generic.List<string> { "gem" }
            });

            Assert.IsFalse(EconomicPriceResolver.IsTradeAllowed("potion", _profile, pos));
            Assert.IsTrue(EconomicPriceResolver.IsTradeAllowed("gem", _profile, pos));
        }
    }
}
