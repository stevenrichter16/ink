using UnityEngine;

namespace InkSim
{
    /// <summary>
    /// Centralizes hit/kill aggression rules for factions.
    /// </summary>
    public static class FactionCombatService
    {
        public static void OnPlayerHit(GridEntity victim, PlayerController player)
        {
            if (victim == null || player == null) return;

            var member = victim.GetComponent<FactionMember>();
            if (member == null || member.faction == null) return;

            var faction = member.faction;
            int rep = ReputationSystem.GetRep(faction.id);
            member.NotePlayerAttack();

            // Report assault to hostility pipeline
            HostilityPipeline.ReportIncident(IncidentType.Assault, victim.gridX, victim.gridY, "player", faction.id);

            // Hostile rep: unchanged behavior
            if (rep <= HostilityService.HostileThreshold)
            {
                member.EnterHostile(player);
                return;
            }

            // Friendly/positive rep
            if (rep >= HostilityService.FriendlyThreshold)
            {
                // Second hit during alert escalates
                if (member.state == FactionMember.AlertState.Alert)
                {
                    member.EnterHostile(player);
                    FactionMember.ForAlliesInRadius(member, faction.rallyRadius, ally =>
                    {
                        ally.EnterHostile(player);
                        ally.NotePlayerAttack();
                    });
                }
                else
                {
                    member.EnterAlert();
                    FactionMember.ForAlliesInRadius(member, faction.rallyRadius, ally =>
                    {
                        ally.EnterAlert();
                        ally.NotePlayerAttack();
                    });
                }

                ReputationSystem.AddRep(faction.id, faction.repOnHit);
                return;
            }

            // Neutral rep
            member.EnterHostile(player);
            FactionMember.ForAlliesInRadius(member, faction.rallyRadius, ally =>
            {
                ally.EnterHostile(player);
                ally.NotePlayerAttack();
            });
            ReputationSystem.AddRep(faction.id, faction.repOnHit);
        }

        public static void OnPlayerKill(GridEntity victim, PlayerController player)
        {
            if (victim == null || player == null) return;
            var member = victim.GetComponent<FactionMember>();
            if (member == null || member.faction == null) return;

            var faction = member.faction;
            int rep = ReputationSystem.GetRep(faction.id);

            // Report murder to hostility pipeline
            HostilityPipeline.ReportIncident(IncidentType.Murder, victim.gridX, victim.gridY, "player", faction.id);

            if (rep > HostilityService.HostileThreshold)
            {
                ReputationSystem.AddRep(faction.id, faction.repOnKill);

                FactionMember.ForAlliesInRadius(member, faction.rallyRadius, ally =>
                {
                    ally.EnterHostile(player);
                    ally.NotePlayerAttack();
                });
            }
        }
    }
}
