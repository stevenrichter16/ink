using UnityEngine;

namespace InkSim
{
    /// <summary>
    /// Orchestrates the world simulation layer. Hooks into the turn loop
    /// and economic day cycle to dispatch faction AI, NPC goals, dynamic spawning,
    /// inscription politics, and quest generation.
    /// </summary>
    public class WorldSimulationService : MonoBehaviour
    {
        public static WorldSimulationService Instance { get; private set; }

        [Header("Simulation Tuning")]
        [Tooltip("Minimum day before simulation systems activate (let economy stabilize first).")]
        public int activationDay = 3;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        /// <summary>
        /// Called every turn from TurnManager (after TurnNumber++).
        /// Lightweight — NPC goals are handled inside NpcAI.TakeTurn() already.
        /// </summary>
        public void OnTurnComplete(int turnNumber)
        {
            // Per-turn work is minimal — NpcGoalSystem execution happens in NpcAI.TakeTurn()
            // Could track metrics here in the future
        }

        /// <summary>
        /// Called from EconomicTickService.AdvanceEconomicDay() after all economic work.
        /// Dispatches the full simulation tick in order.
        /// </summary>
        public void OnEconomicDay(int dayNumber)
        {
            if (dayNumber < activationDay)
            {
                Debug.Log($"[WorldSim] Day {dayNumber}: Simulation waiting for activation day {activationDay}.");
                return;
            }

            Debug.Log($"[WorldSim] === Economic Day {dayNumber} — Running world simulation ===");

            // Phase 1: Faction strategy (patrol adjustment, territory contests, diplomacy)
            FactionStrategyService.Execute(dayNumber);

            // Phase 1.5: Hostility pipeline decay (tension de-escalation)
            HostilityPipeline.EvaluateEscalation(dayNumber);

            // Phase 2: Inscription politics (factions write/erase palimpsest inscriptions)
            InscriptionPoliticsService.Execute(dayNumber);

            // Phase 3: Dynamic spawning (reinforcements, raids, prosperity migration)
            DynamicSpawnService.Execute(dayNumber);

            // Phase 4: NPC goal assignment (trade, patrol, migrate, inscribe)
            NpcGoalSystem.AssignGoals(dayNumber);

            // Phase 5: Dynamic quest generation (from world state)
            DynamicQuestService.Execute(dayNumber);

            Debug.Log($"[WorldSim] === Day {dayNumber} simulation complete ===");
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }
    }
}
