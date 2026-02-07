using System.Collections.Generic;
using UnityEngine;

namespace InkSim
{
    /// <summary>
    /// Runtime state for a conversation in progress between two entities.
    /// Owned by ConversationManager, not a MonoBehaviour.
    /// </summary>
    public class ActiveConversation
    {
        public int conversationId;
        public ConversationTemplate template;
        public GridEntity initiator;
        public GridEntity responder;
        public FactionMember initiatorFm;
        public FactionMember responderFm;

        // State
        public int currentLineIndex;
        public int turnsUntilNextLine;
        public bool isComplete;

        // The turn on which this conversation was created (first line already delivered).
        // TickConversations skips conversations created on the same turn to avoid double-delivery.
        public int createdOnTurn = -1;

        // Pre-resolved text for each line (tokens replaced at creation time)
        public string[] resolvedTexts;

        /// <summary>
        /// Advance one turn. Returns the current line if it's time to speak, or null if waiting.
        /// </summary>
        public ConversationLine Tick()
        {
            if (isComplete || template == null || template.lines == null)
                return null;

            if (currentLineIndex >= template.lines.Length)
            {
                isComplete = true;
                return null;
            }

            turnsUntilNextLine--;
            if (turnsUntilNextLine > 0)
                return null; // Still waiting

            // Deliver current line
            var line = template.lines[currentLineIndex];
            currentLineIndex++;

            // Set up delay for next line
            if (currentLineIndex < template.lines.Length)
                turnsUntilNextLine = Mathf.Max(1, template.lines[currentLineIndex].turnDelay);
            else
                isComplete = true; // That was the last line

            return line;
        }

        /// <summary>
        /// Get the entity who speaks the current line (before Tick advances).
        /// Uses the line at currentLineIndex - 1 since Tick already advanced.
        /// </summary>
        public GridEntity GetCurrentSpeaker()
        {
            int idx = currentLineIndex - 1;
            if (idx < 0 || idx >= template.lines.Length) return initiator;

            return template.lines[idx].speaker == ConversationLine.Speaker.Initiator
                ? initiator
                : responder;
        }

        /// <summary>
        /// Get the resolved text for the most recently delivered line.
        /// </summary>
        public string GetCurrentText()
        {
            int idx = currentLineIndex - 1;
            if (idx < 0 || idx >= resolvedTexts.Length) return "";
            return resolvedTexts[idx];
        }
    }

    /// <summary>
    /// Orchestrates NPC-to-NPC conversations. Handles initiation (who talks to whom),
    /// multi-turn ticking (delivering lines across game turns), topic selection
    /// (based on faction relationships and world state), and token resolution.
    ///
    /// Added to the WorldSimulationService GameObject by TestMapBuilder.
    /// Called by TurnManager after entity turns, and by NpcAI/EnemyAI when idle.
    /// </summary>
    public class ConversationManager : MonoBehaviour
    {
        public static ConversationManager Instance { get; private set; }

        // Tuning
        private const int ConversationRange = 4;
        private const float InitiationChance = 0.08f;
        private const int MinTurnsBetweenConversations = 8;
        private const int MaxSimultaneousConversations = 4;

        // Runtime state
        private readonly List<ActiveConversation> _active = new List<ActiveConversation>(8);
        private readonly Dictionary<GridEntity, int> _lastConversationTurn = new Dictionary<GridEntity, int>(32);
        private readonly HashSet<GridEntity> _inConversation = new HashSet<GridEntity>();
        private int _nextConversationId;

        // Reusable lists to avoid GC
        private readonly List<FactionMember> _partnerCandidates = new List<FactionMember>(16);
        private readonly List<WeightedTopic> _topicWeights = new List<WeightedTopic>(16);

        private struct WeightedTopic
        {
            public ConversationTopicTag topic;
            public int weight;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            Instance = null;
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }
            Instance = this;
        }

        // =====================================================================
        // PUBLIC API
        // =====================================================================

        /// <summary>
        /// Called by TurnManager after all entity turns are processed.
        /// Advances each active conversation by one turn.
        /// </summary>
        public void TickConversations(int currentTurn)
        {
            for (int i = _active.Count - 1; i >= 0; i--)
            {
                var conv = _active[i];

                // Skip conversations created this same turn (first line already delivered by TryInitiateConversation)
                if (conv.createdOnTurn == currentTurn)
                    continue;

                // Validate participants still alive
                if (!IsEntityValid(conv.initiator) || !IsEntityValid(conv.responder))
                {
                    EndConversation(conv, i);
                    continue;
                }

                // Interrupt if either participant entered combat
                if (IsInCombat(conv.initiatorFm) || IsInCombat(conv.responderFm))
                {
                    EndConversation(conv, i);
                    continue;
                }

                var line = conv.Tick();
                if (line != null)
                {
                    GridEntity speaker = conv.GetCurrentSpeaker();
                    GridEntity listener = (speaker == conv.initiator) ? conv.responder : conv.initiator;
                    string text = conv.GetCurrentText();
                    Color color = SpeechBubblePool.GetColorForRelationship(conv.initiatorFm, conv.responderFm);
                    DeliverLine(speaker, listener, text, color);
                }

                if (conv.isComplete)
                {
                    EndConversation(conv, i);
                }
            }

            // Clean up stale cooldown entries
            CleanupCooldowns();
        }

        /// <summary>
        /// Called by NpcAI/EnemyAI during TakeTurn() when idle.
        /// Attempts to start a conversation with a nearby entity.
        /// Returns true if the entity started a conversation this turn.
        /// </summary>
        public bool TryInitiateConversation(GridEntity entity)
        {
            if (entity == null) return false;
            if (_active.Count >= MaxSimultaneousConversations) return false;
            if (_inConversation.Contains(entity)) return false;
            if (!IsOffCooldown(entity)) return false;
            if (Random.value > InitiationChance) return false;

            // Merchants don't participate in ambient chatter (they have shop dialogue)
            if (entity.GetComponent<Merchant>() != null) return false;

            var initiatorFm = entity.GetComponent<FactionMember>();
            if (initiatorFm == null || initiatorFm.faction == null) return false;
            if (initiatorFm.state == FactionMember.AlertState.Hostile) return false;

            // Find conversation partner
            FactionMember partnerFm = FindPartner(entity, initiatorFm);
            if (partnerFm == null) return false;

            GridEntity partner = partnerFm.GetComponent<GridEntity>();
            if (partner == null || _inConversation.Contains(partner)) return false;

            // Select topic based on relationship + world state
            ConversationTopicTag topic = SelectTopic(entity, initiatorFm, partner, partnerFm);

            // Find matching template
            ConversationTemplate template = ConversationDatabase.FindTemplate(initiatorFm, partnerFm, topic);
            if (template == null) return false;

            // Create and start conversation
            var conv = CreateConversation(template, entity, partner, initiatorFm, partnerFm);
            int currentTurn = TurnManager.Instance != null ? TurnManager.Instance.TurnNumber : 0;
            conv.createdOnTurn = currentTurn;
            _active.Add(conv);
            _lastConversationTurn[entity] = currentTurn;
            _lastConversationTurn[partner] = currentTurn;
            _inConversation.Add(entity);
            _inConversation.Add(partner);

            // Log to event feed
            bool sameFaction = initiatorFm.faction.id == partnerFm.faction.id;
            string topicLabel = template.topic.ToString();
            SimulationEventLog.LogSilent(
                $"{entity.name} ({initiatorFm.faction.displayName}) started {topicLabel} conversation with {partner.name}" +
                (sameFaction ? "" : $" ({partnerFm.faction.displayName})"));

            // Deliver first line immediately
            var firstLine = conv.Tick();
            if (firstLine != null)
            {
                GridEntity speaker = conv.GetCurrentSpeaker();
                GridEntity listener = (speaker == conv.initiator) ? conv.responder : conv.initiator;
                string text = conv.GetCurrentText();
                Color color = SpeechBubblePool.GetColorForRelationship(initiatorFm, partnerFm);
                DeliverLine(speaker, listener, text, color);
            }

            return true;
        }

        /// <summary>
        /// Remove an entity from any active conversation (e.g., when entering combat).
        /// Called by FactionMember when state changes to Hostile or when an entity acquires a target.
        /// </summary>
        public void InterruptConversation(GridEntity entity)
        {
            if (entity == null) return;
            if (!_inConversation.Contains(entity)) return;

            for (int i = _active.Count - 1; i >= 0; i--)
            {
                var conv = _active[i];
                if (conv.initiator == entity || conv.responder == entity)
                {
                    EndConversation(conv, i);
                }
            }
        }

        /// <summary>
        /// Clear all active conversations (for game restart).
        /// </summary>
        public void ClearAll()
        {
            _active.Clear();
            _inConversation.Clear();
            _lastConversationTurn.Clear();
        }

        // =====================================================================
        // PARTNER FINDING
        // =====================================================================

        private FactionMember FindPartner(GridEntity entity, FactionMember entityFm)
        {
            _partnerCandidates.Clear();

            var members = FactionMember.ActiveMembers;
            if (members == null) return null;

            for (int i = 0; i < members.Count; i++)
            {
                var fm = members[i];
                if (fm == null || fm == entityFm) continue;
                if (!fm.isActiveAndEnabled) continue;
                if (fm.faction == null) continue;

                var ge = fm.GetComponent<GridEntity>();
                if (ge == null || !ge.gameObject.activeInHierarchy) continue;
                if (_inConversation.Contains(ge)) continue;
                if (ge.GetComponent<Merchant>() != null) continue; // Skip merchants

                // Range check
                int dist = GridWorld.Distance(entity.gridX, entity.gridY, ge.gridX, ge.gridY);
                if (dist > ConversationRange) continue;

                // Don't talk to hostile entities (they'd fight, not chat)
                // Exception: hostile cross-faction entities CAN exchange threats
                // We allow conversations between non-hostile entities AND hostile entities at distance
                // (threats/taunts happen from a safe distance)
                _partnerCandidates.Add(fm);
            }

            if (_partnerCandidates.Count == 0) return null;

            // Prefer same-faction partners (70% chance if available)
            FactionMember sameFactionPick = null;
            FactionMember crossFactionPick = null;

            for (int i = 0; i < _partnerCandidates.Count; i++)
            {
                if (_partnerCandidates[i].faction.id == entityFm.faction.id)
                {
                    if (sameFactionPick == null || Random.value < 0.5f)
                        sameFactionPick = _partnerCandidates[i];
                }
                else
                {
                    if (crossFactionPick == null || Random.value < 0.5f)
                        crossFactionPick = _partnerCandidates[i];
                }
            }

            if (sameFactionPick != null && crossFactionPick != null)
                return Random.value < 0.7f ? sameFactionPick : crossFactionPick;

            return sameFactionPick ?? crossFactionPick;
        }

        // =====================================================================
        // TOPIC SELECTION
        // =====================================================================

        private ConversationTopicTag SelectTopic(
            GridEntity entityA, FactionMember fmA,
            GridEntity entityB, FactionMember fmB)
        {
            _topicWeights.Clear();

            bool sameFaction = fmA.faction.id == fmB.faction.id;

            if (sameFaction)
            {
                // Same-faction base topics
                AddTopic(ConversationTopicTag.Greeting, 30);
                AddTopic(ConversationTopicTag.Rumor, 25);
                AddTopic(ConversationTopicTag.StatusReport, 20);

                // Rank difference — orders
                if (HasRankDifference(fmA, fmB))
                    AddTopic(ConversationTopicTag.OrdersFromAbove, 25);
            }
            else
            {
                // Cross-faction — reputation determines baseline.
                // Note: entities at interRep <= -25 are actively hostile (fight on sight)
                // so they rarely reach conversation. We handle negative-neutral (-24 to -1)
                // with a mix of threats and wary encounters.
                int interRep = ReputationSystem.GetInterRep(fmA.faction.id, fmB.faction.id);

                if (interRep >= HostilityService.FriendlyThreshold)
                {
                    // Friendly: alliance talk, trade
                    AddTopic(ConversationTopicTag.AllianceAffirm, 30);
                    AddTopic(ConversationTopicTag.TradeNegotiation, 20);
                }
                else if (interRep < 0)
                {
                    // Negative-neutral: mix of threats, taunts, and wary encounters
                    // The more negative, the more hostile the conversation tone
                    int hostileWeight = Mathf.Clamp(-interRep * 2, 10, 50);
                    int waryWeight = Mathf.Max(10, 40 - hostileWeight / 2);
                    AddTopic(ConversationTopicTag.Threat, hostileWeight);
                    AddTopic(ConversationTopicTag.Taunt, hostileWeight / 2);
                    AddTopic(ConversationTopicTag.WaryEncounter, waryWeight);
                }
                else
                {
                    // Neutral (0 to 24): cautious, maybe trade
                    AddTopic(ConversationTopicTag.WaryEncounter, 30);
                    AddTopic(ConversationTopicTag.TradeNegotiation, 15);
                }

                // Trade embargo boost
                var relation = TradeRelationRegistry.GetRelation(fmA.faction.id, fmB.faction.id);
                if (relation != null && relation.status == TradeStatus.Embargo)
                    AddTopic(ConversationTopicTag.TradeEmbargo, 35);
            }

            // World context overlays (both same and cross-faction)
            var dcs = DistrictControlService.Instance;
            if (dcs != null)
            {
                var districtState = dcs.GetStateByPosition(entityA.gridX, entityA.gridY);
                if (districtState != null)
                {
                    // Contested territory
                    if (FactionStrategyService.ContestedDistricts.ContainsKey(districtState.Id))
                        AddTopic(ConversationTopicTag.TerritoryContest, sameFaction ? 30 : 30);

                    // Low prosperity
                    if (districtState.prosperity < 0.5f)
                        AddTopic(ConversationTopicTag.ProsperityLament, sameFaction ? 20 : 15);

                    // High heat (raid warning) — same faction only
                    if (sameFaction)
                    {
                        int fIdx = FactionStrategyService.GetFactionIndex(dcs, fmA.faction.id);
                        if (fIdx >= 0 && districtState.heat[fIdx] > 0.7f)
                            AddTopic(ConversationTopicTag.RaidWarning, 25);
                    }
                }
            }

            // Quest hint (same faction, rare)
            if (sameFaction)
                AddTopic(ConversationTopicTag.QuestHint, 5);

            // Weighted random selection
            return PickWeightedTopic();
        }

        private void AddTopic(ConversationTopicTag topic, int weight)
        {
            _topicWeights.Add(new WeightedTopic { topic = topic, weight = weight });
        }

        private ConversationTopicTag PickWeightedTopic()
        {
            if (_topicWeights.Count == 0) return ConversationTopicTag.Greeting;

            int totalWeight = 0;
            for (int i = 0; i < _topicWeights.Count; i++)
                totalWeight += _topicWeights[i].weight;

            int roll = Random.Range(0, totalWeight);
            int running = 0;

            for (int i = 0; i < _topicWeights.Count; i++)
            {
                running += _topicWeights[i].weight;
                if (roll < running)
                    return _topicWeights[i].topic;
            }

            return _topicWeights[_topicWeights.Count - 1].topic;
        }

        // =====================================================================
        // CONVERSATION CREATION & TOKEN RESOLUTION
        // =====================================================================

        private ActiveConversation CreateConversation(
            ConversationTemplate template,
            GridEntity initiator, GridEntity responder,
            FactionMember initiatorFm, FactionMember responderFm)
        {
            var conv = new ActiveConversation
            {
                conversationId = _nextConversationId++,
                template = template,
                initiator = initiator,
                responder = responder,
                initiatorFm = initiatorFm,
                responderFm = responderFm,
                currentLineIndex = 0,
                turnsUntilNextLine = 0, // First line plays immediately
                isComplete = false,
            };

            // Pre-resolve all text tokens
            conv.resolvedTexts = new string[template.lines.Length];
            for (int i = 0; i < template.lines.Length; i++)
            {
                GridEntity speaker = template.lines[i].speaker == ConversationLine.Speaker.Initiator
                    ? initiator : responder;
                GridEntity listener = speaker == initiator ? responder : initiator;
                FactionMember speakerFm = speaker == initiator ? initiatorFm : responderFm;
                FactionMember listenerFm = speaker == initiator ? responderFm : initiatorFm;

                conv.resolvedTexts[i] = ResolveTokens(template.lines[i].text, speakerFm, listenerFm, speaker);
            }

            return conv;
        }

        private string ResolveTokens(string text, FactionMember speakerFm, FactionMember listenerFm, GridEntity speaker)
        {
            if (string.IsNullOrEmpty(text)) return text;

            // Faction names
            string selfName = speakerFm?.faction?.displayName ?? "Unknown";
            string otherName = listenerFm?.faction?.displayName ?? "Unknown";
            text = text.Replace("{FACTION_SELF}", selfName);
            text = text.Replace("{FACTION_OTHER}", otherName);

            // District info
            var dcs = DistrictControlService.Instance;
            if (dcs != null)
            {
                var districtState = dcs.GetStateByPosition(speaker.gridX, speaker.gridY);
                if (districtState != null && districtState.Definition != null)
                {
                    text = text.Replace("{DISTRICT}", districtState.Definition.displayName);

                    // Prosperity descriptor
                    string prosDesc;
                    if (districtState.prosperity >= 0.8f) prosDesc = "thriving";
                    else if (districtState.prosperity >= 0.5f) prosDesc = "stable";
                    else if (districtState.prosperity >= 0.3f) prosDesc = "struggling";
                    else prosDesc = "desperate";
                    text = text.Replace("{PROSPERITY}", prosDesc);

                    // Control descriptor
                    int fIdx = speakerFm?.faction != null
                        ? FactionStrategyService.GetFactionIndex(dcs, speakerFm.faction.id)
                        : -1;
                    string controlDesc;
                    if (fIdx >= 0)
                    {
                        float control = districtState.control[fIdx];
                        if (control >= 0.7f) controlDesc = "firmly held";
                        else if (control >= 0.4f) controlDesc = "contested";
                        else if (control >= 0.2f) controlDesc = "slipping";
                        else controlDesc = "lost";
                    }
                    else
                    {
                        controlDesc = "unclaimed";
                    }
                    text = text.Replace("{CONTROL}", controlDesc);
                }
                else
                {
                    text = text.Replace("{DISTRICT}", "this place");
                    text = text.Replace("{PROSPERITY}", "uncertain");
                    text = text.Replace("{CONTROL}", "unknown");
                }
            }

            // Trade status
            if (speakerFm?.faction != null && listenerFm?.faction != null)
            {
                var relation = TradeRelationRegistry.GetRelation(speakerFm.faction.id, listenerFm.faction.id);
                string tradeDesc = "open";
                if (relation != null)
                {
                    switch (relation.status)
                    {
                        case TradeStatus.Restricted: tradeDesc = "restricted"; break;
                        case TradeStatus.Embargo: tradeDesc = "embargoed"; break;
                        case TradeStatus.Exclusive: tradeDesc = "exclusive"; break;
                        case TradeStatus.Alliance: tradeDesc = "allied"; break;
                        default: tradeDesc = "open"; break;
                    }
                }
                text = text.Replace("{TRADE_STATUS}", tradeDesc);
            }
            else
            {
                text = text.Replace("{TRADE_STATUS}", "open");
            }

            return text;
        }

        // =====================================================================
        // HELPERS
        // =====================================================================

        /// <summary>
        /// Delivers a dialogue line: shows speech bubble AND logs to ConversationLogPanel.
        /// </summary>
        private void DeliverLine(GridEntity speaker, GridEntity listener, string text, Color color)
        {
            SpeechBubblePool.Show(speaker, text, color);

            // Build speaker display name
            string speakerName = speaker.name;
            var speakerFm = speaker.GetComponent<FactionMember>();
            if (speakerFm != null && speakerFm.faction != null)
                speakerName = $"{speakerFm.faction.displayName} {speakerFm.rankId}";

            // Build listener display name
            string listenerName = "";
            if (listener != null)
            {
                listenerName = listener.name;
                var listenerFm = listener.GetComponent<FactionMember>();
                if (listenerFm != null && listenerFm.faction != null)
                    listenerName = $"{listenerFm.faction.displayName} {listenerFm.rankId}";
            }

            ConversationLogPanel.PushLine(speaker, speakerName, listener, listenerName, text, color);
        }

        private void EndConversation(ActiveConversation conv, int index)
        {
            _inConversation.Remove(conv.initiator);
            _inConversation.Remove(conv.responder);
            _active.RemoveAt(index);
        }

        private bool IsEntityValid(GridEntity entity)
        {
            return entity != null && entity.gameObject.activeInHierarchy;
        }

        private bool IsInCombat(FactionMember fm)
        {
            if (fm == null) return false;
            if (fm.state == FactionMember.AlertState.Hostile) return true;

            // Check if NPC has a hostile target
            var npc = fm.GetComponent<NpcAI>();
            if (npc != null && npc.hostileTarget != null) return true;

            // Check if enemy is in Chase or Attack state
            var enemy = fm.GetComponent<EnemyAI>();
            if (enemy != null && enemy.state != EnemyAI.AIState.Idle) return true;

            return false;
        }

        private bool IsOffCooldown(GridEntity entity)
        {
            if (!_lastConversationTurn.TryGetValue(entity, out int lastTurn))
                return true;

            int currentTurn = TurnManager.Instance != null ? TurnManager.Instance.TurnNumber : 0;
            return currentTurn - lastTurn >= MinTurnsBetweenConversations;
        }

        private void CleanupCooldowns()
        {
            // Remove entries for dead entities (periodically, not every tick)
            if (Random.value > 0.1f) return; // 10% chance per tick

            var toRemove = new List<GridEntity>();
            foreach (var kvp in _lastConversationTurn)
            {
                if (kvp.Key == null || !kvp.Key.gameObject.activeInHierarchy)
                    toRemove.Add(kvp.Key);
            }
            foreach (var key in toRemove)
                _lastConversationTurn.Remove(key);
        }

        private static bool HasRankDifference(FactionMember a, FactionMember b)
        {
            return RankToInt(a.rankId) > RankToInt(b.rankId);
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

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }
    }
}
