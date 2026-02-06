using System.Collections.Generic;
using UnityEngine;

namespace InkSim
{
    /// <summary>
    /// Goal types that override default NPC behavior (Stationary/Wander).
    /// </summary>
    public enum NpcGoalType
    {
        None,           // Fall through to default behavior
        PatrolRoute,    // Walk between waypoints in faction territory
        Trade,          // Move toward a merchant to trade (affects supply)
        Migrate,        // Move toward a high-prosperity district
        Inscribe,       // Move to a location to write a palimpsest inscription
        Guard,          // Stand near a contested point, attack hostiles
        Flee            // Run from combat when low health
    }

    /// <summary>
    /// Represents an active goal for an NPC.
    /// </summary>
    public class NpcGoal
    {
        public NpcGoalType type;
        public Vector2Int target;
        public string targetDistrictId;
        public List<string> inscriptionTokens;
        public int turnsRemaining;  // Goal expires if not achieved

        // Patrol-specific: list of waypoints to visit
        public List<Vector2Int> waypoints;
        public int currentWaypointIndex;
    }

    /// <summary>
    /// Gives each NPC a "current goal" that overrides their default Stationary/Wander behavior.
    /// Goals are assigned per economic day based on faction strategy.
    /// Execution happens per turn inside NpcAI.TakeTurn().
    /// </summary>
    public static class NpcGoalSystem
    {
        private static Dictionary<NpcAI, NpcGoal> _goals = new Dictionary<NpcAI, NpcGoal>();

        /// <summary>
        /// Assign goals to all NPCs based on faction strategy and world state.
        /// Called each economic day by WorldSimulationService.
        /// </summary>
        public static void AssignGoals(int dayNumber)
        {
            var dcs = DistrictControlService.Instance;
            if (dcs == null) return;

            // Clean up stale references
            CleanupDeadNpcs();

            // Iterate all registered faction members and assign goals
            var members = FactionMember.ActiveMembers;
            if (members == null) return;

            int assigned = 0;

            for (int i = 0; i < members.Count; i++)
            {
                var fm = members[i];
                if (fm == null || !fm.gameObject.activeInHierarchy) continue;

                var npc = fm.GetComponent<NpcAI>();
                if (npc == null) continue; // Only NPCs get goals, not enemies

                // Skip NPCs that are merchants with a shop (they stay put)
                var merchant = fm.GetComponent<Merchant>();
                if (merchant != null)
                    continue; // Merchants don't get goals — they mind the shop

                // Determine goal based on faction state and NPC role
                NpcGoal goal = DetermineGoal(npc, fm, dcs);
                if (goal != null && goal.type != NpcGoalType.None)
                {
                    _goals[npc] = goal;
                    assigned++;
                }
            }

            Debug.Log($"[NpcGoalSystem] Day {dayNumber}: Assigned {assigned} goals to NPCs.");
        }

        /// <summary>
        /// Try to execute one step of the NPC's current goal.
        /// Called from NpcAI.TakeTurn() before the behavior switch.
        /// Returns true if the goal handled this turn (NPC should skip default behavior).
        /// </summary>
        public static bool TryExecuteGoal(NpcAI npc)
        {
            if (!_goals.TryGetValue(npc, out var goal)) return false;
            if (goal.type == NpcGoalType.None) return false;

            // Decrement turns remaining
            goal.turnsRemaining--;
            if (goal.turnsRemaining <= 0)
            {
                _goals.Remove(npc);
                return false; // Goal expired
            }

            var gridWorld = GridWorld.Instance;
            if (gridWorld == null) return false;

            switch (goal.type)
            {
                case NpcGoalType.PatrolRoute:
                    return ExecutePatrol(npc, goal, gridWorld);

                case NpcGoalType.Guard:
                    return ExecuteGuard(npc, goal, gridWorld);

                case NpcGoalType.Trade:
                    return ExecuteTrade(npc, goal, gridWorld);

                case NpcGoalType.Migrate:
                    return ExecuteMigrate(npc, goal, gridWorld);

                case NpcGoalType.Inscribe:
                    return ExecuteInscribe(npc, goal, gridWorld);

                case NpcGoalType.Flee:
                    return ExecuteFlee(npc, goal, gridWorld);

                default:
                    return false;
            }
        }

        /// <summary>
        /// Determine what goal an NPC should pursue based on its faction and the world state.
        /// </summary>
        private static NpcGoal DetermineGoal(NpcAI npc, FactionMember fm, DistrictControlService dcs)
        {
            if (fm.faction == null) return null;

            string factionId = fm.faction.id;

            // Check if NPC's district is contested — guards are needed
            var districtState = dcs.GetStateByPosition(npc.gridX, npc.gridY);
            if (districtState != null && FactionStrategyService.ContestedDistricts.ContainsKey(districtState.Id))
            {
                int fIdx = FactionStrategyService.GetFactionIndex(dcs, factionId);
                if (fIdx >= 0 && districtState.control[fIdx] > 0.2f)
                {
                    // This NPC's faction has presence in a contested district — Guard duty
                    return new NpcGoal
                    {
                        type = NpcGoalType.Guard,
                        target = new Vector2Int(npc.gridX, npc.gridY), // Guard current position
                        turnsRemaining = 20,
                    };
                }
            }

            // High-rank NPCs patrol their faction's territory
            if (fm.rankId == "mid" || fm.rankId == "high")
            {
                // Find a district controlled by this faction
                for (int d = 0; d < dcs.States.Count; d++)
                {
                    var state = dcs.States[d];
                    int fIdx = FactionStrategyService.GetFactionIndex(dcs, factionId);
                    if (fIdx < 0) continue;

                    if (state.control[fIdx] > 0.4f)
                    {
                        var def = state.Definition;
                        if (def == null) continue;

                        // Generate patrol waypoints within the district
                        var waypoints = GeneratePatrolWaypoints(def, 3);
                        if (waypoints.Count > 0)
                        {
                            return new NpcGoal
                            {
                                type = NpcGoalType.PatrolRoute,
                                waypoints = waypoints,
                                currentWaypointIndex = 0,
                                target = waypoints[0],
                                targetDistrictId = state.Id,
                                turnsRemaining = 30,
                            };
                        }
                    }
                }
            }

            // Low-rank NPCs might migrate toward prosperity
            if (districtState != null && districtState.prosperity < 0.4f)
            {
                // Look for a better district
                DistrictState bestDistrict = null;
                float bestProsperity = districtState.prosperity;

                for (int d = 0; d < dcs.States.Count; d++)
                {
                    var state = dcs.States[d];
                    if (state.prosperity > bestProsperity + 0.2f)
                    {
                        bestProsperity = state.prosperity;
                        bestDistrict = state;
                    }
                }

                if (bestDistrict != null && bestDistrict.Definition != null)
                {
                    var def = bestDistrict.Definition;
                    int centerX = (def.minX + def.maxX) / 2;
                    int centerY = (def.minY + def.maxY) / 2;

                    return new NpcGoal
                    {
                        type = NpcGoalType.Migrate,
                        target = new Vector2Int(centerX, centerY),
                        targetDistrictId = bestDistrict.Id,
                        turnsRemaining = 40,
                    };
                }
            }

            // Default: no special goal
            return null;
        }

        // === Goal Execution ===

        private static bool ExecutePatrol(NpcAI npc, NpcGoal goal, GridWorld gridWorld)
        {
            if (goal.waypoints == null || goal.waypoints.Count == 0) return false;

            Vector2Int target = goal.waypoints[goal.currentWaypointIndex];

            // Check if we've reached the current waypoint
            if (npc.gridX == target.x && npc.gridY == target.y)
            {
                // Move to next waypoint
                goal.currentWaypointIndex = (goal.currentWaypointIndex + 1) % goal.waypoints.Count;
                target = goal.waypoints[goal.currentWaypointIndex];
                goal.target = target;
            }

            return MoveToward(npc, target, gridWorld);
        }

        private static bool ExecuteGuard(NpcAI npc, NpcGoal goal, GridWorld gridWorld)
        {
            // Look for hostile entities nearby
            int scanRange = 4;
            for (int dx = -scanRange; dx <= scanRange; dx++)
            {
                for (int dy = -scanRange; dy <= scanRange; dy++)
                {
                    int checkX = npc.gridX + dx;
                    int checkY = npc.gridY + dy;

                    var entity = gridWorld.GetEntityAt(checkX, checkY);
                    if (entity == null) continue;
                    if (entity == npc) continue;

                    if (HostilityService.IsHostile(npc, entity))
                    {
                        // Set hostile target and let existing combat system handle it
                        npc.hostileTarget = entity;
                        return false; // Let normal combat behavior take over
                    }
                }
            }

            // No hostiles nearby — wander near guard position
            int distToPost = GridWorld.Distance(npc.gridX, npc.gridY, goal.target.x, goal.target.y);
            if (distToPost > 3)
            {
                // Drifted too far — move back toward guard position
                return MoveToward(npc, goal.target, gridWorld);
            }

            // Stay near post — small random movement
            if (Random.value < 0.2f)
            {
                Vector2Int randomDir = GetRandomDirection();
                int newX = npc.gridX + randomDir.x;
                int newY = npc.gridY + randomDir.y;

                if (gridWorld.CanEnter(newX, newY) && GridWorld.Distance(newX, newY, goal.target.x, goal.target.y) <= 3)
                {
                    npc.TryMove(randomDir);
                }
            }

            return true; // Guard handled this turn (even if just standing)
        }

        private static bool ExecuteTrade(NpcAI npc, NpcGoal goal, GridWorld gridWorld)
        {
            int dist = GridWorld.Distance(npc.gridX, npc.gridY, goal.target.x, goal.target.y);

            if (dist <= 1)
            {
                // Adjacent to target — "trade" by adjusting supply
                if (!string.IsNullOrEmpty(goal.targetDistrictId))
                {
                    // Simulate a trade transaction
                    var dcs = DistrictControlService.Instance;
                    if (dcs != null)
                    {
                        // Small supply boost to destination district
                        SupplyService.UpdateSupply(goal.targetDistrictId, "potion", 1);
                        Debug.Log($"[NpcGoalSystem] {npc.name} completed trade in {goal.targetDistrictId}");
                    }
                }

                _goals.Remove(npc);
                return true;
            }

            return MoveToward(npc, goal.target, gridWorld);
        }

        private static bool ExecuteMigrate(NpcAI npc, NpcGoal goal, GridWorld gridWorld)
        {
            int dist = GridWorld.Distance(npc.gridX, npc.gridY, goal.target.x, goal.target.y);

            if (dist <= 2)
            {
                // Arrived at destination
                Debug.Log($"[NpcGoalSystem] {npc.name} migrated to {goal.targetDistrictId}");
                _goals.Remove(npc);
                return true;
            }

            return MoveToward(npc, goal.target, gridWorld);
        }

        private static bool ExecuteInscribe(NpcAI npc, NpcGoal goal, GridWorld gridWorld)
        {
            int dist = GridWorld.Distance(npc.gridX, npc.gridY, goal.target.x, goal.target.y);

            if (dist <= 1)
            {
                // At inscription point — write inscription
                if (goal.inscriptionTokens != null && goal.inscriptionTokens.Count > 0)
                {
                    var layer = new PalimpsestLayer
                    {
                        center = new Vector2Int(npc.gridX, npc.gridY),
                        radius = 3,
                        priority = 1,
                        turnsRemaining = 30,
                        tokens = new List<string>(goal.inscriptionTokens)
                    };
                    OverlayResolver.RegisterLayer(layer);
                    Debug.Log($"[NpcGoalSystem] {npc.name} inscribed [{string.Join(", ", goal.inscriptionTokens)}] at ({npc.gridX},{npc.gridY})");
                }

                _goals.Remove(npc);
                return true;
            }

            return MoveToward(npc, goal.target, gridWorld);
        }

        private static bool ExecuteFlee(NpcAI npc, NpcGoal goal, GridWorld gridWorld)
        {
            // Check if health recovered
            if (npc.currentHealth > npc.maxHealth * 0.5f)
            {
                _goals.Remove(npc);
                return false; // Recovered, resume normal behavior
            }

            // Find nearest hostile and run away
            GridEntity nearestHostile = null;
            int nearestDist = int.MaxValue;

            for (int dx = -5; dx <= 5; dx++)
            {
                for (int dy = -5; dy <= 5; dy++)
                {
                    var entity = gridWorld.GetEntityAt(npc.gridX + dx, npc.gridY + dy);
                    if (entity == null || entity == npc) continue;
                    if (!HostilityService.IsHostile(npc, entity)) continue;

                    int dist = Mathf.Abs(dx) + Mathf.Abs(dy);
                    if (dist < nearestDist)
                    {
                        nearestDist = dist;
                        nearestHostile = entity;
                    }
                }
            }

            if (nearestHostile != null)
            {
                // Move away from hostile
                int dx = Mathf.Clamp(npc.gridX - nearestHostile.gridX, -1, 1);
                int dy = Mathf.Clamp(npc.gridY - nearestHostile.gridY, -1, 1);

                Vector2Int fleeDir = new Vector2Int(dx, dy);
                if (fleeDir == Vector2Int.zero) fleeDir = GetRandomDirection();

                npc.TryMove(fleeDir);
                return true;
            }

            return false;
        }

        // === Helpers ===

        /// <summary>
        /// Move one step toward a target position.
        /// </summary>
        private static bool MoveToward(NpcAI npc, Vector2Int target, GridWorld gridWorld)
        {
            Vector2Int dir = GridWorld.DirectionToward(npc.gridX, npc.gridY, target.x, target.y);
            if (dir == Vector2Int.zero) return false;

            if (npc.TryMove(dir))
                return true;

            // Try alternate directions
            Vector2Int alt1 = new Vector2Int(dir.y, dir.x);
            Vector2Int alt2 = new Vector2Int(-dir.y, -dir.x);

            if (npc.TryMove(alt1)) return true;
            if (npc.TryMove(alt2)) return true;

            return true; // Still "handled" even if blocked
        }

        private static Vector2Int GetRandomDirection()
        {
            int r = Random.Range(0, 4);
            switch (r)
            {
                case 0: return Vector2Int.up;
                case 1: return Vector2Int.down;
                case 2: return Vector2Int.left;
                default: return Vector2Int.right;
            }
        }

        /// <summary>
        /// Generate random walkable waypoints within a district for patrol routes.
        /// </summary>
        private static List<Vector2Int> GeneratePatrolWaypoints(DistrictDefinition def, int count)
        {
            var waypoints = new List<Vector2Int>();
            var gridWorld = GridWorld.Instance;
            if (gridWorld == null) return waypoints;

            for (int i = 0; i < count; i++)
            {
                for (int attempt = 0; attempt < 10; attempt++)
                {
                    int x = Random.Range(def.minX + 1, def.maxX);
                    int y = Random.Range(def.minY + 1, def.maxY);
                    if (gridWorld.CanEnter(x, y))
                    {
                        waypoints.Add(new Vector2Int(x, y));
                        break;
                    }
                }
            }

            return waypoints;
        }

        /// <summary>
        /// Remove goals for NPCs that have been destroyed or disabled.
        /// </summary>
        private static void CleanupDeadNpcs()
        {
            var toRemove = new List<NpcAI>();
            foreach (var kvp in _goals)
            {
                if (kvp.Key == null || !kvp.Key.gameObject.activeInHierarchy)
                    toRemove.Add(kvp.Key);
            }
            foreach (var npc in toRemove)
                _goals.Remove(npc);
        }

        /// <summary>
        /// Check if an NPC has an active goal. Used externally for queries.
        /// </summary>
        public static bool HasGoal(NpcAI npc)
        {
            return _goals.ContainsKey(npc) && _goals[npc].type != NpcGoalType.None;
        }

        /// <summary>
        /// Assign a flee goal to an NPC (called when NPC health drops low in combat).
        /// </summary>
        public static void AssignFleeGoal(NpcAI npc)
        {
            _goals[npc] = new NpcGoal
            {
                type = NpcGoalType.Flee,
                turnsRemaining = 10,
            };
        }

        /// <summary>Clear all goals for testing / game restart.</summary>
        public static void Clear()
        {
            _goals.Clear();
        }
    }
}
