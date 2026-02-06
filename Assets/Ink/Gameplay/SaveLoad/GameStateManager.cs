using System.Collections.Generic;
using UnityEngine;

namespace InkSim
{
    /// <summary>
    /// Bridges live game objects and serializable GameState.
    /// Handles collecting state for saving and applying state for loading.
    /// </summary>
    public static class GameStateManager
    {
        #region Collect State
        
        /// <summary>
        /// Collect current game state from all relevant objects.
        /// </summary>
        public static GameState CollectState()
        {
            var state = GameState.Create();
            
            // Player
            var player = Object.FindObjectOfType<PlayerController>();
            if (player != null)
            {
                state.player = CollectPlayerData(player);
                state.quests = CollectQuests(player.questLog);
            }
            else
            {
                Debug.LogWarning("[GameStateManager] No player found during save");
                state.player = new PlayerSaveData();
                state.quests = new List<QuestSaveData>();
            }
            
            // Enemies
            state.enemies = CollectEnemyData();
            
            // Ground items
            state.groundItems = CollectGroundItems();

            // Economy
            state.economy = CollectEconomicData();
            
            Debug.Log($"[GameStateManager] Collected: Player({state.player.gridX},{state.player.gridY}), " +
                      $"{state.enemies.Count} enemies, {state.groundItems.Count} items");
            
            return state;
        }
        
private static PlayerSaveData CollectPlayerData(PlayerController player)
        {
            var data = new PlayerSaveData
            {
                gridX = player.gridX,
                gridY = player.gridY,
                currentHealth = player.currentHealth,
                maxHealth = player.MaxHealth,
                baseAttack = player.baseAttack,
                baseDefense = player.baseDefense,
                coins = player.Coins,
                keys = player.Keys,
                level = player.levelable?.Level ?? 1,
            };
            
            // Inventory
            if (player.inventory != null)
            {
                foreach (var item in player.inventory.items)
                {
                    data.inventory.Add(new InventorySlotData(item.Id, item.quantity));
                }
            }
            
            // Equipment
            if (player.equipment != null)
            {
                data.equippedWeapon = player.equipment.weapon?.Id ?? "";
                data.equippedArmor = player.equipment.armor?.Id ?? "";
                data.equippedAccessory = player.equipment.accessory?.Id ?? "";
            }
            
            return data;
        }
        
        private static List<EnemySaveData> CollectEnemyData()
        {
            var list = new List<EnemySaveData>();
            
            foreach (var enemy in Object.FindObjectsOfType<EnemyAI>())
            {
                string enemyId = enemy.lootTableId ?? "slime";
                int level = enemy.levelable?.Level ?? 1;
                list.Add(new EnemySaveData(
                    enemyId,
                    enemy.gridX,
                    enemy.gridY,
                    enemy.currentHealth,
                    level,
                    enemy.state.ToString()
                ));
            }
            
            return list;
        }
        
        private static List<ItemPickupSaveData> CollectGroundItems()
        {
            var list = new List<ItemPickupSaveData>();
            
            foreach (var pickup in Object.FindObjectsOfType<ItemPickup>())
            {
                list.Add(new ItemPickupSaveData(
                    pickup.itemId,
                    pickup.gridX,
                    pickup.gridY,
                    pickup.quantity
                ));
            }
            
            return list;
        }

        private static List<QuestSaveData> CollectQuests(QuestLog questLog)
        {
            if (questLog == null)
                return new List<QuestSaveData>();
            
            return questLog.ToSaveData();
        }

        private static EconomicSaveData CollectEconomicData()
        {
            var econ = new EconomicSaveData();

            var policies = TaxRegistry.GetAllPolicies();
            for (int i = 0; i < policies.Count; i++)
            {
                var p = policies[i];
                econ.activeTaxPolicies.Add(new TaxPolicySaveData
                {
                    id = p.id,
                    type = p.type,
                    rate = p.rate,
                    jurisdictionId = p.jurisdictionId,
                    exemptFactions = p.exemptFactions != null ? new List<string>(p.exemptFactions) : new List<string>(),
                    exemptItems = p.exemptItems != null ? new List<string>(p.exemptItems) : new List<string>(),
                    targetItems = p.targetItems != null ? new List<string>(p.targetItems) : new List<string>(),
                    turnsRemaining = p.turnsRemaining,
                    sourceLayerId = p.sourceLayerId
                });
            }

            var relations = TradeRelationRegistry.GetAll();
            for (int i = 0; i < relations.Count; i++)
            {
                var r = relations[i];
                if (r == null) continue;
                econ.tradeRelations.Add(new TradeRelationSaveData
                {
                    sourceFactionId = r.sourceFactionId,
                    targetFactionId = r.targetFactionId,
                    status = r.status,
                    tariffRate = r.tariffRate,
                    bannedItems = r.bannedItems != null ? new List<string>(r.bannedItems) : new List<string>(),
                    exclusiveItems = r.exclusiveItems != null ? new List<string>(r.exclusiveItems) : new List<string>()
                });
            }

            var dcs = DistrictControlService.Instance;
            if (dcs != null && dcs.States != null)
            {
                for (int i = 0; i < dcs.States.Count; i++)
                {
                    var s = dcs.States[i];
                    var saved = new DistrictEconomicSaveData
                    {
                        districtId = s.Id,
                        treasury = s.treasury,
                        corruption = s.corruption,
                        economicActivity = s.economicActivity
                    };

                    if (s.itemSupply != null)
                    {
                        foreach (var kvp in s.itemSupply)
                            saved.itemSupply.Add(new ItemFloatPair(kvp.Key, kvp.Value));
                    }
                    if (s.itemDemand != null)
                    {
                        foreach (var kvp in s.itemDemand)
                            saved.itemDemand.Add(new ItemFloatPair(kvp.Key, kvp.Value));
                    }

                    econ.districtEconomics.Add(saved);
                }
            }

            var events = EconomicEventService.GetAllEvents();
            for (int i = 0; i < events.Count; i++)
            {
                var ev = events[i];
                if (ev == null) continue;
                econ.activeEvents.Add(new DemandEventSaveData
                {
                    id = ev.id,
                    itemId = ev.itemId,
                    demandMultiplier = ev.demandMultiplier,
                    durationDays = ev.durationDays,
                    districtId = ev.districtId,
                    description = ev.description
                });
            }

            // Faction reputation
            var dcs2 = DistrictControlService.Instance;
            if (dcs2 != null && dcs2.Factions != null)
            {
                for (int i = 0; i < dcs2.Factions.Count; i++)
                {
                    var faction = dcs2.Factions[i];
                    if (faction == null || string.IsNullOrEmpty(faction.id)) continue;
                    int rep = ReputationSystem.GetRep(faction.id);
                    econ.factionReputation.Add(new FactionRepSaveData(faction.id, rep));
                }
            }

            return econ;
        }

        #endregion

        #region Apply State
        
        /// <summary>
        /// Apply loaded state to restore the game world.
        /// </summary>
        public static bool ApplyState(GameState state)
        {
            if (state == null)
            {
                Debug.LogError("[GameStateManager] Cannot apply null state");
                return false;
            }
            
            Debug.Log("[GameStateManager] Applying saved state...");
            
            // 1. Clear current dynamic objects
            ClearCurrentState();
            
            // 2. Restore player
            var player = Object.FindObjectOfType<PlayerController>();
            if (player != null && state.player != null)
            {
                ApplyPlayerData(player, state.player);
                ApplyQuests(player.questLog, state.quests);
            }
            
            // 3. Restore enemies
            RestoreEnemies(state.enemies);
            
            // 4. Restore ground items
            RestoreGroundItems(state.groundItems);

            // 5. Restore economy
            ApplyEconomicData(state.economy);
            
            // 6. Refresh UI
            RefreshAllUI();
            
            Debug.Log("[GameStateManager] State applied successfully");
            return true;
        }
        
        private static void ClearCurrentState()
        {
            var gridWorld = GridWorld.Instance;
            var turnManager = TurnManager.Instance;
            
            // Destroy all enemies
            var enemies = Object.FindObjectsOfType<EnemyAI>();
            foreach (var enemy in enemies)
            {
                if (gridWorld != null)
                    gridWorld.ClearOccupant(enemy.gridX, enemy.gridY);
                
                if (turnManager != null)
                    turnManager.UnregisterEnemy(enemy);
                
                Object.Destroy(enemy.gameObject);
            }
            
            // Destroy all ground items
            var pickups = Object.FindObjectsOfType<ItemPickup>();
            foreach (var pickup in pickups)
            {
                Object.Destroy(pickup.gameObject);
            }
            
            Debug.Log($"[GameStateManager] Cleared {enemies.Length} enemies, {pickups.Length} items");
        }
        
private static void ApplyPlayerData(PlayerController player, PlayerSaveData data)
        {
            var gridWorld = GridWorld.Instance;
            float tileSize = gridWorld?.tileSize ?? 1f;
            
            // Clear old grid position
            if (gridWorld != null)
                gridWorld.ClearOccupant(player.gridX, player.gridY);
            
            // Set new position
            player.gridX = data.gridX;
            player.gridY = data.gridY;
            player.transform.localPosition = new Vector3(data.gridX * tileSize, data.gridY * tileSize, 0);
            
            // Register at new position
            if (gridWorld != null)
                gridWorld.SetOccupant(data.gridX, data.gridY, player);
            
            // Restore level/XP first (affects MaxHealth)
            if (player.levelable != null)
            {
                player.levelable.SetLevelAndXp(data.level, data.xp);
            }
            
            // Restore stats
            player.currentHealth = data.currentHealth;
            player.baseAttack = data.baseAttack;
            player.baseDefense = data.baseDefense;
            // Clear and restore inventory
            if (player.inventory != null)
            {
                player.inventory.Clear();
                foreach (var slot in data.inventory)
                {
                    player.inventory.AddItem(slot.itemId, slot.quantity);
                }
            }
            
            // Restore equipment
            if (player.equipment != null && player.inventory != null)
            {
                // Unequip everything first
                player.equipment.UnequipAll(player.inventory);
                
                // Re-equip from inventory by finding and equipping items
                EquipItemById(player, data.equippedWeapon);
                EquipItemById(player, data.equippedArmor);
                EquipItemById(player, data.equippedAccessory);
            }
            
            Debug.Log($"[GameStateManager] Player restored at ({data.gridX},{data.gridY}) HP:{data.currentHealth}/{data.maxHealth}");
        }

        private static void ApplyQuests(QuestLog questLog, List<QuestSaveData> quests)
        {
            if (questLog == null) return;
            questLog.ApplySaveData(quests);
        }

        /// <summary>
        /// Helper to find and equip an item by ID from player inventory.
        /// </summary>
        private static void EquipItemById(PlayerController player, string itemId)
        {
            if (string.IsNullOrEmpty(itemId)) return;
            
            var item = player.inventory.GetItem(itemId);
            if (item != null)
            {
                player.equipment.Equip(item, player.inventory);
            }
        }

        
private static void RestoreEnemies(List<EnemySaveData> enemies)
        {
            var gridWorld = GridWorld.Instance;
            
            foreach (var data in enemies)
            {
                // Use EnemyFactory to spawn at position with level
                var enemy = EnemyFactory.Spawn(data.enemyId, data.gridX, data.gridY, gridWorld, data.level);
                
                if (enemy != null)
                {
                    // Override current health to match saved state
                    enemy.currentHealth = data.currentHealth;
                    
                    // Restore state if valid
                    if (System.Enum.TryParse<EnemyAI.AIState>(data.state, out var parsedState))
                    {
                        enemy.state = parsedState;
                    }
                }
                else
                {
                    Debug.LogWarning($"[GameStateManager] Failed to spawn enemy '{data.enemyId}' at ({data.gridX},{data.gridY})");
                }
            }
            
            Debug.Log($"[GameStateManager] Restored {enemies.Count} enemies");
        }
        
        private static void RestoreGroundItems(List<ItemPickupSaveData> items)
        {
            var gridWorld = GridWorld.Instance;
            float tileSize = gridWorld?.tileSize ?? 1f;
            
            foreach (var data in items)
            {
                ItemPickup.CreateFromLoot(data.itemId, data.gridX, data.gridY, data.quantity, tileSize);
            }
            
            Debug.Log($"[GameStateManager] Restored {items.Count} ground items");
        }
        
private static void RefreshAllUI()
        {
            // UIs use LateUpdate polling, so they'll auto-refresh next frame.
            // Nothing to do here - health, equipment, inventory UIs will update automatically.
        }

        private static void ApplyEconomicData(EconomicSaveData data)
        {
            TaxRegistry.Clear();
            TradeRelationRegistry.Clear();
            EconomicEventService.Clear();
            SupplyService.Clear();

            if (data == null) return;

            if (data.activeTaxPolicies != null)
            {
                for (int i = 0; i < data.activeTaxPolicies.Count; i++)
                {
                    var p = data.activeTaxPolicies[i];
                    if (p == null) continue;
                    TaxRegistry.AddPolicy(new TaxPolicy
                    {
                        id = p.id,
                        type = p.type,
                        rate = p.rate,
                        jurisdictionId = p.jurisdictionId,
                        exemptFactions = p.exemptFactions != null ? new List<string>(p.exemptFactions) : new List<string>(),
                        exemptItems = p.exemptItems != null ? new List<string>(p.exemptItems) : new List<string>(),
                        targetItems = p.targetItems != null ? new List<string>(p.targetItems) : new List<string>(),
                        turnsRemaining = p.turnsRemaining,
                        sourceLayerId = p.sourceLayerId
                    });
                }
            }

            if (data.tradeRelations != null)
            {
                for (int i = 0; i < data.tradeRelations.Count; i++)
                {
                    var r = data.tradeRelations[i];
                    if (r == null) continue;
                    TradeRelationRegistry.SetRelation(new FactionTradeRelation
                    {
                        sourceFactionId = r.sourceFactionId,
                        targetFactionId = r.targetFactionId,
                        status = r.status,
                        tariffRate = r.tariffRate,
                        bannedItems = r.bannedItems != null ? new List<string>(r.bannedItems) : new List<string>(),
                        exclusiveItems = r.exclusiveItems != null ? new List<string>(r.exclusiveItems) : new List<string>()
                    });
                }
            }

            var dcs = DistrictControlService.Instance;
            if (dcs != null && dcs.States != null && data.districtEconomics != null)
            {
                for (int i = 0; i < data.districtEconomics.Count; i++)
                {
                    var saved = data.districtEconomics[i];
                    if (saved == null || string.IsNullOrEmpty(saved.districtId)) continue;
                    DistrictState state = null;
                    for (int s = 0; s < dcs.States.Count; s++)
                    {
                        if (dcs.States[s].Id == saved.districtId)
                        {
                            state = dcs.States[s];
                            break;
                        }
                    }
                    if (state == null) continue;

                    state.treasury = saved.treasury;
                    state.corruption = saved.corruption;
                    state.economicActivity = saved.economicActivity;

                    if (state.itemSupply != null) state.itemSupply.Clear();
                    if (state.itemDemand != null) state.itemDemand.Clear();

                    if (saved.itemSupply != null)
                    {
                        for (int j = 0; j < saved.itemSupply.Count; j++)
                        {
                            var pair = saved.itemSupply[j];
                            if (pair == null || string.IsNullOrEmpty(pair.itemId)) continue;
                            SupplyService.SetSupply(saved.districtId, pair.itemId, pair.value);
                        }
                    }
                    if (saved.itemDemand != null && state.itemDemand != null)
                    {
                        for (int j = 0; j < saved.itemDemand.Count; j++)
                        {
                            var pair = saved.itemDemand[j];
                            if (pair == null || string.IsNullOrEmpty(pair.itemId)) continue;
                            state.itemDemand[pair.itemId] = pair.value;
                        }
                    }
                }
            }

            if (data.activeEvents != null)
            {
                for (int i = 0; i < data.activeEvents.Count; i++)
                {
                    var ev = data.activeEvents[i];
                    if (ev == null) continue;
                    EconomicEventService.TriggerEvent(new DemandEvent
                    {
                        id = ev.id,
                        itemId = ev.itemId,
                        demandMultiplier = ev.demandMultiplier,
                        durationDays = ev.durationDays,
                        districtId = ev.districtId,
                        description = ev.description
                    });
                }
            }

            // Restore faction reputation
            if (data.factionReputation != null)
            {
                for (int i = 0; i < data.factionReputation.Count; i++)
                {
                    var rep = data.factionReputation[i];
                    if (rep == null || string.IsNullOrEmpty(rep.factionId)) continue;
                    ReputationSystem.SetRep(rep.factionId, rep.reputation);
                }
            }
        }

        #endregion

        #region Convenience Methods
        
        /// <summary>
        /// Collect state and save in one call.
        /// </summary>
        public static bool QuickSave()
        {
            var state = CollectState();
            return SaveSystem.Save(state);
        }
        
        /// <summary>
        /// Load state and apply in one call.
        /// </summary>
        public static bool QuickLoad()
        {
            if (SaveSystem.TryLoad(out var state))
            {
                return ApplyState(state);
            }
            return false;
        }
        
        #endregion
    }
}
