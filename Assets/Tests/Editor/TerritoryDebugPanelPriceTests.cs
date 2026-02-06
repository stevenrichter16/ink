using NUnit.Framework;
using UnityEngine;

namespace InkSim.Tests
{
    public class TerritoryDebugPanelPriceTests
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
                dcs.InitializeForTests();
            }
        }

        [TearDown]
        public void TearDown()
        {
            if (_profile != null) ScriptableObject.DestroyImmediate(_profile);
            if (_dcsGO != null)
            {
                GameObject.DestroyImmediate(_dcsGO);
                DistrictControlService.ClearInstanceForTests();
            }
        }

        [Test]
        public void BuildPriceLine_ContainsDistrictAndFinalPrice()
        {
            var svc = DistrictControlService.Instance;
            var state = svc?.States != null && svc.States.Count > 0 ? svc.States[0] : null;
            if (state == null)
                Assert.Inconclusive("No district state available.");

            var line = TerritoryDebugPanel.BuildPriceLine(state, "potion", _profile);
            StringAssert.Contains(state.Definition.displayName, line);
            StringAssert.Contains("Final:", line);
            StringAssert.Contains("15", line); // base 15, no modifiers
        }
    }
}
