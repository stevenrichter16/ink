using System.Collections.Generic;
using UnityEngine;

namespace InkSim
{
    /// <summary>
    /// Faction AI — each economic day, every faction evaluates its position
    /// and takes strategic actions: patrol adjustment, territory contests,
    /// diplomatic shifts, and economic retaliation.
    /// </summary>
    public static class FactionStrategyService
    {
        // Track how many consecutive days each district has been contested
        // Key = districtId, Value = days contested
        private static Dictionary<string, int> _contestedDays = new Dictionary<string, int>();

        // Public accessor so DynamicSpawnService and DynamicQuestService can read contest state
        public static IReadOnlyDictionary<string, int> ContestedDistricts => _contestedDays;

        // Last skirmish info for quest generation
        public static string LastSkirmishDistrictId { get; private set; }
        public static string LastSkirmishAttackerFactionId { get; private set; }

        public static void Execute(int dayNumber)
        {
            var dcs = DistrictControlService.Instance;
            if (dcs == null || dcs.States == null || dcs.Factions == null) return;

            Debug.Log($"[FactionStrategy] Day {dayNumber}: Evaluating {dcs.Factions.Count} factions across {dcs.States.Count} districts.");

            EvaluatePatrols(dcs);
            EvaluateContests(dcs);
            EvaluateDiplomacy(dcs);
            EvaluateEconomicRetaliation(dcs);
        }

        /// <summary>
        /// Factions invest patrol in districts they're losing control of,
        /// and withdraw patrol from districts they dominate.
        /// </summary>
        public static void EvaluatePatrols(DistrictControlService dcs)
        {
            for (int d = 0; d < dcs.States.Count; d++)
            {
                var state = dcs.States[d];

                for (int f = 0; f < dcs.Factions.Count; f++)
                {
                    float control = state.control[f];
                    float currentPatrol = state.patrol[f];

                    // Only adjust factions that have some presence
                    if (control < 0.05f && currentPatrol < 0.05f) continue;

                    if (control < 0.4f && currentPatrol > 0.01f)
                    {
                        // Losing ground — increase patrol
                        float boost = Mathf.Min(0.1f, 0.5f - control);
                        dcs.AdjustPatrol(state.Id, f, boost);
                        Debug.Log($"[FactionStrategy] {dcs.Factions[f].id} boosting patrol in {state.Id} by {boost:F2} (control={control:F2})");
                    }
                    else if (control > 0.7f)
                    {
                        // Dominating — reduce patrol, save resources
                        float reduction = -Mathf.Min(0.05f, currentPatrol * 0.2f);
                        if (currentPatrol + reduction > 0.05f)
                        {
                            dcs.AdjustPatrol(state.Id, f, reduction);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// When two factions have similar control in a district, mark it as contested.
        /// After 3+ days contested, trigger skirmish behavior.
        /// </summary>
        public static void EvaluateContests(DistrictControlService dcs)
        {
            // Track which districts are currently contested
            HashSet<string> currentlyContested = new HashSet<string>();

            for (int d = 0; d < dcs.States.Count; d++)
            {
                var state = dcs.States[d];

                // Find top-2 factions by control
                int first = -1, second = -1;
                float firstControl = 0f, secondControl = 0f;

                for (int f = 0; f < state.control.Length; f++)
                {
                    if (state.control[f] > firstControl)
                    {
                        second = first;
                        secondControl = firstControl;
                        first = f;
                        firstControl = state.control[f];
                    }
                    else if (state.control[f] > secondControl)
                    {
                        second = f;
                        secondControl = state.control[f];
                    }
                }

                // Contest threshold: gap < 0.15 and both have meaningful control
                if (first >= 0 && second >= 0 && secondControl > 0.1f && (firstControl - secondControl) < 0.15f)
                {
                    currentlyContested.Add(state.Id);

                    if (!_contestedDays.ContainsKey(state.Id))
                        _contestedDays[state.Id] = 0;
                    _contestedDays[state.Id]++;

                    int days = _contestedDays[state.Id];

                    // Both factions increase patrol in contested districts
                    dcs.AdjustPatrol(state.Id, first, 0.05f);
                    dcs.AdjustPatrol(state.Id, second, 0.05f);

                    Debug.Log($"[FactionStrategy] CONTESTED: {state.Id} — {dcs.Factions[first].id} ({firstControl:F2}) vs {dcs.Factions[second].id} ({secondControl:F2}), day {days}");

                    // After 3+ days, mark for skirmish spawning
                    if (days >= 3)
                    {
                        LastSkirmishDistrictId = state.Id;
                        LastSkirmishAttackerFactionId = dcs.Factions[second].id;
                        Debug.Log($"[FactionStrategy] SKIRMISH triggered in {state.Id}! {dcs.Factions[second].id} challenging {dcs.Factions[first].id}");
                    }
                }
            }

            // Clear contest counters for districts no longer contested
            List<string> toRemove = new List<string>();
            foreach (var kvp in _contestedDays)
            {
                if (!currentlyContested.Contains(kvp.Key))
                    toRemove.Add(kvp.Key);
            }
            foreach (var key in toRemove)
                _contestedDays.Remove(key);
        }

        /// <summary>
        /// Faction-to-faction reputation shifts based on territorial conflict.
        /// Contesting the same district reduces inter-faction rep.
        /// Allied factions in non-contested areas slowly build trust.
        /// </summary>
        public static void EvaluateDiplomacy(DistrictControlService dcs)
        {
            for (int d = 0; d < dcs.States.Count; d++)
            {
                var state = dcs.States[d];
                bool isContested = _contestedDays.ContainsKey(state.Id);

                // Find factions with meaningful presence
                List<int> presentFactions = new List<int>();
                for (int f = 0; f < state.control.Length; f++)
                {
                    if (state.control[f] > 0.1f)
                        presentFactions.Add(f);
                }

                // For each pair of present factions
                for (int i = 0; i < presentFactions.Count; i++)
                {
                    for (int j = i + 1; j < presentFactions.Count; j++)
                    {
                        int fA = presentFactions[i];
                        int fB = presentFactions[j];

                        string factionA = dcs.Factions[fA].id;
                        string factionB = dcs.Factions[fB].id;

                        if (isContested)
                        {
                            // Contesting same district — hostility grows
                            ReputationSystem.AddInterRep(factionA, factionB, -3);
                        }
                        else
                        {
                            // Sharing non-contested district — check if allied
                            var relation = TradeRelationRegistry.GetRelation(factionA, factionB);
                            if (relation != null && relation.status == TradeStatus.Alliance)
                            {
                                // Allied factions build trust slowly
                                ReputationSystem.AddInterRep(factionA, factionB, 1);
                            }
                        }
                    }
                }
            }

            // Faction that lost a district loses rep toward the winner
            for (int d = 0; d < dcs.States.Count; d++)
            {
                var state = dcs.States[d];
                int owner = state.ControllingFactionIndex;
                if (owner < 0) continue;

                for (int f = 0; f < state.lossStreak.Length; f++)
                {
                    if (f == owner) continue;
                    if (state.lossStreak[f] >= 3 && state.control[f] > 0.05f)
                    {
                        string loserId = dcs.Factions[f].id;
                        string winnerId = dcs.Factions[owner].id;
                        ReputationSystem.AddInterRep(loserId, winnerId, -5);
                    }
                }
            }
        }

        /// <summary>
        /// Factions respond to trade embargoes and alliances with economic actions.
        /// This sets up data that InscriptionPoliticsService will use to write inscriptions.
        /// </summary>
        public static void EvaluateEconomicRetaliation(DistrictControlService dcs)
        {
            var allRelations = TradeRelationRegistry.GetAll();
            if (allRelations == null) return;

            for (int r = 0; r < allRelations.Count; r++)
            {
                var rel = allRelations[r];
                if (rel.status == TradeStatus.Embargo)
                {
                    // Embargoed faction raises heat in districts they control
                    // (makes inscriptions more aggressive — handled by InscriptionPoliticsService)
                    for (int d = 0; d < dcs.States.Count; d++)
                    {
                        var state = dcs.States[d];
                        int srcIdx = GetFactionIndex(dcs, rel.sourceFactionId);
                        if (srcIdx >= 0 && state.control[srcIdx] > 0.5f)
                        {
                            // Increase heat slightly — embargoed faction is "on edge"
                            state.heat[srcIdx] = Mathf.Min(1f, state.heat[srcIdx] + 0.05f);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Get the faction index within DistrictControlService.Factions for a given faction ID.
        /// Returns -1 if not found.
        /// </summary>
        public static int GetFactionIndex(DistrictControlService dcs, string factionId)
        {
            if (dcs == null || dcs.Factions == null || string.IsNullOrEmpty(factionId)) return -1;
            for (int i = 0; i < dcs.Factions.Count; i++)
            {
                if (dcs.Factions[i] != null && dcs.Factions[i].id == factionId)
                    return i;
            }
            return -1;
        }

        /// <summary>Clear state for testing / game restart.</summary>
        public static void Clear()
        {
            _contestedDays.Clear();
            LastSkirmishDistrictId = null;
            LastSkirmishAttackerFactionId = null;
        }
    }
}
