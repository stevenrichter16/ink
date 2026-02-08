using UnityEngine;

namespace InkSim
{
    public enum QuestState
    {
        Active,
        Completed,
        TurnedIn
    }

    /// <summary>
    /// Simple quest definition. A single counter-based objective with basic rewards.
    /// Extend as needed for more complex quest types.
    /// </summary>
    [CreateAssetMenu(menuName = "Ink/Quest Definition", fileName = "QuestDefinition")]
    public class QuestDefinition : ScriptableObject
    {
        [Header("Identity")]
        public string id = "quest_id";
        public string title = "Quest Title";
        [TextArea] public string description;

        [Header("Objective")]
        public string objectiveHint = "Defeat enemies or gather items.";
        public int requiredCount = 1;
        [Tooltip("If set, increment progress when this enemy ID is killed.")]
        public string killTargetEnemyId;
        [Tooltip("Auto-turn-in when objective is completed.")]
        public bool autoTurnInOnComplete = false;

        /// <summary>District this quest relates to (for territory impact on completion). Set at runtime by DynamicQuestService.</summary>
        [System.NonSerialized] public string districtId;

        [Header("Rewards")]
        public int rewardCoins = 0;
        public int rewardXp = 0;
        public string rewardItemId;
        public int rewardItemQuantity = 1;
    }

    /// <summary>
    /// Runtime registry for quest definitions, loaded from Resources/Quests.
    /// </summary>
    public static class QuestDatabase
    {
        private static bool _initialized;
        private static readonly System.Collections.Generic.Dictionary<string, QuestDefinition> _quests =
            new System.Collections.Generic.Dictionary<string, QuestDefinition>();

        public static void Initialize()
        {
            if (_initialized) return;
            var assets = Resources.LoadAll<QuestDefinition>("Quests");
            foreach (var def in assets)
            {
                Register(def);
            }
            _initialized = true;
        }

        public static void Register(QuestDefinition def)
        {
            if (def == null || string.IsNullOrEmpty(def.id)) return;
            _quests[def.id] = def;
        }

        public static QuestDefinition Get(string questId)
        {
            if (!_initialized) Initialize();
            return _quests.TryGetValue(questId, out var def) ? def : null;
        }
    }
}
