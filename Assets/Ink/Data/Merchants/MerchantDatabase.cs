using System.Collections.Generic;
using UnityEngine;

namespace InkSim
{
    /// <summary>
    /// Static registry of merchant profiles.
    /// Auto-populates on first access.
    /// </summary>
    public static class MerchantDatabase
    {
        private static Dictionary<string, MerchantProfile> _profiles;
        private static bool _initialized;

        public static IEnumerable<string> AllIds
        {
            get
            {
                if (!_initialized) Initialize();
                return _profiles.Keys;
            }
        }


        private static void Initialize()
        {
            _profiles = new Dictionary<string, MerchantProfile>();

            // Create default merchants programmatically
            CreateGeneralStore();
            CreateWeaponsmith();
            CreateScribeShop();
            CreateGoblinFence();
            CreateBoneArmory();
            CreateDemonBroker();
            CreateSnakeHerbalist();

            _initialized = true;
            Debug.Log($"[MerchantDatabase] Initialized with {_profiles.Count} merchants");
        }

        private static void CreateGeneralStore()
        {
            var profile = ScriptableObject.CreateInstance<MerchantProfile>();
            profile.id = "general_store";
            profile.displayName = "General Store";
            profile.factionId = "faction_inkbound";
            profile.homeDistrictId = "district_market";
            profile.buyMultiplier = 1.0f;
            profile.sellMultiplier = 0.5f;
            profile.restockEachVisit = true;
            profile.stock = new List<MerchantStockEntry>
            {
                new MerchantStockEntry("potion", 5),
                new MerchantStockEntry("leather_armor", 1),
                new MerchantStockEntry("key", 3)
            };

            _profiles[profile.id] = profile;
        }

        private static void CreateWeaponsmith()
        {
            var profile = ScriptableObject.CreateInstance<MerchantProfile>();
            profile.id = "weaponsmith";
            profile.displayName = "Weaponsmith";
            profile.factionId = "faction_inkguard";
            profile.homeDistrictId = "district_temple";
            profile.buyMultiplier = 1.2f;  // Slight markup
            profile.sellMultiplier = 0.6f; // Better sell prices for weapons
            profile.restockEachVisit = true;
            profile.stock = new List<MerchantStockEntry>
            {
                new MerchantStockEntry("sword", 2),
                new MerchantStockEntry("iron_armor", 1),
                new MerchantStockEntry("steel_armor", 1)
            };

            _profiles[profile.id] = profile;
        }

        private static void CreateScribeShop()
        {
            var profile = ScriptableObject.CreateInstance<MerchantProfile>();
            profile.id = "scribe_shop";
            profile.displayName = "Scribe's Study";
            profile.factionId = "faction_inkbound_scribes";
            profile.homeDistrictId = "district_market";
            profile.buyMultiplier = 0.9f;  // Slight discount — cooperative faction
            profile.sellMultiplier = 0.4f;
            profile.restockEachVisit = true;
            profile.stock = new List<MerchantStockEntry>
            {
                new MerchantStockEntry("ink", 10),
                new MerchantStockEntry("ink_quill", 1),
                new MerchantStockEntry("ink_vial", 8),
                new MerchantStockEntry("scribe_robes", 1),
                new MerchantStockEntry("focus_crystal", 1),
                new MerchantStockEntry("potion", 3)
            };

            _profiles[profile.id] = profile;
        }

        private static void CreateGoblinFence()
        {
            var profile = ScriptableObject.CreateInstance<MerchantProfile>();
            profile.id = "goblin_fence";
            profile.displayName = "Goblin Fence";
            profile.factionId = "faction_goblin";
            profile.homeDistrictId = "district_slums";
            profile.buyMultiplier = 0.7f;  // Cheap but sketchy
            profile.sellMultiplier = 0.3f; // Low buyback
            profile.restockEachVisit = true;
            profile.stock = new List<MerchantStockEntry>
            {
                new MerchantStockEntry("shiv", 2),
                new MerchantStockEntry("scrap_armor", 2),
                new MerchantStockEntry("lucky_tooth", 1),
                new MerchantStockEntry("dagger", 2),
                new MerchantStockEntry("leather_armor", 1),
                new MerchantStockEntry("key", 2)
            };

            _profiles[profile.id] = profile;
        }

        private static void CreateBoneArmory()
        {
            var profile = ScriptableObject.CreateInstance<MerchantProfile>();
            profile.id = "bone_armory";
            profile.displayName = "Bone Armory";
            profile.factionId = "faction_skeleton";
            profile.homeDistrictId = "district_ironkeep";
            profile.buyMultiplier = 1.4f;  // Military prices
            profile.sellMultiplier = 0.7f; // Fair trade for weapons
            profile.restockEachVisit = true;
            profile.stock = new List<MerchantStockEntry>
            {
                new MerchantStockEntry("bone_blade", 2),
                new MerchantStockEntry("bone_plate", 1),
                new MerchantStockEntry("skull_talisman", 1),
                new MerchantStockEntry("sword", 1),
                new MerchantStockEntry("shield", 1),
                new MerchantStockEntry("iron_armor", 1)
            };
            profile.acceptedTypes = new List<ItemType> { ItemType.Weapon, ItemType.Armor };

            _profiles[profile.id] = profile;
        }

        private static void CreateDemonBroker()
        {
            var profile = ScriptableObject.CreateInstance<MerchantProfile>();
            profile.id = "demon_broker";
            profile.displayName = "Demon Broker";
            profile.factionId = "faction_demon";
            profile.homeDistrictId = "district_boneyard";
            profile.buyMultiplier = 1.5f;  // Premium prices
            profile.sellMultiplier = 0.8f; // Pays well
            profile.restockEachVisit = true;
            profile.stock = new List<MerchantStockEntry>
            {
                new MerchantStockEntry("hellfire_brand", 1),
                new MerchantStockEntry("demon_plate", 1),
                new MerchantStockEntry("infernal_eye", 1),
                new MerchantStockEntry("brimstone", 3),
                new MerchantStockEntry("gem", 2)
            };

            _profiles[profile.id] = profile;
        }

        private static void CreateSnakeHerbalist()
        {
            var profile = ScriptableObject.CreateInstance<MerchantProfile>();
            profile.id = "snake_herbalist";
            profile.displayName = "Herbalist";
            profile.factionId = "faction_snake";
            profile.homeDistrictId = "district_wilds";
            profile.buyMultiplier = 1.1f;
            profile.sellMultiplier = 0.5f;
            profile.restockEachVisit = true;
            profile.stock = new List<MerchantStockEntry>
            {
                new MerchantStockEntry("antivenom", 6),
                new MerchantStockEntry("venom_fang", 1),
                new MerchantStockEntry("snakeskin_vest", 1),
                new MerchantStockEntry("potion", 5),
                new MerchantStockEntry("potion_large", 2)
            };

            _profiles[profile.id] = profile;
        }

        /// <summary>
        /// Get a merchant profile by ID.
        /// </summary>
        public static MerchantProfile Get(string id)
        {
            if (!_initialized) Initialize();

            if (string.IsNullOrEmpty(id)) return null;
            return _profiles.TryGetValue(id, out var profile) ? profile : null;
        }

        /// <summary>
        /// Register a custom merchant profile.
        /// </summary>
        public static void Register(MerchantProfile profile)
        {
            if (!_initialized) Initialize();

            if (profile != null && !string.IsNullOrEmpty(profile.id))
            {
                _profiles[profile.id] = profile;
            }
        }
    }
}
