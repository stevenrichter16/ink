using System.Collections.Generic;
using UnityEngine;
using static InkSim.TileInfoPanel;

namespace InkSim
{
    /// <summary>
    /// Provides debug/cheat tile actions (Toggle Walkable, Log State, etc.)
    /// </summary>
    public class DebugActionProvider : ITileActionProvider
    {
        public IEnumerable<TileAction> GetActions(GridWorld world)
        {
            // Toggle walkable
            yield return new TileAction(
                "Toggle Walkable",
                ActionCategory.Debug,
                (x, y) => {
                    bool current = world.IsWalkable(x, y);
                    world.SetWalkable(x, y, !current);
                    Debug.Log($"[Debug] Tile ({x}, {y}) walkable: {!current}");
                },
                (x, y) => true, // Always available
                priority: 0
            );

            // Log tile state
            yield return new TileAction(
                "Log Tile Info",
                ActionCategory.Debug,
                (x, y) => {
                    var entity = world.GetEntityAt(x, y);
                    var item = world.GetItemAt(x, y);
                    bool walkable = world.IsWalkable(x, y);
                    
                    Debug.Log($"[Debug] Tile ({x}, {y}):");
                    Debug.Log($"  Walkable: {walkable}");
                    Debug.Log($"  Entity: {(entity != null ? entity.GetType().Name : "None")}");
                    Debug.Log($"  Item: {(item != null ? item.itemType.ToString() : "None")}");
                    
                    if (entity is EnemyAI enemy)
                    {
                        Debug.Log($"  Enemy HP: {enemy.currentHealth}/{enemy.maxHealth}");
                        Debug.Log($"  Enemy State: {enemy.state}");
                        Debug.Log($"  Loot Table: {enemy.lootTableId}");
                    }
                },
                (x, y) => true, // Always available
                priority: 1
            );

            // Stats (prints detailed entity stats)
            yield return new TileAction(
                "Stats",
                ActionCategory.Debug,
                (x, y) => {
                    var entity = world.GetEntityAt(x, y);
                    if (entity == null)
                    {
                        Debug.Log($"[Stats] No entity at ({x},{y}).");
                        return;
                    }

                    var sb = new System.Text.StringBuilder();
                    sb.AppendLine($"[Stats] Entity at ({x},{y}) :: {entity.GetType().Name} :: {entity.name}");

                    if (entity is PlayerController pc)
                    {
                        sb.AppendLine($"  HP {pc.currentHealth}/{pc.MaxHealth}");
                        sb.AppendLine($"  ATK {pc.AttackDamage}  DEF {pc.Defense}");
                    }
                    else if (entity is EnemyAI enemy)
                    {
                        sb.AppendLine($"  HP {enemy.currentHealth}/{enemy.maxHealth}");
                        sb.AppendLine($"  DMG {enemy.attackDamage}  State {enemy.state}");
                        sb.AppendLine($"  LootTable {enemy.lootTableId}");
                    }
                    else if (entity is NpcAI npc)
                    {
                        sb.AppendLine($"  HP {npc.currentHealth}/{npc.maxHealth}");
                        sb.AppendLine($"  DMG {npc.attackDamage}");
                    }

                    var fm = entity.GetComponent<FactionMember>();
                    if (fm != null && fm.faction != null)
                    {
                        int rep = ReputationSystem.GetRep(fm.faction.id);
                        sb.AppendLine($"  Faction {fm.faction.displayName}  Rep {rep}  State {fm.state}");
                    }

                    var level = entity.GetComponent<Levelable>();
                    if (level != null)
                    {
                        sb.AppendLine($"  Level {level.Level}  XP {level.Xp}/{level.XpToNextLevel}");
                    }

                    Debug.Log(sb.ToString());
                },
                (x, y) => world.GetEntityAt(x, y) != null,
                priority: -1  // show near top of Debug actions
            );

            // Clear tile (remove entity and items)
            yield return new TileAction(
                "Clear Tile",
                ActionCategory.Debug,
                (x, y) => {
                    var entity = world.GetEntityAt(x, y);
                    if (entity != null && !(entity is PlayerController))
                    {
                        world.ClearOccupant(x, y);
                        Object.Destroy(entity.gameObject);
                    }
                    
                    // Also destroy any pickups
                    foreach (var pickup in UnityEngine.Object.FindObjectsByType<ItemPickup>(FindObjectsSortMode.None))
                    {
                        if (pickup.gridX == x && pickup.gridY == y)
                            Object.Destroy(pickup.gameObject);
                    }
                },
                (x, y) => {
                    var entity = world.GetEntityAt(x, y);
                    bool hasNonPlayerEntity = entity != null && !(entity is PlayerController);
                    bool hasPickup = false;
                    foreach (var pickup in UnityEngine.Object.FindObjectsByType<ItemPickup>(FindObjectsSortMode.None))
                    {
                        if (pickup.gridX == x && pickup.gridY == y)
                        {
                            hasPickup = true;
                            break;
                        }
                    }
                    return hasNonPlayerEntity || hasPickup;
                },
                priority: 2
            );
        }
    }
}
