using System.Collections.Generic;
using UnityEngine;
using static InkSim.TileInfoPanel;

namespace InkSim
{
    /// <summary>
    /// Provides a Talk action for NPCs that have a DialogueRunner.
    /// </summary>
    public class DialogueActionProvider : ITileActionProvider
    {
        public IEnumerable<TileAction> GetActions(GridWorld world)
        {
            yield return new TileAction(
                "Talk",
                ActionCategory.Combat,
                (x, y) =>
                {
                    var entity = world.GetEntityAt(x, y);
                    if (entity == null) return;

                    var runner = entity.GetComponent<DialogueRunner>();
                    if (runner == null) return;

                    var player = Object.FindObjectOfType<PlayerController>();
                    if (player == null) return;

                    runner.Begin(player);
                },
                (x, y) =>
                {
                    var entity = world.GetEntityAt(x, y);
                    if (entity == null) return false;
                    return entity.GetComponent<DialogueRunner>() != null;
                },
                priority: -2 // Show before trade
            );
        }
    }
}
