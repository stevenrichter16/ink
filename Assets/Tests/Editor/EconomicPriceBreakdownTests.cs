using NUnit.Framework;
using UnityEngine;

namespace InkSim.Tests
{
    /// <summary>
    /// TDD: price breakdown helper to expose individual modifiers for debug panel.
    /// </summary>
    public class EconomicPriceBreakdownTests
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

            SupplyService.Clear();
            TaxRegistry.Clear();
            OverlayResolver.SetRegistry(null);
            EconomicEventService.Clear();
        }

        [TearDown]
        public void TearDown()
        {
            if (_profile != null)
                ScriptableObject.DestroyImmediate(_profile);
            if (_dcsGO != null)
                GameObject.DestroyImmediate(_dcsGO);
            SupplyService.Clear();
            TaxRegistry.Clear();
            OverlayResolver.SetRegistry(null);
            EconomicEventService.Clear();
        }

        [Test]
        public void Breakdown_ReportsUnmodifiedPrice()
        {
            var dcs = DistrictControlService.Instance;
            var state = dcs?.States != null && dcs.States.Count > 0 ? dcs.States[0] : null;
            if (state == null)
                Assert.Inconclusive("No district state available.");
            var pos = new Vector2Int(state.Definition.minX, state.Definition.minY);

            var breakdown = EconomicPriceResolver.GetBuyBreakdown("potion", _profile, pos);

            Assert.AreEqual(15, breakdown.baseValue);
            Assert.AreEqual(15, breakdown.finalPrice);
            Assert.AreEqual(1f, breakdown.priceMultiplier);
            Assert.AreEqual(0f, breakdown.tax);
        }
    }
}
