using NUnit.Framework;
using UnityEngine;

namespace InkSim.Tests
{
    public class EconomicInscriptionTests
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
            _profile.factionId = null;

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
        public void InscribeTaxPolicy_AffectsPricing()
        {
            var dcs = DistrictControlService.Instance;
            var state = dcs?.States != null && dcs.States.Count > 0 ? dcs.States[0] : null;
            if (state == null)
                Assert.Inconclusive("No district state available.");

            var pos = new Vector2Int(state.Definition.minX, state.Definition.minY);
            EconomicInscriptionService.InscribeTaxPolicy(state.Id, 0.20f, 3);

            int price = EconomicPriceResolver.ResolveBuyPrice("potion", _profile, pos);
            Assert.AreEqual(18, price);
        }

        [Test]
        public void InscribeDemandEvent_AffectsPricing()
        {
            var dcs = DistrictControlService.Instance;
            var state = dcs?.States != null && dcs.States.Count > 0 ? dcs.States[0] : null;
            if (state == null)
                Assert.Inconclusive("No district state available.");

            var pos = new Vector2Int(state.Definition.minX, state.Definition.minY);
            EconomicInscriptionService.InscribeDemandEvent("potion", 2f, 3, state.Id, "Test spike");

            int price = EconomicPriceResolver.ResolveBuyPrice("potion", _profile, pos);
            Assert.AreEqual(30, price);
        }
    }
}
