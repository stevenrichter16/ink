using UnityEngine;

namespace InkSim
{
    /// <summary>
    /// Applies a reputation penalty when the player kills a faction member.
    /// </summary>
    public static class FactionKillReputation
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Initialize()
        {
            CombatEvents.OnEntityKilled -= HandleEntityKilled;
            CombatEvents.OnEntityKilled += HandleEntityKilled;
        }

        private static void HandleEntityKilled(GridEntity victim, GridEntity killer)
        {
            if (victim == null || killer == null) return;
            if (killer.GetComponent<PlayerController>() == null) return;

            FactionCombatService.OnPlayerKill(victim, killer.GetComponent<PlayerController>());
        }
    }
}
