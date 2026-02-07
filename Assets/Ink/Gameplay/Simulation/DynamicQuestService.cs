using System.Collections.Generic;
using UnityEngine;

namespace InkSim
{
    /// <summary>
    /// Generates quests from world state each economic day.
    /// Triggers include: contested districts, supply scarcity, raids, demon presence.
    /// Max 3 active dynamic quests at a time.
    /// </summary>
    public static class DynamicQuestService
    {
        private const int MaxActiveQuests = 3;
        private const int QuestExpirationDays = 5;

        // Track active dynamic quests: questId → day created
        private static Dictionary<string, int> _activeQuests = new Dictionary<string, int>();

        // Unique counter for quest IDs
        private static int _questCounter = 0;

        public static void Execute(int dayNumber)
        {
            var dcs = DistrictControlService.Instance;
            if (dcs == null) return;

            // Expire old quests first
            ExpireOldQuests(dayNumber);

            // Don't generate if already at cap
            if (_activeQuests.Count >= MaxActiveQuests)
            {
                Debug.Log($"[DynamicQuest] Day {dayNumber}: At quest cap ({_activeQuests.Count}/{MaxActiveQuests}). Skipping generation.");
                return;
            }

            int generated = 0;

            // Try each generator in priority order (stop if we hit the cap)
            generated += GenerateRaidQuests(dcs, dayNumber);
            if (_activeQuests.Count >= MaxActiveQuests) goto done;

            generated += GenerateContestQuests(dcs, dayNumber);
            if (_activeQuests.Count >= MaxActiveQuests) goto done;

            generated += GenerateDemonPurgeQuests(dcs, dayNumber);
            if (_activeQuests.Count >= MaxActiveQuests) goto done;

            generated += GenerateScarcityQuests(dcs, dayNumber);

            done:
            if (generated > 0)
                Debug.Log($"[DynamicQuest] Day {dayNumber}: Generated {generated} new quest(s). Active: {_activeQuests.Count}/{MaxActiveQuests}");
        }

        /// <summary>
        /// Remove quests that have been active for too long.
        /// </summary>
        private static void ExpireOldQuests(int dayNumber)
        {
            var toRemove = new List<string>();

            foreach (var kvp in _activeQuests)
            {
                if (dayNumber - kvp.Value >= QuestExpirationDays)
                {
                    toRemove.Add(kvp.Key);
                }
            }

            foreach (var questId in toRemove)
            {
                _activeQuests.Remove(questId);
                Debug.Log($"[DynamicQuest] Quest {questId} expired (active for {QuestExpirationDays}+ days).");
            }
        }

        /// <summary>
        /// When a raid was spawned by DynamicSpawnService, create a quest to repel it.
        /// </summary>
        private static int GenerateRaidQuests(DistrictControlService dcs, int dayNumber)
        {
            if (string.IsNullOrEmpty(DynamicSpawnService.LastRaidDistrictId)) return 0;
            if (DynamicSpawnService.LastRaidDay != dayNumber) return 0; // Only on the day of the raid

            string districtId = DynamicSpawnService.LastRaidDistrictId;
            string factionId = DynamicSpawnService.LastRaidFactionId;
            string enemyId = DynamicSpawnService.GetEnemyIdForFaction(factionId);
            if (string.IsNullOrEmpty(enemyId)) return 0;

            // Find display name for the district
            string districtName = districtId;
            for (int d = 0; d < dcs.States.Count; d++)
            {
                if (dcs.States[d].Id == districtId && dcs.States[d].Definition != null)
                {
                    districtName = dcs.States[d].Definition.displayName;
                    break;
                }
            }

            // Find display name for the faction
            string factionName = factionId;
            int fIdx = FactionStrategyService.GetFactionIndex(dcs, factionId);
            if (fIdx >= 0) factionName = dcs.Factions[fIdx].displayName;

            string questId = $"dyn_raid_{_questCounter++}";
            var quest = CreateQuest(
                questId,
                $"Repel the {factionName} Raid",
                $"Raiders from the {factionName} are attacking {districtName}! Drive them back.",
                $"Defeat {enemyId}s in the area.",
                enemyId,
                3,
                75 + dayNumber * 5,
                50 + dayNumber * 3,
                "potion"
            );

            return TryRegisterQuest(quest, dayNumber) ? 1 : 0;
        }

        /// <summary>
        /// When districts are contested for 3+ days, create defense quests.
        /// </summary>
        private static int GenerateContestQuests(DistrictControlService dcs, int dayNumber)
        {
            int generated = 0;

            foreach (var kvp in FactionStrategyService.ContestedDistricts)
            {
                if (kvp.Value < 3) continue; // Not contested long enough
                if (_activeQuests.Count >= MaxActiveQuests) break;

                string districtId = kvp.Key;

                // Check if we already have a quest for this district
                bool alreadyHasQuest = false;
                foreach (var activeQuestId in _activeQuests.Keys)
                {
                    if (activeQuestId.Contains("contest") && activeQuestId.Contains(districtId.GetHashCode().ToString()))
                    {
                        alreadyHasQuest = true;
                        break;
                    }
                }
                if (alreadyHasQuest) continue;

                // Find district info
                string districtName = districtId;
                for (int d = 0; d < dcs.States.Count; d++)
                {
                    if (dcs.States[d].Id == districtId && dcs.States[d].Definition != null)
                    {
                        districtName = dcs.States[d].Definition.displayName;
                        break;
                    }
                }

                // Determine what enemy type to target (most common hostile faction)
                string targetEnemyId = "skeleton"; // default
                string attackerFaction = FactionStrategyService.LastSkirmishAttackerFactionId;
                if (!string.IsNullOrEmpty(attackerFaction))
                {
                    string mapped = DynamicSpawnService.GetEnemyIdForFaction(attackerFaction);
                    if (!string.IsNullOrEmpty(mapped)) targetEnemyId = mapped;
                }

                string questId = $"dyn_contest_{districtId.GetHashCode()}_{_questCounter++}";
                var quest = CreateQuest(
                    questId,
                    $"Defend {districtName}",
                    $"{districtName} is under siege! Help the defenders repel the invaders.",
                    $"Defeat {kvp.Value} invaders in {districtName}.",
                    targetEnemyId,
                    kvp.Value, // Kill count scales with contest duration
                    50 + kvp.Value * 15,
                    40 + kvp.Value * 10,
                    null
                );

                if (TryRegisterQuest(quest, dayNumber))
                    generated++;
            }

            return generated;
        }

        /// <summary>
        /// When demons have significant control anywhere, create demon purge quests.
        /// </summary>
        private static int GenerateDemonPurgeQuests(DistrictControlService dcs, int dayNumber)
        {
            int demonFactionIdx = FactionStrategyService.GetFactionIndex(dcs, "faction_demon");
            if (demonFactionIdx < 0) return 0;

            for (int d = 0; d < dcs.States.Count; d++)
            {
                if (_activeQuests.Count >= MaxActiveQuests) break;

                var state = dcs.States[d];
                if (state.control[demonFactionIdx] < 0.3f) continue;

                // Check if we already have a demon purge quest
                bool exists = false;
                foreach (var id in _activeQuests.Keys)
                {
                    if (id.StartsWith("dyn_demon_")) { exists = true; break; }
                }
                if (exists) break;

                string districtName = state.Definition != null ? state.Definition.displayName : state.Id;

                string questId = $"dyn_demon_{_questCounter++}";
                var quest = CreateQuest(
                    questId,
                    $"Demon Purge: {districtName}",
                    $"Demonic influence grows in {districtName}. Cleanse the area of their taint.",
                    "Slay demons in the district.",
                    "demon",
                    2,
                    100 + dayNumber * 5,
                    80 + dayNumber * 3,
                    "gem"
                );

                return TryRegisterQuest(quest, dayNumber) ? 1 : 0;
            }

            return 0;
        }

        /// <summary>
        /// When supply drops below 0.3 for any item in a district, create supply quests.
        /// </summary>
        private static int GenerateScarcityQuests(DistrictControlService dcs, int dayNumber)
        {
            int generated = 0;

            for (int d = 0; d < dcs.States.Count; d++)
            {
                if (_activeQuests.Count >= MaxActiveQuests) break;

                var state = dcs.States[d];
                if (state.itemSupply == null) continue;

                foreach (var kvp in state.itemSupply)
                {
                    if (_activeQuests.Count >= MaxActiveQuests) break;
                    if (kvp.Value >= 0.3f) continue; // Not scarce enough

                    string itemId = kvp.Key;
                    string districtName = state.Definition != null ? state.Definition.displayName : state.Id;

                    // Check if we already have a scarcity quest for this item
                    bool exists = false;
                    foreach (var id in _activeQuests.Keys)
                    {
                        if (id.Contains("scarcity") && id.Contains(itemId))
                        {
                            exists = true;
                            break;
                        }
                    }
                    if (exists) continue;

                    // Scarcity quests are kill quests that encourage hunting
                    // (the real "supply" comes from the economic simulation, but killing
                    // enemies reduces faction pressure which helps prosperity recover)
                    string questId = $"dyn_scarcity_{itemId}_{_questCounter++}";
                    var quest = CreateQuest(
                        questId,
                        $"Supply Run: {itemId}",
                        $"{districtName} is running low on {itemId}. Clear hostile forces to restore trade routes.",
                        "Defeat enemies disrupting supply lines.",
                        "goblin", // Goblins are the common trade-route raiders
                        2,
                        40 + dayNumber * 3,
                        30 + dayNumber * 2,
                        itemId
                    );

                    if (TryRegisterQuest(quest, dayNumber))
                        generated++;
                }
            }

            return generated;
        }

        /// <summary>
        /// Create a QuestDefinition at runtime.
        /// </summary>
        private static QuestDefinition CreateQuest(string id, string title, string desc, string hint,
            string enemyId, int count, int coins, int xp, string rewardItem)
        {
            var quest = ScriptableObject.CreateInstance<QuestDefinition>();
            quest.id = id;
            quest.title = title;
            quest.description = desc;
            quest.objectiveHint = hint;
            quest.killTargetEnemyId = enemyId;
            quest.requiredCount = count;
            quest.rewardCoins = coins;
            quest.rewardXp = xp;
            quest.rewardItemId = rewardItem;
            quest.rewardItemQuantity = 1;
            quest.autoTurnInOnComplete = true;
            return quest;
        }

        /// <summary>
        /// Register a quest with the database and add it to the player's quest log.
        /// Returns true if successful.
        /// </summary>
        private static bool TryRegisterQuest(QuestDefinition quest, int dayNumber)
        {
            if (quest == null) return false;

            // Register with the global database
            QuestDatabase.Register(quest);

            // Find the player's quest log
            var player = UnityEngine.Object.FindFirstObjectByType<PlayerController>();
            if (player == null || player.questLog == null)
            {
                Debug.LogWarning($"[DynamicQuest] Cannot find player quest log. Quest '{quest.title}' registered but not added to log.");
                _activeQuests[quest.id] = dayNumber;
                return true;
            }

            // Don't duplicate
            if (player.questLog.HasQuest(quest.id))
                return false;

            player.questLog.AddQuest(quest);
            _activeQuests[quest.id] = dayNumber;
            Debug.Log($"[DynamicQuest] NEW QUEST: \"{quest.title}\" — {quest.description}");

            // Player-visible quest notification
            SimulationEventLog.Banner($"\u2728 New Quest: {quest.title}", SimulationEventLog.ColorQuest);

            return true;
        }

        /// <summary>Clear state for testing / game restart.</summary>
        public static void Clear()
        {
            _activeQuests.Clear();
            _questCounter = 0;
        }
    }
}
