namespace InkSim
{
    /// <summary>
    /// Categories of items that determine behavior and equip slots.
    /// </summary>
    public enum ItemType
    {
        Consumable,   // Potions — use and destroy
        Weapon,       // Equip to weapon slot
        Armor,        // Equip to armor slot
        Accessory,    // Equip to accessory slot
        KeyItem,      // Keys — quest items
        Currency      // Coins, gems — stackable, no equip
    }
}
