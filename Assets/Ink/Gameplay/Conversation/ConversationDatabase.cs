using System.Collections.Generic;
using UnityEngine;

namespace InkSim
{
    /// <summary>
    /// Static registry that loads all ConversationTemplate assets from Resources/Conversations
    /// and provides filtered lookups by topic and faction relationship.
    /// </summary>
    public static class ConversationDatabase
    {
        private static bool _initialized;
        private static readonly List<ConversationTemplate> _all = new List<ConversationTemplate>();
        private static readonly Dictionary<ConversationTopicTag, List<ConversationTemplate>> _byTopic
            = new Dictionary<ConversationTopicTag, List<ConversationTemplate>>();

        // Reusable list for FindTemplate filtering (avoids allocation per lookup)
        private static readonly List<ConversationTemplate> _filteredCandidates = new List<ConversationTemplate>(16);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            _initialized = false;
            _all.Clear();
            _byTopic.Clear();
            _filteredCandidates.Clear();
        }

        public static void EnsureInitialized()
        {
            if (_initialized) return;
            _initialized = true;

            // Load from Resources (for designer-authored .asset files)
            var assets = Resources.LoadAll<ConversationTemplate>("Conversations");
            foreach (var t in assets)
                Register(t);

            // Seed from code (built-in authored content)
            var seeded = ConversationContentSeeder.Seed();
            if (seeded != null)
            {
                foreach (var t in seeded)
                    Register(t);
            }

            Debug.Log($"[ConversationDatabase] Loaded {_all.Count} conversation templates across {_byTopic.Count} topics.");
        }

        private static void Register(ConversationTemplate t)
        {
            if (t == null) return;
            _all.Add(t);
            if (!_byTopic.ContainsKey(t.topic))
                _byTopic[t.topic] = new List<ConversationTemplate>();
            _byTopic[t.topic].Add(t);
        }

        /// <summary>
        /// Get all templates for a given topic.
        /// </summary>
        public static List<ConversationTemplate> GetByTopic(ConversationTopicTag topic)
        {
            EnsureInitialized();
            if (_byTopic.TryGetValue(topic, out var list))
                return list;
            return null;
        }

        /// <summary>
        /// Find a valid conversation template for two entities given their topic and faction relationship.
        /// Returns null if no valid template is found.
        /// When districtState is provided, templates with predicates are validated against world state.
        /// </summary>
        public static ConversationTemplate FindTemplate(
            FactionMember initiator,
            FactionMember responder,
            ConversationTopicTag topic,
            DistrictState districtState = null)
        {
            EnsureInitialized();

            if (!_byTopic.TryGetValue(topic, out var candidates) || candidates.Count == 0)
                return null;

            // Use faction IDs directly instead of GetComponent<GridEntity>() for same-faction check
            bool sameFaction = initiator.faction != null && responder.faction != null
                && initiator.faction.id == responder.faction.id;

            int interRep = 0;
            if (!sameFaction && initiator.faction != null && responder.faction != null)
                interRep = ReputationSystem.GetInterRep(initiator.faction.id, responder.faction.id);

            // Reuse static list instead of allocating new List per call
            _filteredCandidates.Clear();
            for (int i = 0; i < candidates.Count; i++)
            {
                var t = candidates[i];

                // Same/cross faction filter
                if (t.sameFactionOnly && !sameFaction) continue;
                if (t.crossFactionOnly && sameFaction) continue;

                // Inter-rep range filter (cross-faction only)
                if (!sameFaction)
                {
                    if (interRep < t.minInterRep || interRep > t.maxInterRep)
                        continue;
                }

                // Rank difference filter (same-faction only)
                if (t.requireRankDifference && sameFaction)
                {
                    if (!HasRankDifference(initiator, responder))
                        continue;
                }

                // Faction gate filter (hostility dialogue is faction-specific)
                if (!string.IsNullOrEmpty(t.requiredInitiatorFactionId))
                {
                    if (initiator.faction == null || initiator.faction.id != t.requiredInitiatorFactionId)
                        continue;
                }

                // World-state predicate filter
                if (t.predicate != null)
                {
                    if (!t.predicate(initiator, responder, districtState))
                        continue;
                }

                // Per-template cooldown filter
                if (t.cooldownTurns > 0 && ConversationManager.Instance != null)
                {
                    int currentTurn = TurnManager.Instance != null ? TurnManager.Instance.TurnNumber : 0;
                    if (!ConversationManager.Instance.IsTemplateCooldownExpired(t.id, currentTurn, t.cooldownTurns))
                        continue;
                }

                _filteredCandidates.Add(t);
            }

            if (_filteredCandidates.Count == 0) return null;
            return _filteredCandidates[Random.Range(0, _filteredCandidates.Count)];
        }

        /// <summary>
        /// Check if the initiator outranks the responder.
        /// Rank hierarchy: high > mid > low.
        /// </summary>
        private static bool HasRankDifference(FactionMember a, FactionMember b)
        {
            int rankA = RankToInt(a.rankId);
            int rankB = RankToInt(b.rankId);
            return rankA > rankB;
        }

        private static int RankToInt(string rankId)
        {
            if (string.IsNullOrEmpty(rankId)) return 0;
            switch (rankId)
            {
                case "high": return 3;
                case "mid": return 2;
                case "low": return 1;
                default: return 0;
            }
        }
    }
}
