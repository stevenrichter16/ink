using NUnit.Framework;
using UnityEngine;

namespace InkSim.Tests
{
    /// <summary>
    /// TDD tests for simple per-district tax registry feeding EconomicPriceResolver.
    /// </summary>
    public class EconomicTaxTests
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

            if (DistrictControlService.Instance == null)
            {
                _dcsGO = new GameObject("DistrictControlService");
                var dcs = _dcsGO.AddComponent<DistrictControlService>();
                var awake = typeof(DistrictControlService).GetMethod("Awake", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                awake?.Invoke(dcs, null);
            }
            TaxRegistry.Clear();
            SupplyService.Clear();
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
        }

        [Test]
        public void DistrictTax_IncreasesBuyPrice()
        {
            var dcs = DistrictControlService.Instance;
            var state = dcs?.States != null && dcs.States.Count > 0 ? dcs.States[0] : null;
            if (state == null)
                Assert.Inconclusive("No district state available for tax test.");

            var pos = new Vector2Int(state.Definition.minX, state.Definition.minY);
            TaxRegistry.SetTax(state.Id, 0.20f); // +20% tax

            // base potion value 15, buy multiplier 1.0 -> 15 * (1 + 0.2) = 18
            int price = EconomicPriceResolver.ResolveBuyPrice("potion", _profile, pos);
            Assert.AreEqual(18, price);
        }
    }
}
