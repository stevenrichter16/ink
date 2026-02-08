using UnityEngine;

namespace InkSim
{
    /// <summary>
    /// Connects player combat actions to the territory control simulation.
    /// When the player kills a faction enemy, the victim's faction loses patrol
    /// presence and gains heat in that district, weakening their territorial hold.
    /// </summary>
    public static class TerritoryImpactService
    {
        /// <summary>Patrol reduction applied to victim's faction per player kill.</summary>
        const float PatrolReductionPerKill = -0.02f;

        /// <summary>Raw heat magnitude added per player kill (scaled by heatFromEdit internally).</summary>
        const float HeatMagnitudePerKill = 0.3f;

        /// <summary>Patrol boost for the dominant defending faction when a quest is completed in their district.</summary>
        const float PatrolBoostPerQuest = 0.05f;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Initialize()
        {
            CombatEvents.OnEntityKilled -= HandleKill;
            CombatEvents.OnEntityKilled += HandleKill;
        }

        private static void HandleKill(GridEntity victim, GridEntity killer)
        {
            // Only react to player kills
            if (killer == null || killer.GetComponent<PlayerController>() == null) return;
            if (victim == null) return;

            var fm = victim.GetComponent<FactionMember>();
            if (fm?.faction == null) return;

            var dcs = DistrictControlService.Instance;
            if (dcs == null) return;

            var state = dcs.GetStateByPosition(victim.gridX, victim.gridY);
            if (state == null) return;

            int fIdx = FactionStrategyService.GetFactionIndex(dcs, fm.faction.id);
            if (fIdx < 0) return;

            // Weaken victim faction's patrol presence in this district
            dcs.AdjustPatrol(state.Id, fIdx, PatrolReductionPerKill);

            // Add heat/chaos to the district for this faction (processed on next day tick)
            dcs.ApplyPalimpsestEdit(state.Id, HeatMagnitudePerKill);
        }

        /// <summary>
        /// Called when a dynamic quest is completed. Grants a patrol boost to the
        /// dominant defending faction in the quest's district.
        /// </summary>
        public static void OnQuestCompleted(string districtId)
        {
            if (string.IsNullOrEmpty(districtId)) return;

            var dcs = DistrictControlService.Instance;
            if (dcs == null) return;

            var state = dcs.GetStateById(districtId);
            if (state == null) return;

            // Boost the currently dominant faction (defending the district)
            int owner = state.ControllingFactionIndex;
            if (owner < 0) return;

            dcs.AdjustPatrol(state.Id, owner, PatrolBoostPerQuest);
        }
    }
}
