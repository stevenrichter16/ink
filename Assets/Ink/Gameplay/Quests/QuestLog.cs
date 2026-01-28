using System.Collections.Generic;
using UnityEngine;

namespace InkSim
{
    /// <summary>
    /// Tracks active/completed quests and grants rewards.
    /// Attach to the Player.
    /// </summary>
    public class QuestLog : MonoBehaviour
    {
        [System.Serializable]
        public class QuestEntry
        {
            public QuestDefinition definition;
            public string questId;
            public QuestState state;
            public int currentCount;
        }

        private readonly List<QuestEntry> _entries = new List<QuestEntry>();
        public IReadOnlyList<QuestEntry> Entries => _entries;

        public event System.Action<QuestEntry> OnQuestAdded;
        public event System.Action<QuestEntry> OnQuestUpdated;

        private PlayerController _player;

        private void Awake()
        {
            _player = GetComponent<PlayerController>();
            QuestDatabase.Initialize();
        }

        public bool HasQuest(string questId) => _entries.Exists(q => q.questId == questId);

        public QuestEntry AddQuest(QuestDefinition def)
        {
            if (def == null) return null;
            if (HasQuest(def.id)) return GetEntry(def.id);

            var entry = new QuestEntry
            {
                definition = def,
                questId = def.id,
                state = QuestState.Active,
                currentCount = 0
            };

            _entries.Add(entry);
            OnQuestAdded?.Invoke(entry);
            return entry;
        }

        public void IncrementProgress(string questId, int amount = 1)
        {
            var entry = GetEntry(questId);
            if (entry == null || entry.state != QuestState.Active) return;

            entry.currentCount += Mathf.Max(1, amount);

            int required = entry.definition != null ? Mathf.Max(1, entry.definition.requiredCount) : 1;
            if (entry.currentCount >= required)
                entry.state = QuestState.Completed;

            OnQuestUpdated?.Invoke(entry);
        }

        /// <summary>
        /// Called by gameplay events when an enemy is killed.
        /// </summary>
        public void OnEnemyKilled(string enemyId)
        {
            if (string.IsNullOrEmpty(enemyId)) return;

            foreach (var entry in _entries)
            {
                if (entry.state != QuestState.Active || entry.definition == null) continue;

                if (!string.IsNullOrEmpty(entry.definition.killTargetEnemyId) &&
                    entry.definition.killTargetEnemyId == enemyId)
                {
                    IncrementProgress(entry.questId, 1);

                    // Auto turn-in if flagged and now completed
                    if (entry.definition.autoTurnInOnComplete && entry.state == QuestState.Completed)
                        TurnIn(entry.questId);
                }
            }
        }

        public void MarkComplete(string questId)
        {
            var entry = GetEntry(questId);
            if (entry == null) return;
            entry.state = QuestState.Completed;
            OnQuestUpdated?.Invoke(entry);
        }

        public void TurnIn(string questId)
        {
            var entry = GetEntry(questId);
            if (entry == null || entry.state != QuestState.Completed) return;

            entry.state = QuestState.TurnedIn;
            GrantRewards(entry.definition);
            OnQuestUpdated?.Invoke(entry);
        }

        private void GrantRewards(QuestDefinition def)
        {
            if (def == null) return;
            ItemDatabase.Initialize();

            if (_player?.inventory != null && def.rewardCoins > 0)
                _player.inventory.AddItem("coin", def.rewardCoins);

            if (_player?.levelable != null && def.rewardXp > 0)
                _player.levelable.AddXp(def.rewardXp);

            if (_player?.inventory != null && !string.IsNullOrEmpty(def.rewardItemId))
                _player.inventory.AddItem(def.rewardItemId, Mathf.Max(1, def.rewardItemQuantity));
        }

        private QuestEntry GetEntry(string questId) => _entries.Find(q => q.questId == questId);

        public QuestState? GetQuestState(string questId)
        {
            var entry = GetEntry(questId);
            return entry != null ? entry.state : (QuestState?)null;
        }

        // ---- Save/Load helpers ----

        public List<QuestSaveData> ToSaveData()
        {
            var list = new List<QuestSaveData>(_entries.Count);
            foreach (var entry in _entries)
            {
                list.Add(new QuestSaveData(entry.questId, entry.state, entry.currentCount));
            }
            return list;
        }

        public void ApplySaveData(List<QuestSaveData> saves)
        {
            _entries.Clear();
            if (saves == null) return;

            QuestDatabase.Initialize();
            foreach (var save in saves)
            {
                var def = QuestDatabase.Get(save.questId);
                var entry = new QuestEntry
                {
                    definition = def,
                    questId = save.questId,
                    state = save.state,
                    currentCount = save.currentCount
                };
                _entries.Add(entry);
            }
        }
    }
}
