using System.Collections.Generic;

namespace InkSim
{
    /// <summary>
    /// Static registry of all item definitions.
    /// Call Initialize() at game start.
    /// </summary>
    public static class ItemDatabase
    {
        private static Dictionary<string, ItemData> _items;
        private static bool _initialized = false;

        /// <summary>
        /// Initialize the database with all game items.
        /// </summary>
        public static void Initialize()
        {
            if (_initialized) return;

            _items = new Dictionary<string, ItemData>();

            // === WEAPONS ===
            Register(new ItemData("sword", "Sword", ItemType.Weapon, 70)
            {
                attackBonus = 2,
                value = 50
            });

            Register(new ItemData("dagger", "Dagger", ItemType.Weapon, 71)
            {
                attackBonus = 1,
                value = 25
            });

            // === ARMOR ===
            Register(new ItemData("leather_armor", "Leather Armor", ItemType.Armor, 42)
            {
                defenseBonus = 1,
                value = 30
            });

            Register(new ItemData("iron_armor", "Iron Armor", ItemType.Armor, 42)
            {
                defenseBonus = 2,
                value = 60
            });

            Register(new ItemData("steel_armor", "Steel Armor", ItemType.Armor, 42)
            {
                defenseBonus = 3,
                value = 120
            });

            Register(new ItemData("shield", "Shield", ItemType.Armor, 86)
            {
                defenseBonus = 2,
                value = 40
            });


            // === ACCESSORIES ===
            Register(new ItemData("ring", "Ring of Power", ItemType.Accessory, 130)
            {
                attackBonus = 1,
                healthBonus = 2,
                value = 100
            });

            // === CONSUMABLES ===
            Register(new ItemData("potion", "Health Potion", ItemType.Consumable, 141)
            {
                healAmount = 5,
                stackable = true,
                maxStack = 10,
                value = 15
            });

            Register(new ItemData("potion_large", "Large Potion", ItemType.Consumable, 142)
            {
                healAmount = 10,
                stackable = true,
                maxStack = 5,
                value = 30
            });

            // === CURRENCY ===
            Register(new ItemData("coin", "Coin", ItemType.Currency, 128)
            {
                stackable = true,
                maxStack = 9999,
                value = 1
            });

            Register(new ItemData("gem", "Gem", ItemType.Currency, 129)
            {
                stackable = true,
                maxStack = 999,
                value = 50
            });

            Register(new ItemData("ink", "Ink", ItemType.Currency, 140)
            {
                stackable = true,
                maxStack = 999,
                value = 0
            });

            // === KEY ITEMS ===
            Register(new ItemData("key", "Key", ItemType.KeyItem, 87)
            {
                stackable = true,
                maxStack = 99,
                value = 0
            });

            _initialized = true;
        }

        private static void Register(ItemData item)
        {
            _items[item.id] = item;
        }

        /// <summary>
        /// Get item data by ID.
        /// </summary>
        public static ItemData Get(string id)
        {
            if (!_initialized) Initialize();
            return _items.TryGetValue(id, out var data) ? data : null;
        }

        /// <summary>
        /// Check if an item exists.
        /// </summary>
        public static bool Exists(string id)
        {
            if (!_initialized) Initialize();
            return _items.ContainsKey(id);
        }

        /// <summary>
        /// Get all item IDs.
        /// </summary>
        public static IEnumerable<string> AllIds
        {
            get
            {
                if (!_initialized) Initialize();
                return _items.Keys;
            }
        }

        /// <summary>
        /// Create a new instance of an item.
        /// </summary>
        public static ItemInstance CreateInstance(string id, int quantity = 1)
        {
            var data = Get(id);
            if (data == null) return null;
            return new ItemInstance(data, quantity);
        }
    }
}
