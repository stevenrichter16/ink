using System.Collections.Generic;
using UnityEngine;
using static InkSim.TileInfoPanel;

namespace InkSim
{
    /// <summary>
    /// Adds an action to change an entity's faction via a popup selector.
    /// </summary>
    public class FactionChangeActionProvider : ITileActionProvider
    {
        public IEnumerable<TileAction> GetActions(GridWorld world)
        {
            yield return new TileAction(
                "Change Faction...",
                ActionCategory.Debug,
                (x, y) =>
                {
                    var entity = world.GetEntityAt(x, y);
                    if (entity == null) return;
                    var member = entity.GetComponent<FactionMember>();
                    if (member == null) return;
                    FactionSelectionPopup.Show(member);
                },
                (x, y) =>
                {
                    var entity = world.GetEntityAt(x, y);
                    if (entity == null || entity is PlayerController) return false;
                    return entity.GetComponent<FactionMember>() != null;
                },
                priority: -2
            );
        }
    }
}
