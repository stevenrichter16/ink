using NUnit.Framework;
using UnityEngine;
using System.Reflection;

namespace InkSim.Tests
{
    /// <summary>
    /// TDD: red tests for EconomicPriceResolver pipeline.
    /// They will fail until pricing uses palimpsest and tax hooks.
    /// </summary>
    public class EconomicPriceResolverTests
    {
        private MerchantProfile _profile;
        private int _layerId;
        private int _layerId2;
        private GameObject _dcsGO;

        [SetUp]
        public void SetUp()
        {
            ItemDatabase.Initialize();
            _profile = ScriptableObject.CreateInstance<MerchantProfile>();
            _profile.buyMultiplier = 1f;
            _profile.sellMultiplier = 0.5f;
            _layerId = -1;
            _layerId2 = -1;
            OverlayResolver.SetRegistry(null);
            SupplyService.Clear();
            TaxRegistry.Clear();
            EconomicEventService.Clear();

            // Minimal DistrictControlService bootstrap for prosperity tests
            if (DistrictControlService.Instance == null)
            {
                _dcsGO = new GameObject("DistrictControlService");
                var dcs = _dcsGO.AddComponent<DistrictControlService>();
                // Manually invoke Awake to populate states in EditMode
                var awake = typeof(DistrictControlService).GetMethod("Awake", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                awake?.Invoke(dcs, null);
            }
        }

        [TearDown]
        public void TearDown()
        {
            if (_layerId > 0)
                OverlayResolver.UnregisterLayer(_layerId);
            if (_layerId2 > 0)
                OverlayResolver.UnregisterLayer(_layerId2);
            if (_profile != null)
                ScriptableObject.DestroyImmediate(_profile);
            if (_dcsGO != null)
            {
                GameObject.DestroyImmediate(_dcsGO);
            }
            OverlayResolver.SetRegistry(null);
            SupplyService.Clear();
            TaxRegistry.Clear();
            EconomicEventService.Clear();
        }

        [Test]
        public void BuyPrice_Applies_PalimpsestPriceAndTax()
        {
            // Arrange: base potion value 15
            var layer = new PalimpsestLayer
            {
                center = new Vector2Int(5, 5),
                radius = 10,
                tokens = { "PRICE:0.5", "TAX:0.20" } // 50% price, +20% tax
            };
            _layerId = OverlayResolver.RegisterLayer(layer);

            // Act: expected price = 15 * 0.5 * (1 + 0.20) = 9
            int price = EconomicPriceResolver.ResolveBuyPrice("potion", _profile, new Vector2Int(5, 5));

            // Assert
            Assert.AreEqual(9, price);
        }

        [Test]
        public void SellPrice_UsesPriceMultiplierOnly()
        {
            var layer = new PalimpsestLayer
            {
                center = Vector2Int.zero,
                radius = 5,
                tokens = { "PRICE:2.0", "TAX:0.50" } // Selling should ignore tax, but respect priceMultiplier
            };
            _layerId = OverlayResolver.RegisterLayer(layer);

            // base gem value 50, sellMultiplier 0.5 -> 25, then *2.0 = 50
            int price = EconomicPriceResolver.ResolveSellPrice("gem", _profile, Vector2Int.zero);

            Assert.AreEqual(50, price);
        }

        [Test]
        public void Prosperity_Multiplies_BuyAndSell()
        {
            var dcs = DistrictControlService.Instance;
            var state = dcs?.States != null && dcs.States.Count > 0 ? dcs.States[0] : null;
            if (state == null)
                Assert.Inconclusive("DistrictControlService did not bootstrap any states; cannot test prosperity.");
            var pos = new Vector2Int(state.Definition.minX, state.Definition.minY);
            state.prosperity = 2f; // 2x prices

            int buy = EconomicPriceResolver.ResolveBuyPrice("potion", _profile, pos); // base 15 * 2
            int sell = EconomicPriceResolver.ResolveSellPrice("potion", _profile, pos); // base 15 *0.5 *2

            Assert.AreEqual(30, buy);
            Assert.AreEqual(15, sell);
        }

        [Test]
        public void FactionReputation_AdjustsPrice()
        {
            // Simulate friendly rep gives discount (test expects < base)
            ReputationSystem.ClearForTests();
            ReputationSystem.SetRep("ghost", 50); // friendly
            _profile.factionId = "ghost";

            int basePrice = EconomicPriceResolver.ResolveBuyPrice("potion", _profile, Vector2Int.zero);
            Assert.Less(basePrice, Mathf.RoundToInt(15 * _profile.buyMultiplier), "Friendly faction should discount buy price");
        }

        [Test]
        public void SupplyDemand_DefaultsToOne()
        {
            // At an arbitrary position with no supply data, price should stay base (ignoring palimpsest/tax)
            var price = EconomicPriceResolver.ResolveBuyPrice("potion", _profile, Vector2Int.zero);
            Assert.AreEqual(Mathf.RoundToInt(15 * _profile.buyMultiplier), price);
        }

        [Test]
        public void NegativeTax_ActsAsDiscount()
        {
            // TAX:-0.20 should reduce price by 20% (buy side only)
            var layer = new PalimpsestLayer
            {
                center = Vector2Int.zero,
                radius = 5,
                tokens = { "TAX:-0.20" }
            };
            _layerId = OverlayResolver.RegisterLayer(layer);

            int price = EconomicPriceResolver.ResolveBuyPrice("potion", _profile, Vector2Int.zero); // 15 * (1-0.2) = 12
            Assert.AreEqual(12, price);
        }

        [Test]
        public void OverlappingLayers_CombineAdditiveTaxAndMultiplicativePrice()
        {
            var a = new PalimpsestLayer { center = Vector2Int.zero, radius = 5, tokens = { "TAX:0.10", "PRICE:0.5" }, priority = 0 };
            var b = new PalimpsestLayer { center = Vector2Int.zero, radius = 5, tokens = { "TAX:-0.05", "PRICE:0.8" }, priority = 1 };
            _layerId = OverlayResolver.RegisterLayer(a);
            _layerId2 = OverlayResolver.RegisterLayer(b);

            // price = 15 * 0.5 * 0.8 = 6, tax = +0.10-0.05=+0.05 => 6 * 1.05 = 6.3 -> 6 rounded
            int price = EconomicPriceResolver.ResolveBuyPrice("potion", _profile, Vector2Int.zero);
            Assert.AreEqual(6, price);
        }

        [Test]
        public void Clamp_MinimumPriceIsOne()
        {
            var layer = new PalimpsestLayer
            {
                center = Vector2Int.zero,
                radius = 5,
                tokens = { "PRICE:0.01", "TAX:-0.90" } // extreme discount
            };
            _layerId = OverlayResolver.RegisterLayer(layer);
            int price = EconomicPriceResolver.ResolveBuyPrice("coin", _profile, Vector2Int.zero); // base 1 => should clamp to 1
            Assert.AreEqual(1, price);
        }

        [Test]
        public void HostileRep_IncreasesPrice()
        {
            ReputationSystem.ClearForTests();
            ReputationSystem.SetRep("ghost", -100); // hostile
            _profile.factionId = "ghost";

            int hostilePrice = EconomicPriceResolver.ResolveBuyPrice("potion", _profile, Vector2Int.zero);
            int basePrice = Mathf.RoundToInt(15 * _profile.buyMultiplier);
            Assert.Greater(hostilePrice, basePrice);
        }

        [Test]
        public void NeutralRep_StaysNearBasePrice()
        {
            ReputationSystem.ClearForTests();
            ReputationSystem.SetRep("ghost", 0);
            _profile.factionId = "ghost";

            int price = EconomicPriceResolver.ResolveBuyPrice("potion", _profile, Vector2Int.zero);
            int basePrice = Mathf.RoundToInt(15 * _profile.buyMultiplier);
            Assert.AreEqual(basePrice, price);
        }

        [Test]
        public void SupplyMultiplier_AppliesToBuyAndSell()
        {
            var dcs = DistrictControlService.Instance;
            var state = dcs?.States != null && dcs.States.Count > 0 ? dcs.States[0] : null;
            if (state == null)
                Assert.Inconclusive("No district state available for supply test.");
            var pos = new Vector2Int(state.Definition.minX, state.Definition.minY);

            SupplyService.SetSupply(state.Id, "potion", 2f); // surplus should reduce price

            int buy = EconomicPriceResolver.ResolveBuyPrice("potion", _profile, pos); // base 15 *0.5 => 8
            int sell = EconomicPriceResolver.ResolveSellPrice("potion", _profile, pos); // base 15 *0.5 *0.5 => 4

            Assert.AreEqual(8, buy);
            Assert.AreEqual(4, sell);
        }

        [Test]
        public void DistrictProsperity_DefaultsToOne()
        {
            var dcs = DistrictControlService.Instance;
            var state = dcs?.States != null && dcs.States.Count > 0 ? dcs.States[0] : null;
            if (state == null)
                Assert.Inconclusive("No district state available to verify prosperity.");
            Assert.AreEqual(1f, state.prosperity, 0.0001f);
        }

        [Test]
        public void RegistryToken_AppliesPriceMultiplier()
        {
            // Create a temporary registry with CHEAP token
            var registry = ScriptableObject.CreateInstance<PalimpsestTokenRegistry>();
            var rule = new PalimpsestTokenRegistry.TokenRule { token = "CHEAP", priceMultiplier = 0.8f };
            registry.ClearAndAddRule(rule);

            OverlayResolver.SetRegistry(registry);

            var layer = new PalimpsestLayer
            {
                center = Vector2Int.zero,
                radius = 5,
                tokens = { "CHEAP" }
            };
            _layerId = OverlayResolver.RegisterLayer(layer);

            int price = EconomicPriceResolver.ResolveBuyPrice("potion", _profile, Vector2Int.zero); // 15 * 0.8 = 12
            Assert.AreEqual(12, price);
        }

        [Test]
        public void RegistryAndLiteralTokens_Combine()
        {
            var registry = ScriptableObject.CreateInstance<PalimpsestTokenRegistry>();
            registry.ClearAndAddRule(new PalimpsestTokenRegistry.TokenRule { token = "CHEAP", priceMultiplier = 0.8f });
            OverlayResolver.SetRegistry(registry);

            var layer = new PalimpsestLayer
            {
                center = Vector2Int.zero,
                radius = 5,
                tokens = { "CHEAP", "TAX:0.10" } // registry price 0.8, literal tax +10%
            };
            _layerId = OverlayResolver.RegisterLayer(layer);

            // base 15 *0.8 =12 ; tax +10% -> 13.2 => 13
            int price = EconomicPriceResolver.ResolveBuyPrice("potion", _profile, Vector2Int.zero);
            Assert.AreEqual(13, price);
        }
    }
}
