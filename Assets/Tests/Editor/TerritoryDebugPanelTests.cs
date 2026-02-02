using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using InkSim;


namespace InkSim.Tests
{
    public class TerritoryDebugPanelTests
    {
        private DistrictControlService _svc;
        private DistrictState _state;

        [SetUp]
        public void Setup()
        {
            var go = new GameObject("DistrictControlService_Test");
            _svc = go.AddComponent<DistrictControlService>();

            // Build fake defs
            var distDef = ScriptableObject.CreateInstance<DistrictDefinition>();
            distDef.id = "market";
            distDef.displayName = "Market";
            distDef.minX = 0;
            distDef.maxX = 10;
            distDef.minY = 0;
            distDef.maxY = 10;

            var factions = new List<FactionDefinition>();
            for (int i = 0; i < 2; i++)
            {
                var f = ScriptableObject.CreateInstance<FactionDefinition>();
                f.id = "f" + i;
                f.displayName = "F" + i;
                factions.Add(f);
            }

            // Inject private fields
            SetPrivateList("_districtDefs", new List<DistrictDefinition> { distDef });
            SetPrivateList("_factions", factions);
            SetPrivateDict("_factionIndex", new Dictionary<string, int> { { "f0", 0 }, { "f1", 1 } });

            _state = new DistrictState(distDef, factions.Count);
            _state.control[0] = 0.30f;
            _state.patrol[0] = 0.30f;
            _state.heat[0] = 0.10f;

            _state.control[1] = 0.40f;
            _state.patrol[1] = 0.25f;
            _state.heat[1] = 0.05f;

            SetPrivateList("_states", new List<DistrictState> { _state });
        }

        [TearDown]
        public void Teardown()
        {
            if (_svc != null) Object.DestroyImmediate(_svc.gameObject);
        }

        [Test]
        public void AdjustPatrol_ClampsAndRecomputesControl()
        {
            _svc.controlGrowth = 0.08f;
            _svc.controlHeatDecay = 0.10f;

            _svc.AdjustPatrol("market", 0, +0.10f);

            Assert.AreEqual(0.40f, _state.patrol[0], 0.0001f, "Patrol should increase and clamp to 0..1");

            // Expected control: C' = C + g*P*(1-C) - h*H*C
            float expected = Mathf.Clamp01(0.30f + 0.08f * 0.40f * (1f - 0.30f) - 0.10f * 0.10f * 0.30f);
            Assert.AreEqual(expected, _state.control[0], 0.0001f, "Control should be recomputed immediately when Patrol changes");
        }

        [Test]
        public void AdvanceDay_AppliesEditsCleanupAndLossStreak()
        {
            // Seed some pending edits/cleanup
            _svc.ApplyPalimpsestEdit("market", 1f);   // raises heat
            _svc.ApplyCleanup("market", 0.5f);        // lowers heat

            // Force control low to trigger loss streak
            _state.control[0] = 0.1f;
            int prevLoss = _state.lossStreak[0];

            _svc.AdvanceDay();

            // Heat updated
            Assert.LessOrEqual(_state.heat[0], 1f);
            // Patrol adjusted by daily formula
            Assert.Greater(_state.patrol[0], 0f);
            // Loss streak increments when under threshold
            Assert.AreEqual(prevLoss + 1, _state.lossStreak[0]);
        }

        private void SetPrivateList<T>(string fieldName, List<T> value)
        {
            var fi = typeof(DistrictControlService).GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
            fi.SetValue(_svc, value);
        }

        private void SetPrivateDict<TKey, TValue>(string fieldName, Dictionary<TKey, TValue> value)
        {
            var fi = typeof(DistrictControlService).GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
            fi.SetValue(_svc, value);
        }
    }
}
