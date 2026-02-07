using System.Collections.Generic;
using UnityEngine;

namespace InkSim
{
    /// <summary>
    /// Each economic day, evaluates whether to spawn faction reinforcements,
    /// trigger raid parties in contested zones, or migrate NPCs based on prosperity.
    /// </summary>
    public static class DynamicSpawnService
    {
        // Population caps
        private const int MaxEnemiesPerFactionPerDistrict = 6;
        private const int GlobalEnemyCap = 50;

        // Last raid info (for quest generation)
        public static string LastRaidDistrictId { get; private set; }
        public static string LastRaidFactionId { get; private set; }
        public static int LastRaidDay { get; private set; }

        // Faction → enemy type mapping
        private static readonly Dictionary<string, string> FactionEnemyMap = new Dictionary<string, string>
        {
            { "faction_skeleton", "skeleton" },
            { "faction_ghost", "ghost" },
            { "faction_goblin", "goblin" },
            { "faction_demon", "demon" },
            { "faction_snake", "snake" },
            { "faction_slime", "slime" },
            { "faction_inkguard", "skeleton" },  // Inkguard spawns human soldiers (reuse skeleton stats)
            { "faction_inkbound", "ghost" },      // Inkbound spawns scribe sentinels (reuse ghost stats)
        };

        public static void Execute(int dayNumber)
        {
            var dcs = DistrictControlService.Instance;
            if (dcs == null) return;

            int totalEnemies = TurnManager.Instance != null ? TurnManager.Instance.GetEnemyCount() : 0;
            if (totalEnemies >= GlobalEnemyCap)
            {
                Debug.Log($"[DynamicSpawn] Day {dayNumber}: Global cap reached ({totalEnemies}/{GlobalEnemyCap}). Skipping spawns.");
                return;
            }

            SpawnReinforcements(dcs, dayNumber);
            SpawnRaidParties(dcs, dayNumber);
            HandleMigration(dcs, dayNumber);
        }

        /// <summary>
        /// When a faction has high control but few enemies in a district, spawn reinforcements.
        /// </summary>
        private static void SpawnReinforcements(DistrictControlService dcs, int dayNumber)
        {
            var gridWorld = GridWorld.Instance;
            if (gridWorld == null) return;

            for (int d = 0; d < dcs.States.Count; d++)
            {
                var state = dcs.States[d];
                var def = state.Definition;
                if (def == null) continue;

                for (int f = 0; f < dcs.Factions.Count; f++)
                {
                    float control = state.control[f];
                    if (control < 0.3f) continue; // Only factions with meaningful presence

                    var faction = dcs.Factions[f];
                    int factionEnemyCount = CountFactionEnemiesInDistrict(faction.id, def);

                    if (factionEnemyCount < 3 && control > 0.5f)
                    {
                        // Need reinforcements
                        if (factionEnemyCount >= MaxEnemiesPerFactionPerDistrict) continue;

                        string enemyId = GetEnemyIdForFaction(faction.id);
                        if (string.IsNullOrEmpty(enemyId)) continue;

                        Vector2Int? spawnPos = FindEmptyTile(def, gridWorld);
                        if (!spawnPos.HasValue) continue;

                        int level = Mathf.Max(1, Mathf.FloorToInt(control * 5f));
                        var spawned = EnemyFactory.Spawn(enemyId, spawnPos.Value.x, spawnPos.Value.y, gridWorld, level, faction, faction.DefaultRankId);
                        if (spawned != null)
                        {
                            Debug.Log($"[DynamicSpawn] Reinforcement: {enemyId} Lv{level} at ({spawnPos.Value.x},{spawnPos.Value.y}) for {faction.id} in {state.Id}");
                            string factionName = faction.displayName;
                            SimulationEventLog.ToastAtGrid($"{factionName} reinforcements!", SimulationEventLog.ColorSpawn, spawnPos.Value.x, spawnPos.Value.y);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// When territory is contested for 3+ days, spawn a raid party from the challenging faction.
        /// </summary>
        private static void SpawnRaidParties(DistrictControlService dcs, int dayNumber)
        {
            var gridWorld = GridWorld.Instance;
            if (gridWorld == null) return;

            foreach (var kvp in FactionStrategyService.ContestedDistricts)
            {
                if (kvp.Value < 3) continue; // Not contested long enough

                string districtId = kvp.Key;
                string attackerFactionId = FactionStrategyService.LastSkirmishAttackerFactionId;
                if (string.IsNullOrEmpty(attackerFactionId)) continue;

                // Find the district definition
                DistrictDefinition districtDef = null;
                for (int d = 0; d < dcs.States.Count; d++)
                {
                    if (dcs.States[d].Id == districtId)
                    {
                        districtDef = dcs.States[d].Definition;
                        break;
                    }
                }
                if (districtDef == null) continue;

                // Find attacker faction
                int attackerIdx = FactionStrategyService.GetFactionIndex(dcs, attackerFactionId);
                if (attackerIdx < 0) continue;

                var attackerFaction = dcs.Factions[attackerIdx];
                string enemyId = GetEnemyIdForFaction(attackerFactionId);
                if (string.IsNullOrEmpty(enemyId)) continue;

                // Spawn 2-3 raid enemies near the district border
                int raidSize = Random.Range(2, 4);
                int spawned = 0;

                for (int i = 0; i < raidSize; i++)
                {
                    Vector2Int? pos = FindEmptyTileAtBorder(districtDef, gridWorld);
                    if (!pos.HasValue) continue;

                    int level = Mathf.Max(2, kvp.Value); // Level scales with contest duration
                    var enemy = EnemyFactory.Spawn(enemyId, pos.Value.x, pos.Value.y, gridWorld, level, attackerFaction, "mid");
                    if (enemy != null)
                    {
                        spawned++;
                    }
                }

                if (spawned > 0)
                {
                    LastRaidDistrictId = districtId;
                    LastRaidFactionId = attackerFactionId;
                    LastRaidDay = dayNumber;
                    Debug.Log($"[DynamicSpawn] RAID: {spawned} {enemyId}(s) from {attackerFactionId} raiding {districtId}!");

                    // Major event — screen banner
                    string factionName = attackerFaction.displayName;
                    string districtDisplayName = districtDef.displayName;
                    SimulationEventLog.Banner($"\u26a0 {factionName} Raid on {districtDisplayName}!", SimulationEventLog.ColorRaid);
                }
            }
        }

        /// <summary>
        /// High-prosperity districts attract more entities; low-prosperity districts lose them.
        /// </summary>
        private static void HandleMigration(DistrictControlService dcs, int dayNumber)
        {
            for (int d = 0; d < dcs.States.Count; d++)
            {
                var state = dcs.States[d];
                var def = state.Definition;
                if (def == null) continue;

                if (state.prosperity > 0.8f)
                {
                    // Prosperous district — small chance to attract a wandering NPC
                    if (Random.value < 0.2f)
                    {
                        Debug.Log($"[DynamicSpawn] {state.Id} prosperity {state.prosperity:F2} — district thriving, could attract settlers.");
                        // NPC spawning could be added here in the future
                        // For now, prosperity effects are visible through the economy
                    }
                }
                else if (state.prosperity < 0.3f)
                {
                    // Decaying district — log the decline
                    Debug.Log($"[DynamicSpawn] {state.Id} prosperity {state.prosperity:F2} — district declining.");
                }
            }
        }

        /// <summary>
        /// Count enemies belonging to a faction within a district's bounds.
        /// </summary>
        private static int CountFactionEnemiesInDistrict(string factionId, DistrictDefinition def)
        {
            var gridWorld = GridWorld.Instance;
            if (gridWorld == null) return 0;

            int count = 0;
            for (int x = def.minX; x <= def.maxX; x++)
            {
                for (int y = def.minY; y <= def.maxY; y++)
                {
                    var entity = gridWorld.GetEntityAt(x, y);
                    if (entity == null) continue;

                    var fm = entity.GetComponent<FactionMember>();
                    if (fm != null && fm.faction != null && fm.faction.id == factionId)
                    {
                        var enemyAI = entity.GetComponent<EnemyAI>();
                        if (enemyAI != null)
                            count++;
                    }
                }
            }
            return count;
        }

        /// <summary>
        /// Find an empty walkable tile within a district's bounds.
        /// </summary>
        private static Vector2Int? FindEmptyTile(DistrictDefinition def, GridWorld gridWorld)
        {
            // Try random positions first (faster than scanning)
            for (int attempt = 0; attempt < 20; attempt++)
            {
                int x = Random.Range(def.minX + 1, def.maxX);
                int y = Random.Range(def.minY + 1, def.maxY);
                if (gridWorld.CanEnter(x, y))
                    return new Vector2Int(x, y);
            }

            // Fallback: systematic scan
            for (int x = def.minX + 1; x < def.maxX; x++)
            {
                for (int y = def.minY + 1; y < def.maxY; y++)
                {
                    if (gridWorld.CanEnter(x, y))
                        return new Vector2Int(x, y);
                }
            }

            return null;
        }

        /// <summary>
        /// Find an empty tile near the border of a district (for raids).
        /// </summary>
        private static Vector2Int? FindEmptyTileAtBorder(DistrictDefinition def, GridWorld gridWorld)
        {
            // Try along each border
            for (int attempt = 0; attempt < 20; attempt++)
            {
                int side = Random.Range(0, 4);
                int x, y;

                switch (side)
                {
                    case 0: // Top border
                        x = Random.Range(def.minX, def.maxX + 1);
                        y = def.maxY;
                        break;
                    case 1: // Bottom border
                        x = Random.Range(def.minX, def.maxX + 1);
                        y = def.minY;
                        break;
                    case 2: // Left border
                        x = def.minX;
                        y = Random.Range(def.minY, def.maxY + 1);
                        break;
                    default: // Right border
                        x = def.maxX;
                        y = Random.Range(def.minY, def.maxY + 1);
                        break;
                }

                if (gridWorld.CanEnter(x, y))
                    return new Vector2Int(x, y);
            }

            return FindEmptyTile(def, gridWorld);
        }

        /// <summary>
        /// Maps a faction ID to the enemy type it spawns.
        /// </summary>
        public static string GetEnemyIdForFaction(string factionId)
        {
            if (FactionEnemyMap.TryGetValue(factionId, out var enemyId))
                return enemyId;
            return null;
        }

        /// <summary>Clear state for testing / game restart.</summary>
        public static void Clear()
        {
            LastRaidDistrictId = null;
            LastRaidFactionId = null;
            LastRaidDay = 0;
        }
    }
}
