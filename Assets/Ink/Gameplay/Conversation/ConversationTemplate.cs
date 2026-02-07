using System.Collections.Generic;
using UnityEngine;

namespace InkSim
{
    /// <summary>
    /// Tags that identify what a conversation is about. Used to match
    /// templates to world-state conditions and faction relationships.
    /// </summary>
    public enum ConversationTopicTag
    {
        // Same-faction topics
        Greeting,           // Generic hello between allies
        Rumor,              // World gossip, overheard news
        StatusReport,       // Patrol/guard status update
        OrdersFromAbove,    // High-rank to low-rank orders

        // Cross-faction economic
        TradeNegotiation,   // Trade proposals between factions
        TradeEmbargo,       // Embargo complaint/announcement

        // Territory (both same and cross-faction)
        TerritoryContest,   // Contested district discussion

        // Hostile cross-faction
        Threat,             // Direct warning/threat
        Taunt,              // Hostile mockery

        // Neutral cross-faction
        WaryEncounter,      // Cautious neutral interaction

        // Friendly cross-faction
        AllianceAffirm,     // Bond between allied factions

        // World context
        ProsperityLament,   // Low prosperity complaint
        RaidWarning,        // Incoming enemy discussion
        QuestHint           // Hint about active quest objectives
    }

    /// <summary>
    /// A single line in a two-party conversation.
    /// </summary>
    [System.Serializable]
    public class ConversationLine
    {
        public enum Speaker
        {
            Initiator,  // The entity that started the conversation
            Responder   // The entity being spoken to
        }

        public Speaker speaker;
        [TextArea] public string text;

        /// <summary>
        /// Turn delay before this line plays.
        /// 0 = same turn as the previous line (immediate follow-up).
        /// 1 = next game turn (natural back-and-forth cadence).
        /// </summary>
        public int turnDelay;
    }

    /// <summary>
    /// Authored two-party conversation template. Loaded from Resources/Conversations.
    /// Each template defines when it can play (faction relationship, topic) and the
    /// sequence of lines exchanged between initiator and responder.
    /// </summary>
    [CreateAssetMenu(menuName = "Ink/Conversation Template", fileName = "ConversationTemplate")]
    public class ConversationTemplate : ScriptableObject
    {
        public string id;
        public ConversationTopicTag topic;

        [Header("Applicability")]
        [Tooltip("If true, both speakers must be in the same faction.")]
        public bool sameFactionOnly;
        [Tooltip("If true, speakers must be in different factions.")]
        public bool crossFactionOnly;

        [Header("Reputation Requirements (cross-faction only)")]
        [Tooltip("Minimum inter-faction reputation for this template to be valid.")]
        public int minInterRep = -100;
        [Tooltip("Maximum inter-faction reputation for this template to be valid.")]
        public int maxInterRep = 100;

        [Header("Rank Requirements (same-faction only)")]
        [Tooltip("If true, initiator must outrank responder (e.g., high > mid > low).")]
        public bool requireRankDifference;

        [Header("Lines")]
        public ConversationLine[] lines;
    }
}
