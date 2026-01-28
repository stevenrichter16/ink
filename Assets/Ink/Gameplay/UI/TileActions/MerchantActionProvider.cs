using System.Collections.Generic;
using UnityEngine;
using static InkSim.TileInfoPanel;

namespace InkSim
{
    /// <summary>
    /// Provides merchant-related tile actions (Trade).
    /// </summary>
    public class MerchantActionProvider : ITileActionProvider
    {
        public IEnumerable<TileAction> GetActions(GridWorld world)
        {
            // Trade with merchant
            yield return new TileAction(
                "Trade",
                ActionCategory.Combat, // Using Combat category to put it near top
                (x, y) => {
                    var entity = world.GetEntityAt(x, y);
                    if (entity == null) return;
                    
                    var merchant = entity.GetComponent<Merchant>();
                    if (merchant == null) return;
                    
                    var player = Object.FindObjectOfType<PlayerController>();
                    if (player == null) return;
                    
                    MerchantUI.Open(merchant, player);
                },
                (x, y) => {
                    var entity = world.GetEntityAt(x, y);
                    if (entity == null) return false;
                    return entity.GetComponent<Merchant>() != null;
                },
                priority: -1  // High priority (shows first in Combat category)
            );
        }
    }
}
