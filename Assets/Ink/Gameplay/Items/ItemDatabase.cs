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

            // =============================================
            // === FACTION-THEMED ITEMS ===
            // =============================================

            // --- Inkbound (scholarly, ink-magic themed) ---
            Register(new ItemData("ink_quill", "Ink Quill", ItemType.Weapon, 131)
            {
                attackBonus = 1,
                speedBonus = 1,
                value = 35,
                rarity = ItemRarity.Common
            });
            Register(new ItemData("scribe_robes", "Scribe's Robes", ItemType.Armor, 42)
            {
                defenseBonus = 1,
                speedBonus = 2,
                value = 45,
                rarity = ItemRarity.Uncommon
            });
            Register(new ItemData("focus_crystal", "Focus Crystal", ItemType.Accessory, 129)
            {
                attackBonus = 2,
                healthBonus = 3,
                value = 120,
                rarity = ItemRarity.Rare
            });
            Register(new ItemData("ink_vial", "Ink Vial", ItemType.Consumable, 140)
            {
                healAmount = 3,
                stackable = true,
                maxStack = 15,
                value = 8,
                rarity = ItemRarity.Common
            });

            // --- Inkguard (military, forged metal) ---
            Register(new ItemData("halberd", "Halberd", ItemType.Weapon, 70)
            {
                attackBonus = 4,
                value = 90,
                rarity = ItemRarity.Rare
            });
            Register(new ItemData("tower_shield", "Tower Shield", ItemType.Armor, 86)
            {
                defenseBonus = 4,
                value = 80,
                rarity = ItemRarity.Rare
            });
            Register(new ItemData("captain_signet", "Captain's Signet", ItemType.Accessory, 130)
            {
                defenseBonus = 2,
                healthBonus = 5,
                value = 150,
                rarity = ItemRarity.Rare
            });
            Register(new ItemData("ration_pack", "Ration Pack", ItemType.Consumable, 38)
            {
                healAmount = 8,
                stackable = true,
                maxStack = 8,
                value = 20,
                rarity = ItemRarity.Common
            });

            // --- Ghost (ethereal, otherworldly) ---
            Register(new ItemData("spectral_blade", "Spectral Blade", ItemType.Weapon, 70)
            {
                attackBonus = 3,
                speedBonus = 1,
                value = 75,
                rarity = ItemRarity.Uncommon
            });
            Register(new ItemData("wraith_cloak", "Wraith Cloak", ItemType.Armor, 42)
            {
                defenseBonus = 2,
                speedBonus = 2,
                value = 70,
                rarity = ItemRarity.Uncommon
            });
            Register(new ItemData("soul_gem", "Soul Gem", ItemType.Currency, 129)
            {
                stackable = true,
                maxStack = 99,
                value = 80,
                rarity = ItemRarity.Rare
            });

            // --- Skeleton (bone-forged, ancient) ---
            Register(new ItemData("bone_blade", "Bone Blade", ItemType.Weapon, 70)
            {
                attackBonus = 3,
                value = 65,
                rarity = ItemRarity.Uncommon
            });
            Register(new ItemData("bone_plate", "Bone Plate Armor", ItemType.Armor, 42)
            {
                defenseBonus = 3,
                speedBonus = -1,
                value = 85,
                rarity = ItemRarity.Uncommon
            });
            Register(new ItemData("skull_talisman", "Skull Talisman", ItemType.Accessory, 130)
            {
                attackBonus = 1,
                defenseBonus = 1,
                healthBonus = 2,
                value = 90,
                rarity = ItemRarity.Uncommon
            });

            // --- Goblin (scrappy, improvised) ---
            Register(new ItemData("shiv", "Goblin Shiv", ItemType.Weapon, 71)
            {
                attackBonus = 2,
                speedBonus = 2,
                value = 40,
                rarity = ItemRarity.Common
            });
            Register(new ItemData("scrap_armor", "Scrap Armor", ItemType.Armor, 42)
            {
                defenseBonus = 2,
                value = 35,
                rarity = ItemRarity.Common
            });
            Register(new ItemData("lucky_tooth", "Lucky Tooth", ItemType.Accessory, 130)
            {
                speedBonus = 3,
                value = 55,
                rarity = ItemRarity.Uncommon
            });

            // --- Demon (corrupted, powerful, expensive) ---
            Register(new ItemData("hellfire_brand", "Hellfire Brand", ItemType.Weapon, 70)
            {
                attackBonus = 5,
                value = 200,
                rarity = ItemRarity.Legendary
            });
            Register(new ItemData("demon_plate", "Demon Plate", ItemType.Armor, 42)
            {
                defenseBonus = 5,
                speedBonus = -2,
                value = 250,
                rarity = ItemRarity.Legendary
            });
            Register(new ItemData("infernal_eye", "Infernal Eye", ItemType.Accessory, 130)
            {
                attackBonus = 3,
                healthBonus = 5,
                value = 300,
                rarity = ItemRarity.Legendary
            });
            Register(new ItemData("brimstone", "Brimstone", ItemType.Consumable, 129)
            {
                healAmount = 15,
                stackable = true,
                maxStack = 5,
                value = 50,
                rarity = ItemRarity.Rare
            });

            // --- Snake (venomous, natural) ---
            Register(new ItemData("venom_fang", "Venom Fang", ItemType.Weapon, 71)
            {
                attackBonus = 2,
                speedBonus = 1,
                value = 45,
                rarity = ItemRarity.Common
            });
            Register(new ItemData("snakeskin_vest", "Snakeskin Vest", ItemType.Armor, 42)
            {
                defenseBonus = 1,
                speedBonus = 3,
                value = 50,
                rarity = ItemRarity.Uncommon
            });
            Register(new ItemData("antivenom", "Antivenom", ItemType.Consumable, 141)
            {
                healAmount = 7,
                stackable = true,
                maxStack = 10,
                value = 22,
                rarity = ItemRarity.Common
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
