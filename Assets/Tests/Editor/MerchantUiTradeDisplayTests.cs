using NUnit.Framework;
using UnityEngine;

namespace InkSim.Tests
{
    public class MerchantUiTradeDisplayTests
    {
        private GameObject _dcsGO;
        private MerchantProfile _profile;

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
        public void Embargo_ShowsTradeBlockedLabel()
        {
            var dcs = DistrictControlService.Instance;
            var state = dcs?.States != null && dcs.States.Count > 0 ? dcs.States[0] : null;
            if (state == null)
                Assert.Inconclusive("No district state available.");

            int ownerIdx = state.ControllingFactionIndex;
            if (ownerIdx < 0 || ownerIdx >= dcs.Factions.Count)
                Assert.Inconclusive("No controlling faction available.");

            string districtFactionId = dcs.Factions[ownerIdx].id;
            TradeRelationRegistry.SetRelation(new FactionTradeRelation
            {
                sourceFactionId = _profile.factionId,
                targetFactionId = districtFactionId,
                status = TradeStatus.Embargo,
                tariffRate = 0.2f
            });

            var pos = new Vector2Int(state.Definition.minX, state.Definition.minY);
            string label = MerchantUI.GetPriceLabel("potion", _profile, pos, 15, isSell: false);
            Assert.AreEqual("TRADE BLOCKED", label);
        }

        [Test]
        public void OpenTrade_ShowsPriceLabel()
        {
            var dcs = DistrictControlService.Instance;
            var state = dcs?.States != null && dcs.States.Count > 0 ? dcs.States[0] : null;
            if (state == null)
                Assert.Inconclusive("No district state available.");

            var pos = new Vector2Int(state.Definition.minX, state.Definition.minY);
            string label = MerchantUI.GetPriceLabel("potion", _profile, pos, 15, isSell: false);
            Assert.AreEqual("15 coins", label);
        }
    }
}
