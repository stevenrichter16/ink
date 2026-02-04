using NUnit.Framework;
using UnityEngine;

namespace InkSim.Tests
{
    public class TerritoryDebugPanelEconomyTests
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
                var awake = typeof(DistrictControlService).GetMethod("Awake", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                awake?.Invoke(dcs, null);
            }
        }

        [TearDown]
        public void TearDown()
        {
            if (_dcsGO != null) GameObject.DestroyImmediate(_dcsGO);
        }

        [Test]
        public void BuildEconomyLine_ContainsTaxSupplyProsperity()
        {
            var svc = DistrictControlService.Instance;
            var state = svc?.States != null && svc.States.Count > 0 ? svc.States[0] : null;
            if (state == null)
                Assert.Inconclusive("No district state available.");

            SupplyService.SetSupply(state.Id, "potion", 2f);
            TaxRegistry.SetTax(state.Id, 0.15f);
            state.prosperity = 1.1f;

            var line = TerritoryDebugPanel.BuildEconomyLine(state, "potion");
            StringAssert.Contains("Tax:+15%", line);
            StringAssert.Contains("Supply:2.00x", line);
            StringAssert.Contains("Pros:1.10x", line);
        }
    }
}
