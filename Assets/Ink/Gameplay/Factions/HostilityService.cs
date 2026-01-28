namespace InkSim
{
    /// <summary>
    /// Centralized hostility rules between entities.
    /// </summary>
    public static class HostilityService
    {
        public const int FriendlyThreshold = 25;
        public const int HostileThreshold = -25;
        public const int HostileAggroRadius = 5;

        public static bool IsHostile(GridEntity attacker, GridEntity target)
        {
            if (attacker == null || target == null) return false;
            if (attacker == target) return false;

            // Same faction = never hostile
            if (AreSameFaction(attacker, target)) return false;

            var attackerFaction = GetFaction(attacker);
            var targetFaction = GetFaction(target);

            // Player attacking something
            if (attacker is PlayerController)
            {
                // Factionless enemies are always hostile to player
                if (target is EnemyAI && targetFaction == null) return true;
                // Check faction reputation
                if (targetFaction != null)
                    return ReputationSystem.GetRep(targetFaction.id) <= HostileThreshold;
                return false;
            }

            // Something attacking player
            if (target is PlayerController)
            {
                // Factionless enemies are always hostile to player
                if (attacker is EnemyAI && attackerFaction == null) return true;
                // Check faction reputation
                if (attackerFaction != null)
                    return ReputationSystem.GetRep(attackerFaction.id) <= HostileThreshold;
                return false;
            }

            // NPC vs NPC or Enemy vs Enemy with no factions = NOT hostile
            // This prevents the combat loop chaos
            if (attackerFaction == null && targetFaction == null)
                return false;

            // One has faction, one doesn't - only hostile if the factioned one is hostile-rep to player
            if (attackerFaction == null || targetFaction == null)
            {
                var faction = attackerFaction ?? targetFaction;
                // Factionless enemies only attack player, not other entities
                if (attacker is EnemyAI && targetFaction == null) return false;
                if (target is EnemyAI && attackerFaction == null) return false;
                // NPCs with factions can be hostile to factionless enemies if rep is hostile
                return ReputationSystem.GetRep(faction.id) <= HostileThreshold;
            }

            // Both have factions - hostile only if different factions AND one is hostile-rep
            // For now, different factions are NOT auto-hostile (allows neutral NPCs)
            return false;
        }

        /// <summary>
        /// Determines if the defender should retaliate against the attacker based on disposition and reputation.
        /// </summary>
        public static bool ShouldRetaliate(GridEntity defender, GridEntity attacker)
        {
            if (defender == null || attacker == null) return false;
            if (defender == attacker) return false;
            if (AreSameFaction(defender, attacker)) return false;

            var defenderFaction = GetFaction(defender);
            if (defenderFaction == null) return true; // no faction -> preserve current behavior

            // If attacker is the player, use reputation and disposition
            if (attacker is PlayerController)
            {
                int rep = ReputationSystem.GetRep(defenderFaction.id);
                if (rep <= HostileThreshold)
                    return true; // hostile regardless of calm/aggressive

                return defenderFaction.disposition == FactionDefinition.FactionDisposition.Aggressive;
            }

            // For non-player attackers, keep existing behavior (retaliate)
            return true;
        }

        public static bool AreSameFaction(GridEntity a, GridEntity b)
        {
            if (a == null || b == null) return false;
            var aFaction = GetFaction(a);
            var bFaction = GetFaction(b);
            if (aFaction == null || bFaction == null) return false;
            return aFaction.id == bFaction.id;
        }

        private static FactionDefinition GetFaction(GridEntity entity)
        {
            return entity != null ? entity.GetComponent<FactionMember>()?.faction : null;
        }
    }
}
