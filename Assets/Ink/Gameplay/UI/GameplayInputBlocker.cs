namespace InkSim
{
    /// <summary>
    /// Centralized check for whether any modal UI overlay is open.
    /// Movement, spellcasting, and tile actions should be blocked when this returns true.
    /// </summary>
    public static class GameplayInputBlocker
    {
        public static bool IsAnyModalOpen =>
            InventoryUI.IsOpen ||
            DialogueRunner.IsOpen ||
            SaveLoadMenu.IsOpen ||
            MerchantUI.IsOpen ||
            TileInfoPanel.IsOpen;
    }
}
