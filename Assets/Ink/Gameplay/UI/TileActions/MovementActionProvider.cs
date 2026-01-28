using System.Collections.Generic;
using UnityEngine;
using static InkSim.TileInfoPanel;

namespace InkSim
{
    /// <summary>
    /// Provides movement-related tile actions (Teleport, Swap, Push, etc.)
    /// </summary>
    public class MovementActionProvider : ITileActionProvider
    {
        public IEnumerable<TileAction> GetActions(GridWorld world)
        {
            // Teleport player to tile
            yield return new TileAction(
                "Teleport Here",
                ActionCategory.Movement,
                (x, y) => {
                    var player = Object.FindObjectOfType<PlayerController>();
                    if (player != null)
                    {
                        world.ClearOccupant(player.gridX, player.gridY);
                        player.gridX = x;
                        player.gridY = y;
                        player.transform.localPosition = new Vector3(x * world.tileSize, y * world.tileSize, 0);
                        world.SetOccupant(x, y, player);
                    }
                },
                (x, y) => IsEmptyWalkable(world, x, y),
                priority: 0
            );

            // Swap player with entity
            yield return new TileAction(
                "Swap with Player",
                ActionCategory.Movement,
                (x, y) => {
                    var player = Object.FindObjectOfType<PlayerController>();
                    var entity = world.GetEntityAt(x, y);
                    if (player != null && entity != null && entity != player)
                    {
                        int playerX = player.gridX;
                        int playerY = player.gridY;
                        
                        // Move entity to player position
                        world.ClearOccupant(x, y);
                        entity.gridX = playerX;
                        entity.gridY = playerY;
                        entity.transform.localPosition = new Vector3(playerX * world.tileSize, playerY * world.tileSize, 0);
                        world.SetOccupant(playerX, playerY, entity);
                        
                        // Move player to entity position
                        world.ClearOccupant(playerX, playerY);
                        player.gridX = x;
                        player.gridY = y;
                        player.transform.localPosition = new Vector3(x * world.tileSize, y * world.tileSize, 0);
                        world.SetOccupant(x, y, player);
                    }
                },
                (x, y) => {
                    var entity = world.GetEntityAt(x, y);
                    return entity != null && !(entity is PlayerController);
                },
                priority: 1
            );
        }

        private static bool IsEmptyWalkable(GridWorld world, int x, int y)
        {
            return world != null && world.IsWalkable(x, y) && world.GetEntityAt(x, y) == null;
        }
    }
}
