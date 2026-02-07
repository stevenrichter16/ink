using System.Collections.Generic;
using UnityEngine;

namespace InkSim
{
    /// <summary>
    /// Seeds the ConversationDatabase with authored conversation templates at runtime.
    /// This avoids needing .asset files (which require script GUIDs) and keeps all
    /// dialogue content in one easily-editable file.
    ///
    /// Call ConversationContentSeeder.Seed() before first use — typically from
    /// ConversationDatabase.EnsureInitialized().
    /// </summary>
    public static class ConversationContentSeeder
    {
        private static bool _seeded;

        public static List<ConversationTemplate> Seed()
        {
            if (_seeded) return null;
            _seeded = true;

            var templates = new List<ConversationTemplate>();

            // ================================================================
            // SAME-FACTION: GREETINGS
            // ================================================================

            templates.Add(Create("greet_quiet_patrol", ConversationTopicTag.Greeting,
                sameFaction: true,
                lines: new[]
                {
                    Line(S.Initiator, "Another quiet patrol.", 0),
                    Line(S.Responder, "Don't jinx it. Last time, goblins were at the gate.", 1),
                }));

            templates.Add(Create("greet_morning", ConversationTopicTag.Greeting,
                sameFaction: true,
                lines: new[]
                {
                    Line(S.Initiator, "Morning.", 0),
                    Line(S.Responder, "Morning. Supplies came in from {DISTRICT}.", 1),
                }));

            templates.Add(Create("greet_any_news", ConversationTopicTag.Greeting,
                sameFaction: true,
                lines: new[]
                {
                    Line(S.Initiator, "Any news?", 0),
                    Line(S.Responder, "Nothing. Just ink and silence.", 1),
                }));

            templates.Add(Create("greet_tired", ConversationTopicTag.Greeting,
                sameFaction: true,
                lines: new[]
                {
                    Line(S.Initiator, "You look tired.", 0),
                    Line(S.Responder, "Third watch in a row. Captain's orders.", 1),
                }));

            // ================================================================
            // SAME-FACTION: STATUS REPORTS
            // ================================================================

            templates.Add(Create("status_sweep", ConversationTopicTag.StatusReport,
                sameFaction: true,
                lines: new[]
                {
                    Line(S.Initiator, "Captain wants the eastern wall swept.", 0),
                    Line(S.Responder, "Understood. I'll take the lower tier.", 1),
                }));

            templates.Add(Create("status_heat", ConversationTopicTag.StatusReport,
                sameFaction: true,
                lines: new[]
                {
                    Line(S.Initiator, "Heat's been rising in {DISTRICT}.", 0),
                    Line(S.Responder, "More inscriptions. The scribes are restless.", 1),
                }));

            templates.Add(Create("status_control", ConversationTopicTag.StatusReport,
                sameFaction: true,
                lines: new[]
                {
                    Line(S.Initiator, "{DISTRICT} is {CONTROL}.", 0),
                    Line(S.Responder, "We hold the line. That's what we do.", 1),
                }));

            templates.Add(Create("status_reinforcements", ConversationTopicTag.StatusReport,
                sameFaction: true,
                lines: new[]
                {
                    Line(S.Initiator, "Reinforcements arrived this morning.", 0),
                    Line(S.Responder, "About time. We were spread thin.", 1),
                }));

            // ================================================================
            // SAME-FACTION: RUMORS
            // ================================================================

            templates.Add(Create("rumor_lost_control", ConversationTopicTag.Rumor,
                sameFaction: true,
                lines: new[]
                {
                    Line(S.Initiator, "Heard the {FACTION_OTHER} lost control of {DISTRICT}.", 0),
                    Line(S.Responder, "Good. Maybe prices will stabilize.", 1),
                }));

            templates.Add(Create("rumor_embargo_ink", ConversationTopicTag.Rumor,
                sameFaction: true,
                lines: new[]
                {
                    Line(S.Initiator, "There's talk of an embargo on ink.", 0),
                    Line(S.Responder, "That would hurt everyone, not just the scribes.", 1),
                }));

            templates.Add(Create("rumor_truce_marker", ConversationTopicTag.Rumor,
                sameFaction: true,
                lines: new[]
                {
                    Line(S.Initiator, "Someone inscribed a truce marker near the border.", 0),
                    Line(S.Responder, "Desperate times call for desperate measures.", 1),
                }));

            templates.Add(Create("rumor_demons_massing", ConversationTopicTag.Rumor,
                sameFaction: true,
                lines: new[]
                {
                    Line(S.Initiator, "The demons are massing in the Boneyard.", 0),
                    Line(S.Responder, "Let them mass. We'll be ready.", 1),
                }));

            // ================================================================
            // SAME-FACTION: ORDERS (require rank difference)
            // ================================================================

            templates.Add(Create("order_patrol_north", ConversationTopicTag.OrdersFromAbove,
                sameFaction: true, requireRankDiff: true,
                lines: new[]
                {
                    Line(S.Initiator, "Patrol the northern road. Report anything unusual.", 0),
                    Line(S.Responder, "Yes, sir. Heading out now.", 1),
                }));

            templates.Add(Create("order_hold_position", ConversationTopicTag.OrdersFromAbove,
                sameFaction: true, requireRankDiff: true,
                lines: new[]
                {
                    Line(S.Initiator, "Hold this position until relieved.", 0),
                    Line(S.Responder, "Understood. No one passes.", 1),
                }));

            templates.Add(Create("order_double_watch", ConversationTopicTag.OrdersFromAbove,
                sameFaction: true, requireRankDiff: true,
                lines: new[]
                {
                    Line(S.Initiator, "Our supply lines are {PROSPERITY}. Double the watch.", 0),
                    Line(S.Responder, "On it, Captain.", 1),
                }));

            // ================================================================
            // CROSS-FACTION: HOSTILE / TENSE (interRep < 0)
            // Note: entities at interRep <= -25 actively fight, so these
            // primarily fire in the -24 to -1 "tense" zone where factions
            // dislike each other but aren't at open war.
            // ================================================================

            templates.Add(Create("hostile_not_welcome", ConversationTopicTag.Threat,
                crossFaction: true, minRep: -100, maxRep: -1,
                lines: new[]
                {
                    Line(S.Initiator, "Your kind isn't welcome here.", 0),
                    Line(S.Responder, "We go where we please.", 1),
                }));

            templates.Add(Create("hostile_leave", ConversationTopicTag.Threat,
                crossFaction: true, minRep: -100, maxRep: -1,
                lines: new[]
                {
                    Line(S.Initiator, "Leave {DISTRICT} before things get ugly.", 0),
                    Line(S.Responder, "Threats? From the {FACTION_OTHER}? Amusing.", 1),
                }));

            templates.Add(Create("hostile_border_warning", ConversationTopicTag.Threat,
                crossFaction: true, minRep: -100, maxRep: -1,
                lines: new[]
                {
                    Line(S.Initiator, "The next patrol that crosses our border won't return.", 0),
                    Line(S.Responder, "Bold words from a faction {CONTROL}.", 1),
                }));

            templates.Add(Create("hostile_markets", ConversationTopicTag.Taunt,
                crossFaction: true, minRep: -100, maxRep: -1,
                lines: new[]
                {
                    Line(S.Initiator, "Stay out of our markets.", 0),
                    Line(S.Responder, "Or what? You'll embargo us again?", 1),
                }));

            // ================================================================
            // CROSS-FACTION: NEUTRAL (interRep between -25 and 25)
            // ================================================================

            templates.Add(Create("neutral_passing", ConversationTopicTag.WaryEncounter,
                crossFaction: true, minRep: -24, maxRep: 24,
                lines: new[]
                {
                    Line(S.Initiator, "You're far from {FACTION_SELF} territory.", 0),
                    Line(S.Responder, "Just passing through. No trouble.", 1),
                }));

            templates.Add(Create("neutral_hands", ConversationTopicTag.WaryEncounter,
                crossFaction: true, minRep: -24, maxRep: 24,
                lines: new[]
                {
                    Line(S.Initiator, "Keep your hands where I can see them.", 0),
                    Line(S.Responder, "Likewise.", 1),
                }));

            templates.Add(Create("neutral_strange_times", ConversationTopicTag.WaryEncounter,
                crossFaction: true, minRep: -24, maxRep: 24,
                lines: new[]
                {
                    Line(S.Initiator, "Strange times when {FACTION_SELF} and {FACTION_OTHER} share the same road.", 0),
                    Line(S.Responder, "Strange indeed. But not unwelcome.", 1),
                }));

            templates.Add(Create("neutral_no_quarrel", ConversationTopicTag.WaryEncounter,
                crossFaction: true, minRep: -24, maxRep: 24,
                lines: new[]
                {
                    Line(S.Initiator, "I have no quarrel with you. Today.", 0),
                    Line(S.Responder, "Today is all we've got.", 1),
                }));

            // ================================================================
            // CROSS-FACTION: FRIENDLY (interRep >= 25)
            // ================================================================

            templates.Add(Create("friendly_trade_open", ConversationTopicTag.AllianceAffirm,
                crossFaction: true, minRep: 25, maxRep: 100,
                lines: new[]
                {
                    Line(S.Initiator, "Trade is {TRADE_STATUS} between us. Your merchants are welcome.", 0),
                    Line(S.Responder, "And yours. May the ink flow freely.", 1),
                }));

            templates.Add(Create("friendly_alliance", ConversationTopicTag.AllianceAffirm,
                crossFaction: true, minRep: 25, maxRep: 100,
                lines: new[]
                {
                    Line(S.Initiator, "Our alliance holds strong in {DISTRICT}.", 0),
                    Line(S.Responder, "As long as {PROSPERITY}, we have no quarrel.", 1),
                }));

            templates.Add(Create("friendly_banner", ConversationTopicTag.AllianceAffirm,
                crossFaction: true, minRep: 25, maxRep: 100,
                lines: new[]
                {
                    Line(S.Initiator, "Good to see the {FACTION_OTHER} banner.", 0),
                    Line(S.Responder, "And yours. Allies are hard to come by.", 1),
                }));

            templates.Add(Create("friendly_healers", ConversationTopicTag.AllianceAffirm,
                crossFaction: true, minRep: 25, maxRep: 100,
                lines: new[]
                {
                    Line(S.Initiator, "Your healers saved two of mine last week.", 0),
                    Line(S.Responder, "That's what allies do.", 1),
                }));

            // ================================================================
            // CROSS-FACTION: TRADE SPECIFIC
            // ================================================================

            templates.Add(Create("trade_embargo_strangling", ConversationTopicTag.TradeEmbargo,
                crossFaction: true, minRep: -100, maxRep: 100,
                lines: new[]
                {
                    Line(S.Initiator, "The embargo on {FACTION_OTHER} goods is strangling the market.", 0),
                    Line(S.Responder, "They brought it on themselves.", 1),
                }));

            templates.Add(Create("trade_leather_iron", ConversationTopicTag.TradeNegotiation,
                crossFaction: true, minRep: -24, maxRep: 100,
                lines: new[]
                {
                    Line(S.Initiator, "We could use your leather. What's your price?", 0),
                    Line(S.Responder, "Fair, if you have iron to trade.", 1),
                }));

            templates.Add(Create("trade_business", ConversationTopicTag.TradeNegotiation,
                crossFaction: true, minRep: -24, maxRep: 100,
                lines: new[]
                {
                    Line(S.Initiator, "Business is {PROSPERITY}. Perhaps we can arrange something.", 0),
                    Line(S.Responder, "Perhaps. If the terms are right.", 1),
                }));

            // ================================================================
            // WORLD CONTEXT: TERRITORY CONTEST
            // ================================================================

            templates.Add(Create("contest_wont_stay", ConversationTopicTag.TerritoryContest,
                lines: new[]
                {
                    Line(S.Initiator, "{DISTRICT} won't stay {CONTROL} for long.", 0),
                    Line(S.Responder, "We'll see about that.", 1),
                }));

            templates.Add(Create("contest_three_days", ConversationTopicTag.TerritoryContest,
                sameFaction: true,
                lines: new[]
                {
                    Line(S.Initiator, "Three days we've held this ground. Three days of fighting.", 0),
                    Line(S.Responder, "Hold it three more and it's ours.", 1),
                }));

            templates.Add(Create("contest_by_right", ConversationTopicTag.TerritoryContest,
                crossFaction: true, minRep: -100, maxRep: 24,
                lines: new[]
                {
                    Line(S.Initiator, "This territory is ours by right.", 0),
                    Line(S.Responder, "Rights mean nothing without strength to hold them.", 1),
                }));

            // ================================================================
            // WORLD CONTEXT: PROSPERITY LAMENT
            // ================================================================

            templates.Add(Create("lament_district", ConversationTopicTag.ProsperityLament,
                lines: new[]
                {
                    Line(S.Initiator, "The people in {DISTRICT} are {PROSPERITY}.", 0),
                    Line(S.Responder, "If supplies don't come soon, there'll be trouble.", 1),
                }));

            templates.Add(Create("lament_markets_dry", ConversationTopicTag.ProsperityLament,
                lines: new[]
                {
                    Line(S.Initiator, "Markets are drying up. Half the stalls are empty.", 0),
                    Line(S.Responder, "War takes its toll on everyone.", 1),
                }));

            // ================================================================
            // WORLD CONTEXT: RAID WARNING
            // ================================================================

            templates.Add(Create("raid_border_patrols", ConversationTopicTag.RaidWarning,
                sameFaction: true,
                lines: new[]
                {
                    Line(S.Initiator, "Enemy patrols spotted near the border.", 0),
                    Line(S.Responder, "Sound the alarm. Get everyone to positions.", 1),
                }));

            templates.Add(Create("raid_supply_wagons", ConversationTopicTag.RaidWarning,
                sameFaction: true,
                lines: new[]
                {
                    Line(S.Initiator, "They hit the supply wagons last night.", 0),
                    Line(S.Responder, "That's the third this week. We need reinforcements.", 1),
                }));

            // ================================================================
            // WORLD CONTEXT: QUEST HINT
            // ================================================================

            templates.Add(Create("quest_demon_hides", ConversationTopicTag.QuestHint,
                sameFaction: true,
                lines: new[]
                {
                    Line(S.Initiator, "Someone in {DISTRICT} was offering coin for demon hides.", 0),
                    Line(S.Responder, "Might be worth checking out.", 1),
                }));

            templates.Add(Create("quest_inscriptions", ConversationTopicTag.QuestHint,
                sameFaction: true,
                lines: new[]
                {
                    Line(S.Initiator, "The scribes want someone to map the inscriptions near {DISTRICT}.", 0),
                    Line(S.Responder, "Dangerous work. The ink is unstable there.", 1),
                    Line(S.Initiator, "Dangerous pays well.", 2),
                }));

            // ================================================================
            // 3-LINE CONVERSATIONS (extended exchanges)
            // ================================================================

            // Same-faction: extended greeting
            templates.Add(Create("greet_wall_duty", ConversationTopicTag.Greeting,
                sameFaction: true,
                lines: new[]
                {
                    Line(S.Initiator, "How's the wall?", 0),
                    Line(S.Responder, "Quiet. Too quiet for {DISTRICT}.", 1),
                    Line(S.Initiator, "Stay sharp. That's when they hit.", 2),
                }));

            templates.Add(Create("greet_new_recruit", ConversationTopicTag.Greeting,
                sameFaction: true,
                lines: new[]
                {
                    Line(S.Initiator, "You're new here.", 0),
                    Line(S.Responder, "Transferred from the outer posts. What's {DISTRICT} like?", 1),
                    Line(S.Initiator, "Keep your head down and you'll be fine.", 2),
                }));

            // Same-faction: extended rumor
            templates.Add(Create("rumor_deserters", ConversationTopicTag.Rumor,
                sameFaction: true,
                lines: new[]
                {
                    Line(S.Initiator, "Three soldiers deserted last night.", 0),
                    Line(S.Responder, "Can you blame them? Morale is {PROSPERITY}.", 1),
                    Line(S.Initiator, "Blame them? No. But the captain will.", 2),
                }));

            templates.Add(Create("rumor_old_ruins", ConversationTopicTag.Rumor,
                sameFaction: true,
                lines: new[]
                {
                    Line(S.Initiator, "They found something in the old ruins.", 0),
                    Line(S.Responder, "What kind of something?", 1),
                    Line(S.Initiator, "The kind nobody wants to talk about.", 2),
                }));

            // Same-faction: extended status report
            templates.Add(Create("status_supply_lines", ConversationTopicTag.StatusReport,
                sameFaction: true,
                lines: new[]
                {
                    Line(S.Initiator, "Supply lines to {DISTRICT} are stretched thin.", 0),
                    Line(S.Responder, "The {FACTION_OTHER} have been raiding caravans.", 1),
                    Line(S.Initiator, "We need more patrols on the roads.", 2),
                }));

            // Same-faction: extended order
            templates.Add(Create("order_fortify", ConversationTopicTag.OrdersFromAbove,
                sameFaction: true, requireRankDiff: true,
                lines: new[]
                {
                    Line(S.Initiator, "Fortify the southern approach. We're expecting trouble.", 0),
                    Line(S.Responder, "How many should I assign?", 1),
                    Line(S.Initiator, "Everyone you can spare. This is priority.", 2),
                }));

            // Cross-faction hostile: extended threat
            templates.Add(Create("hostile_last_warning", ConversationTopicTag.Threat,
                crossFaction: true, minRep: -100, maxRep: -1,
                lines: new[]
                {
                    Line(S.Initiator, "This is your last warning.", 0),
                    Line(S.Responder, "We don't take orders from the {FACTION_OTHER}.", 1),
                    Line(S.Initiator, "Then you'll take steel.", 2),
                }));

            templates.Add(Create("hostile_burned_village", ConversationTopicTag.Threat,
                crossFaction: true, minRep: -100, maxRep: -1,
                lines: new[]
                {
                    Line(S.Initiator, "We saw what you did to the village.", 0),
                    Line(S.Responder, "War has casualties.", 1),
                    Line(S.Initiator, "So does vengeance.", 2),
                }));

            // Cross-faction neutral: extended encounter
            templates.Add(Create("neutral_trade_road", ConversationTopicTag.WaryEncounter,
                crossFaction: true, minRep: -24, maxRep: 24,
                lines: new[]
                {
                    Line(S.Initiator, "The trade road used to be safe.", 0),
                    Line(S.Responder, "Used to be. Before the inscriptions spread.", 1),
                    Line(S.Initiator, "At least we agree on something.", 2),
                }));

            templates.Add(Create("neutral_common_enemy", ConversationTopicTag.WaryEncounter,
                crossFaction: true, minRep: -24, maxRep: 24,
                lines: new[]
                {
                    Line(S.Initiator, "The demons don't care whose flag you fly.", 0),
                    Line(S.Responder, "No. They just kill.", 1),
                    Line(S.Initiator, "Maybe that's reason enough to stop killing each other.", 2),
                }));

            // Cross-faction friendly: extended alliance
            templates.Add(Create("friendly_joint_patrol", ConversationTopicTag.AllianceAffirm,
                crossFaction: true, minRep: 25, maxRep: 100,
                lines: new[]
                {
                    Line(S.Initiator, "Our commander proposed joint patrols in {DISTRICT}.", 0),
                    Line(S.Responder, "About time. We've been asking for months.", 1),
                    Line(S.Initiator, "Better late than never. When can you start?", 2),
                }));

            templates.Add(Create("friendly_shared_intel", ConversationTopicTag.AllianceAffirm,
                crossFaction: true, minRep: 25, maxRep: 100,
                lines: new[]
                {
                    Line(S.Initiator, "We intercepted {FACTION_OTHER} troop movements near the border.", 0),
                    Line(S.Responder, "Wait — that's us. Those are OUR troops.", 1),
                    Line(S.Initiator, "...Right. Old habits. My apologies.", 2),
                }));

            // Trade: extended negotiation
            templates.Add(Create("trade_ink_deal", ConversationTopicTag.TradeNegotiation,
                crossFaction: true, minRep: -24, maxRep: 100,
                lines: new[]
                {
                    Line(S.Initiator, "We need ink. Badly.", 0),
                    Line(S.Responder, "Everyone needs ink. Price is going up.", 1),
                    Line(S.Initiator, "Name your terms. We can pay.", 2),
                }));

            // Territory: extended contest
            templates.Add(Create("contest_holding_ground", ConversationTopicTag.TerritoryContest,
                sameFaction: true,
                lines: new[]
                {
                    Line(S.Initiator, "They pushed us back from the outer wall.", 0),
                    Line(S.Responder, "Again? That's the third time this season.", 1),
                    Line(S.Initiator, "Third time we take it back, too.", 2),
                }));

            // Prosperity: extended lament
            templates.Add(Create("lament_children_hungry", ConversationTopicTag.ProsperityLament,
                lines: new[]
                {
                    Line(S.Initiator, "The children in {DISTRICT} are going hungry.", 0),
                    Line(S.Responder, "The merchants hoarded what little was left.", 1),
                    Line(S.Initiator, "Someone needs to do something before it's too late.", 2),
                }));

            // Raid: extended warning
            templates.Add(Create("raid_scouts_report", ConversationTopicTag.RaidWarning,
                sameFaction: true,
                lines: new[]
                {
                    Line(S.Initiator, "Scouts report a warband massing to the east.", 0),
                    Line(S.Responder, "How many?", 1),
                    Line(S.Initiator, "More than we can handle alone. Send for reinforcements.", 2),
                }));

            Debug.Log($"[ConversationContentSeeder] Seeded {templates.Count} conversation templates.");
            return templates;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            _seeded = false;
        }

        // ================================================================
        // HELPER METHODS
        // ================================================================

        // Shorthand alias for Speaker enum
        private enum S { Initiator = 0, Responder = 1 }

        private static ConversationLine Line(S speaker, string text, int turnDelay)
        {
            return new ConversationLine
            {
                speaker = speaker == S.Initiator
                    ? ConversationLine.Speaker.Initiator
                    : ConversationLine.Speaker.Responder,
                text = text,
                turnDelay = turnDelay
            };
        }

        private static ConversationTemplate Create(
            string id,
            ConversationTopicTag topic,
            ConversationLine[] lines,
            bool sameFaction = false,
            bool crossFaction = false,
            int minRep = -100,
            int maxRep = 100,
            bool requireRankDiff = false)
        {
            var template = ScriptableObject.CreateInstance<ConversationTemplate>();
            template.id = id;
            template.topic = topic;
            template.sameFactionOnly = sameFaction;
            template.crossFactionOnly = crossFaction;
            template.minInterRep = minRep;
            template.maxInterRep = maxRep;
            template.requireRankDifference = requireRankDiff;
            template.lines = lines;
            template.name = id;
            return template;
        }
    }
}
