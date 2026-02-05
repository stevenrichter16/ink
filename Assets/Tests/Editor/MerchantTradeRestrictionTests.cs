using NUnit.Framework;
using UnityEngine;

namespace InkSim.Tests
{
    public class MerchantTradeRestrictionTests
    {
        private GameObject _dcsGO;
        private GameObject _merchantGO;
        private GameObject _playerGO;
        private MerchantProfile _profile;
        private Merchant _merchant;
        private PlayerController _player;

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

            var dcsSvc = DistrictControlService.Instance;
            var state = dcsSvc?.States != null && dcsSvc.States.Count > 0 ? dcsSvc.States[0] : null;
            if (state == null)
                Assert.Inconclusive("No district state available.");

            _profile = ScriptableObject.CreateInstance<MerchantProfile>();
            _profile.id = "test_merchant";
            _profile.displayName = "Test Merchant";
            _profile.buyMultiplier = 1f;
            _profile.sellMultiplier = 1f;
            _profile.factionId = "ghost";
            _profile.stock = new System.Collections.Generic.List<MerchantStockEntry>
            {
                new MerchantStockEntry("potion", 5)
            };
            MerchantDatabase.Register(_profile);

            _merchantGO = new GameObject("Merchant");
            _merchant = _merchantGO.AddComponent<Merchant>();
            _merchant.profileId = _profile.id;
            _merchant.OnShopOpened();

            var pos = new Vector2Int(state.Definition.minX, state.Definition.minY);
            _merchant.transform.position = new Vector3(pos.x, pos.y, 0f);

            _playerGO = new GameObject("Player");
            _player = _playerGO.AddComponent<PlayerController>();
            var playerAwake = typeof(PlayerController).GetMethod("Awake", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            playerAwake?.Invoke(_player, null);
            _player.inventory.AddItem("coin", 10);

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
            if (_merchantGO != null)
                GameObject.DestroyImmediate(_merchantGO);
            if (_playerGO != null)
                GameObject.DestroyImmediate(_playerGO);
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
        public void Embargo_BlocksMerchantBuy()
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
                tariffRate = 0.5f
            });

            bool ok = MerchantService.TryBuy(_merchant, _player, "potion", 1);
            Assert.IsFalse(ok);
            Assert.AreEqual(10, _player.inventory.CountItem("coin"));
            Assert.AreEqual(0, _player.inventory.CountItem("potion"));
        }
    }
}
