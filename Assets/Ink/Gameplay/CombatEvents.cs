using System;

namespace InkSim
{
    /// <summary>
    /// Centralized combat events for cross-system hooks.
    /// </summary>
    public static class CombatEvents
    {
        public static event Action<GridEntity, GridEntity> OnEntityKilled;

        public static void RaiseEntityKilled(GridEntity victim, GridEntity killer)
        {
            OnEntityKilled?.Invoke(victim, killer);
        }
    }
}
