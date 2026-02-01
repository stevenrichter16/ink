using System.Collections.Generic;
using UnityEngine;

namespace InkSim
{
    /// <summary>
    /// Manages per-district control/patrol/heat for each faction and runs the lean daily tick.
    /// </summary>
    public class DistrictControlService : MonoBehaviour
    {
        public static DistrictControlService Instance { get; private set; }

        [Header("Defaults")]
        [Range(0f, 1f)] public float defaultControl = 0.3f;
        [Range(0f, 1f)] public float defaultPatrol = 0.3f;
        [Range(0f, 1f)] public float defaultHeat = 0.1f;
        [Tooltip("Number of days C must stay below LostThreshold to lose district.")]
        public int lostDays = 3;
        [Range(0f, 1f)] public float lostThreshold = 0.2f;

        [Header("Per-day coefficients")]
        public float patrolInvest = 0.05f;
        [Tooltip("Extra patrol allocation pressure per point of heat.")]
        public float patrolHeatResponse = 0.08f;
        [Tooltip("Patrol loss per point of heat when chaos makes patrols scatter.")]
        public float patrolHeatPenalty = 0.20f;
        public float controlGrowth = 0.08f;
        public float controlHeatDecay = 0.10f;
        public float heatFromEdit = 0.10f;
        public float heatFromCleanup = 0.05f;
        public float heatBaselineDecay = 0.01f;
        public bool enableDebugLogs = true;

        private List<DistrictDefinition> _districtDefs = new List<DistrictDefinition>();
        private List<FactionDefinition> _factions = new List<FactionDefinition>();
        private Dictionary<string, int> _factionIndex = new Dictionary<string, int>();
        private List<DistrictState> _states = new List<DistrictState>();
        private Dictionary<string, float> _pendingEdits = new Dictionary<string, float>();
        private Dictionary<string, float> _pendingCleanup = new Dictionary<string, float>();

        /// <summary>Number of in-game days that have elapsed since bootstrap.</summary>
        public int CurrentDay { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            Bootstrap();
        }

        private void Bootstrap()
        {
            _districtDefs.AddRange(Resources.LoadAll<DistrictDefinition>("Districts"));
            _factions.AddRange(Resources.LoadAll<FactionDefinition>("Factions"));
            _factionIndex.Clear();
            for (int i = 0; i < _factions.Count; i++)
                _factionIndex[_factions[i].id.ToLowerInvariant()] = i;

            _states.Clear();
            foreach (var def in _districtDefs)
            {
                var state = new DistrictState(def, _factions.Count);
                for (int f = 0; f < _factions.Count; f++)
                {
                    state.control[f] = defaultControl;
                    state.patrol[f] = defaultPatrol;
                    state.heat[f] = defaultHeat;
                }
                _states.Add(state);
            }

            CurrentDay = 0;
        }

        /// <summary>Flag that a palimpsest edit occurred in a district this day.</summary>
        public void ApplyPalimpsestEdit(string districtId, float magnitude = 1f)
        {
            if (string.IsNullOrEmpty(districtId)) return;
            if (!_pendingEdits.ContainsKey(districtId))
                _pendingEdits[districtId] = 0f;
            _pendingEdits[districtId] += magnitude;
        }

        /// <summary>Flag cleanup action for a district this day.</summary>
        public void ApplyCleanup(string districtId, float intensity = 1f)
        {
            if (string.IsNullOrEmpty(districtId)) return;
            if (!_pendingCleanup.ContainsKey(districtId))
                _pendingCleanup[districtId] = 0f;
            _pendingCleanup[districtId] = Mathf.Max(_pendingCleanup[districtId], intensity);
        }

        /// <summary>Advance one in-game day for all districts/factions.</summary>
        public void AdvanceDay()
        {
            CurrentDay++;

            foreach (var state in _states)
            {
                float edits = _pendingEdits.TryGetValue(state.Id, out var e) ? e : 0f;
                float cleanup = _pendingCleanup.TryGetValue(state.Id, out var c) ? c : 0f;

                for (int f = 0; f < _factions.Count; f++)
                {
                    float H = state.heat[f];
                    float P = state.patrol[f];
                    float C = state.control[f];

                    // Patrol: baseline invest + heat response - chaos penalty
                    P = Mathf.Clamp01(P + patrolInvest * 0.5f + patrolHeatResponse * H - patrolHeatPenalty * H);

                    // Control uses patrol directly
                    C = Mathf.Clamp01(C + controlGrowth * P * (1f - C) - controlHeatDecay * H * C);

                    // Heat
                    float deltaH = heatFromEdit * edits - heatFromCleanup * cleanup - heatBaselineDecay;
                    H = Mathf.Clamp01(H + deltaH);

                    state.patrol[f] = P;
                    state.control[f] = C;
                    state.heat[f] = H;

                    if (enableDebugLogs)
                    {
                        Debug.Log($"[TerritoryTick] Day {CurrentDay} Faction={_factions[f].displayName} District={state.Definition.displayName} -> C {C:0.00} P {P:0.00} H {H:0.00} (edits {edits:0.00} cleanup {cleanup:0.00})");
                    }

                    // Loss streak
                    if (C < lostThreshold)
                        state.lossStreak[f]++;
                    else
                        state.lossStreak[f] = 0;
                }

                // Loss: if controlling faction streak exceeds threshold, neutralize
                int owner = state.ControllingFactionIndex;
                if (owner >= 0 && state.lossStreak[owner] >= lostDays)
                {
                    state.Neutralize();
                }
            }

            _pendingEdits.Clear();
            _pendingCleanup.Clear();

            if (enableDebugLogs)
            {
                Debug.Log($"[TerritoryTick] AdvanceDay complete. CurrentDay={CurrentDay}");
            }
        }

        public DistrictState GetStateByPosition(int x, int y)
        {
            foreach (var state in _states)
            {
                if (state.Definition.Contains(x, y))
                    return state;
            }
            return null;
        }

        public IReadOnlyList<DistrictState> States => _states;
        public IReadOnlyList<FactionDefinition> Factions => _factions;

        /// <summary>Adjust patrol value for a specific faction in a district (clamped 0..1).</summary>
        public void AdjustPatrol(string districtId, int factionIndex, float delta)
        {
            if (string.IsNullOrEmpty(districtId)) return;
            if (factionIndex < 0 || factionIndex >= _factions.Count) return;

            foreach (var state in _states)
            {
                if (state.Id == districtId)
                {
                    state.patrol[factionIndex] = Mathf.Clamp01(state.patrol[factionIndex] + delta);
                    RecomputeControl(state, factionIndex);

                    if (enableDebugLogs)
                    {
                        Debug.Log($"[TerritoryDebug] Patrol adjusted (pre-tick) District={state.Definition.displayName} Faction={_factions[factionIndex].displayName} P {state.patrol[factionIndex]:0.00} C {state.control[factionIndex]:0.00} H {state.heat[factionIndex]:0.00}");
                    }
                    return;
                }
            }
        }

        /// <summary>
        /// Recompute control immediately from current patrol/heat for the given faction/state (no day advance).
        /// This gives instant feedback when patrol is adjusted via the debug UI.
        /// </summary>
        private void RecomputeControl(DistrictState state, int f)
        {
            float P = state.patrol[f];
            float H = state.heat[f];
            float C = state.control[f];

            C = Mathf.Clamp01(C + controlGrowth * P * (1f - C) - controlHeatDecay * H * C);
            state.control[f] = C;
        }
    }
}
