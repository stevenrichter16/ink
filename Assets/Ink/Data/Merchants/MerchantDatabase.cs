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
            
            _initialized = true;
            Debug.Log($"[MerchantDatabase] Initialized with {_profiles.Count} merchants");
        }
        
        private static void CreateGeneralStore()
        {
            var profile = ScriptableObject.CreateInstance<MerchantProfile>();
            profile.id = "general_store";
            profile.displayName = "General Store";
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
