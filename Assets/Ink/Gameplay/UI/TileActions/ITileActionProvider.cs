using System.Collections.Generic;

namespace InkSim
{
    /// <summary>
    /// Interface for modular tile action providers.
    /// Implement this to add custom actions to the TileInfoPanel.
    /// </summary>
    public interface ITileActionProvider
    {
        /// <summary>
        /// Get all actions this provider offers.
        /// </summary>
        /// <param name="world">The grid world reference</param>
        /// <returns>Enumerable of tile actions</returns>
        IEnumerable<TileInfoPanel.TileAction> GetActions(GridWorld world);
    }
}
