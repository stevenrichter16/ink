using System.Collections.Generic;
using UnityEngine;
using static InkSim.TileInfoPanel;

namespace InkSim
{
    /// <summary>
    /// Provides spawn-related tile actions (enemies and items).
    /// Dynamically generates actions from EnemyDatabase.
    /// </summary>
    public class SpawnActionProvider : ITileActionProvider
    {
        public IEnumerable<TileAction> GetActions(GridWorld world)
        {
            // === SPAWN ENEMIES (from database) ===
            int enemyPriority = 0;
            foreach (var enemyData in EnemyDatabase.All)
            {
                var data = enemyData; // Capture for closure
                yield return new TileAction(
                    $"Spawn {data.displayName}",
                    ActionCategory.SpawnEnemy,
                    (x, y) => SpawnEnemyAt(world, data, x, y),
                    (x, y) => IsSpawnable(world, x, y),
                    priority: enemyPriority++
                );
            }

            // === SPAWN ITEMS ===
            yield return new TileAction(
                "Spawn Potion",
                ActionCategory.SpawnItem,
                (x, y) => ItemPickup.CreateFromLoot("potion", x, y, 1, world.tileSize),
                (x, y) => IsWalkable(world, x, y),
                priority: 0
            );

            yield return new TileAction(
                "Spawn Sword",
                ActionCategory.SpawnItem,
                (x, y) => ItemPickup.CreateFromLoot("sword", x, y, 1, world.tileSize),
                (x, y) => IsWalkable(world, x, y),
                priority: 1
            );

            yield return new TileAction(
                "Spawn Coins",
                ActionCategory.SpawnItem,
                (x, y) => ItemPickup.CreateFromLoot("coin", x, y, 10, world.tileSize),
                (x, y) => IsWalkable(world, x, y),
                priority: 2
            );
        }

        private static bool IsWalkable(GridWorld world, int x, int y)
        {
            return world != null && world.IsWalkable(x, y);
        }

        private static bool IsSpawnable(GridWorld world, int x, int y)
        {
            if (!IsWalkable(world, x, y)) return false;

            var occupant = world.GetEntityAt(x, y);
            if (occupant == null) return true;

            return occupant.GetComponent<PlayerController>() == null;
        }

        private static void SpawnEnemyAt(GridWorld world, EnemyData data, int x, int y)
        {
            if (world == null || data == null) return;

            var occupant = world.GetEntityAt(x, y);
            if (occupant != null)
            {
                if (occupant.GetComponent<PlayerController>() != null)
                {
                    Debug.LogWarning("[SpawnActionProvider] Cannot replace player.");
                    return;
                }

                if (occupant is EnemyAI enemy)
                    TurnManager.Instance?.UnregisterEnemy(enemy);
                else if (occupant is NpcAI npc)
                    TurnManager.Instance?.UnregisterNPC(npc);

                world.ClearOccupant(x, y);
                occupant.gameObject.SetActive(false);
            }

            EnemyFactory.SpawnFromData(data, x, y, world);
        }
    }
}
