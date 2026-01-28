using System.Collections.Generic;
using UnityEngine;

namespace InkSim
{
    /// <summary>
    /// Defines a faction, its ranks, and defaults for stats/loadouts/dialogue.
    /// </summary>
    [CreateAssetMenu(fileName = "FactionDefinition", menuName = "Ink/Faction Definition")]
    public class FactionDefinition : ScriptableObject
    {
        public enum FactionDisposition
        {
            Calm,
            Aggressive
        }

        [Header("Identity")]
        public string id = "faction_id";
        public string displayName = "Faction";
        public int defaultReputation = 0;
        public LevelProfile defaultLevelProfile;
        public FactionDisposition disposition = FactionDisposition.Aggressive;
        [Header("Aggro/Alert Settings")]
        public int rallyRadius = 5;
        public int alertDurationTurns = 2;
        public int repOnHit = -5;
        public int repOnKill = -100;

        [Header("Ranks")]
        public List<RankDefinition> ranks = new List<RankDefinition>();

        [System.Serializable]
        public class RankDefinition
        {
            public string rankId = "low";
            public string displayName = "Acolyte";
            public int level = 1;
            public int baseDefense = 0;
            public LevelProfile levelProfileOverride;
            public List<int> spriteIndices = new List<int>();

            [Header("Default Equipment (Item IDs)")]
            public string weaponId;
            public string armorId;
            public string accessoryId;

            [Header("Default Spells")]
            public List<SpellData> defaultSpells = new List<SpellData>();

            [Header("Dialogue (optional)")]
            public DialogueSequence neutralDialogue;
            public DialogueSequence friendlyDialogue;
            public DialogueSequence hostileDialogue;
        }

        public RankDefinition GetRank(string rankId)
        {
            if (string.IsNullOrEmpty(rankId)) return null;
            return ranks.Find(r => r.rankId == rankId);
        }
    }
}
