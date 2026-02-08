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
                predicate: ConversationPredicates.RequireStableProsperity,
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
                predicate: ConversationPredicates.RequireElevatedHeat,
                cooldownTurns: 10,
                lines: new[]
                {
                    Line(S.Initiator, "Heat's been rising in {DISTRICT}.", 0),
                    Line(S.Responder, "More inscriptions. The scribes are restless.", 1),
                }));

            templates.Add(Create("status_control", ConversationTopicTag.StatusReport,
                sameFaction: true,
                predicate: ConversationPredicates.RequireNotLostControl,
                lines: new[]
                {
                    Line(S.Initiator, "{DISTRICT} is {CONTROL}.", 0),
                    Line(S.Responder, "We hold the line. That's what we do.", 1),
                }));

            templates.Add(Create("status_reinforcements", ConversationTopicTag.StatusReport,
                sameFaction: true,
                predicate: ConversationPredicates.RequireRecentReinforcements,
                cooldownTurns: 15,
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
                predicate: ConversationPredicates.RequireFactionLossEvent,
                cooldownTurns: 20,
                lines: new[]
                {
                    Line(S.Initiator, "Heard the {FACTION_OTHER} lost control of {DISTRICT}.", 0),
                    Line(S.Responder, "Good. Maybe prices will stabilize.", 1),
                }));

            templates.Add(Create("rumor_embargo_ink", ConversationTopicTag.Rumor,
                sameFaction: true,
                predicate: ConversationPredicates.RequireAnyEmbargoExists,
                cooldownTurns: 15,
                lines: new[]
                {
                    Line(S.Initiator, "There's talk of an embargo on ink.", 0),
                    Line(S.Responder, "That would hurt everyone, not just the scribes.", 1),
                }));

            templates.Add(Create("rumor_truce_marker", ConversationTopicTag.Rumor,
                sameFaction: true,
                predicate: ConversationPredicates.RequireTruceInscriptionNearby,
                cooldownTurns: 20,
                lines: new[]
                {
                    Line(S.Initiator, "Someone inscribed a truce marker near the border.", 0),
                    Line(S.Responder, "Desperate times call for desperate measures.", 1),
                }));

            templates.Add(Create("rumor_demons_massing", ConversationTopicTag.Rumor,
                sameFaction: true,
                predicate: ConversationPredicates.RequireDemonThreat,
                cooldownTurns: 15,
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
                predicate: ConversationPredicates.RequirePoorProsperity,
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
                predicate: ConversationPredicates.RequireEmbargoBetweenFactions,
                cooldownTurns: 10,
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
                predicate: ConversationPredicates.RequireContestedThreePlusDays,
                cooldownTurns: 10,
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
                predicate: ConversationPredicates.RequireRecentRaidInDistrict,
                cooldownTurns: 10,
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
                predicate: ConversationPredicates.RequireActiveDemonQuest,
                cooldownTurns: 15,
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
                predicate: ConversationPredicates.RequireLowMorale,
                cooldownTurns: 20,
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
                predicate: ConversationPredicates.RequireSupplyStressAndRaid,
                cooldownTurns: 15,
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
                predicate: ConversationPredicates.RequireRecentAttackByResponderFaction,
                cooldownTurns: 20,
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
                predicate: ConversationPredicates.RequireDemonThreat,
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
                predicate: ConversationPredicates.RequireContestedWithSkirmish,
                cooldownTurns: 10,
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

            // ================================================================
            // HOSTILITY PIPELINE — FACTION-SPECIFIC DIALOGUE
            // 7 stages x 7 factions x 5 lines = 245 templates
            // ================================================================

            SeedHostilityDialogue(templates);

            Debug.Log($"[ConversationContentSeeder] Seeded {templates.Count} conversation templates.");
            return templates;
        }

        // ================================================================
        // HOSTILITY PIPELINE DIALOGUE SEEDER
        // ================================================================

        private static void SeedHostilityDialogue(List<ConversationTemplate> templates)
        {
            // --- LowTension (Uneasy): muttering, suspicion ---
            // INKGUARD
            templates.Add(CreateFaction("hostility_low_inkguard_01", ConversationTopicTag.HostilityLowTension,
                new[] { Line(S.Initiator, "Something stirs in the ranks of the {FACTION_OTHER}. Stay vigilant, brother.", 0),
                        Line(S.Responder, "Aye. The Codex warns of complacency.", 1) },
                "faction_inkguard", ConversationPredicates.RequireTensionUneasy));
            templates.Add(CreateFaction("hostility_low_inkguard_02", ConversationTopicTag.HostilityLowTension,
                new[] { Line(S.Initiator, "I mislike those {FACTION_OTHER} patrols. Too close. Too many.", 0),
                        Line(S.Responder, "Report it to the chapter. Let them decide.", 1) },
                "faction_inkguard", ConversationPredicates.RequireTensionUneasy));
            templates.Add(CreateFaction("hostility_low_inkguard_03", ConversationTopicTag.HostilityLowTension,
                new[] { Line(S.Initiator, "The {FACTION_OTHER} grow bold near {DISTRICT}.", 0),
                        Line(S.Responder, "Bold or foolish. The Codex does not forgive either.", 1) },
                "faction_inkguard", ConversationPredicates.RequireTensionUneasy));
            templates.Add(CreateFaction("hostility_low_inkguard_04", ConversationTopicTag.HostilityLowTension,
                new[] { Line(S.Initiator, "Have you noticed the {FACTION_OTHER} stockpiling near the border?", 0),
                        Line(S.Responder, "I have. Double the sentries tonight.", 1) },
                "faction_inkguard", ConversationPredicates.RequireTensionUneasy));
            templates.Add(CreateFaction("hostility_low_inkguard_05", ConversationTopicTag.HostilityLowTension,
                new[] { Line(S.Initiator, "There was a scuffle at the checkpoint. {FACTION_OTHER} merchants, pushing through.", 0),
                        Line(S.Responder, "Merchants or scouts? Keep your blade close.", 1) },
                "faction_inkguard", ConversationPredicates.RequireTensionUneasy));

            // INKBOUND
            templates.Add(CreateFaction("hostility_low_inkbound_01", ConversationTopicTag.HostilityLowTension,
                new[] { Line(S.Initiator, "The ledgers show irregularities in {FACTION_OTHER} trade routes through {DISTRICT}.", 0),
                        Line(S.Responder, "Document everything. The archives will remember.", 1) },
                "faction_inkbound", ConversationPredicates.RequireTensionUneasy));
            templates.Add(CreateFaction("hostility_low_inkbound_02", ConversationTopicTag.HostilityLowTension,
                new[] { Line(S.Initiator, "The archives speak of similar tensions. They did not end well.", 0),
                        Line(S.Responder, "Then we must be prepared. Knowledge is our shield.", 1) },
                "faction_inkbound", ConversationPredicates.RequireTensionUneasy));
            templates.Add(CreateFaction("hostility_low_inkbound_03", ConversationTopicTag.HostilityLowTension,
                new[] { Line(S.Initiator, "A {FACTION_OTHER} scribe was seen copying our route maps.", 0),
                        Line(S.Responder, "Seal the restricted archives. No unauthorized access.", 1) },
                "faction_inkbound", ConversationPredicates.RequireTensionUneasy));
            templates.Add(CreateFaction("hostility_low_inkbound_04", ConversationTopicTag.HostilityLowTension,
                new[] { Line(S.Initiator, "The {FACTION_OTHER} question our accounting methods. Publicly.", 0),
                        Line(S.Responder, "Let them question. Our records are beyond reproach.", 1) },
                "faction_inkbound", ConversationPredicates.RequireTensionUneasy));
            templates.Add(CreateFaction("hostility_low_inkbound_05", ConversationTopicTag.HostilityLowTension,
                new[] { Line(S.Initiator, "Whispers in the market. The {FACTION_OTHER} spread lies about our prices.", 0),
                        Line(S.Responder, "Publish the true figures. Let ink silence rumor.", 1) },
                "faction_inkbound", ConversationPredicates.RequireTensionUneasy));

            // SKELETON
            templates.Add(CreateFaction("hostility_low_skeleton_01", ConversationTopicTag.HostilityLowTension,
                new[] { Line(S.Initiator, "The {FACTION_OTHER} eye us with fresh disgust. As if we chose this unlife.", 0),
                        Line(S.Responder, "Let them stare. We endure.", 1) },
                "faction_skeleton", ConversationPredicates.RequireTensionUneasy));
            templates.Add(CreateFaction("hostility_low_skeleton_02", ConversationTopicTag.HostilityLowTension,
                new[] { Line(S.Initiator, "I can feel it in my bones. Literally. Trouble brews.", 0),
                        Line(S.Responder, "Your joints creak with wisdom, friend.", 1) },
                "faction_skeleton", ConversationPredicates.RequireTensionUneasy));
            templates.Add(CreateFaction("hostility_low_skeleton_03", ConversationTopicTag.HostilityLowTension,
                new[] { Line(S.Initiator, "A {FACTION_OTHER} child threw a stone at me today.", 0),
                        Line(S.Responder, "They learn hatred young. We should pity them.", 1) },
                "faction_skeleton", ConversationPredicates.RequireTensionUneasy));
            templates.Add(CreateFaction("hostility_low_skeleton_04", ConversationTopicTag.HostilityLowTension,
                new[] { Line(S.Initiator, "The {FACTION_OTHER} barred us from the market again.", 0),
                        Line(S.Responder, "Their loss. Our coin spends the same as any.", 1) },
                "faction_skeleton", ConversationPredicates.RequireTensionUneasy));
            templates.Add(CreateFaction("hostility_low_skeleton_05", ConversationTopicTag.HostilityLowTension,
                new[] { Line(S.Initiator, "Something about the {FACTION_OTHER} has changed. Their eyes hold... intent.", 0),
                        Line(S.Responder, "Watch the borders. The dead sleep lightly.", 1) },
                "faction_skeleton", ConversationPredicates.RequireTensionUneasy));

            // GHOST
            templates.Add(CreateFaction("hostility_low_ghost_01", ConversationTopicTag.HostilityLowTension,
                new[] { Line(S.Initiator, "The living of {FACTION_OTHER} carry a familiar weight. I have seen it before the last war.", 0),
                        Line(S.Responder, "History echoes. We remember what they forget.", 1) },
                "faction_ghost", ConversationPredicates.RequireTensionUneasy));
            templates.Add(CreateFaction("hostility_low_ghost_02", ConversationTopicTag.HostilityLowTension,
                new[] { Line(S.Initiator, "Memory stirs. This feeling... it echoes.", 0),
                        Line(S.Responder, "What do the echoes say?", 1),
                        Line(S.Initiator, "They say prepare.", 2) },
                "faction_ghost", ConversationPredicates.RequireTensionUneasy));
            templates.Add(CreateFaction("hostility_low_ghost_03", ConversationTopicTag.HostilityLowTension,
                new[] { Line(S.Initiator, "The {FACTION_OTHER} cannot see us watching. That is our advantage.", 0),
                        Line(S.Responder, "We see all and forget nothing.", 1) },
                "faction_ghost", ConversationPredicates.RequireTensionUneasy));
            templates.Add(CreateFaction("hostility_low_ghost_04", ConversationTopicTag.HostilityLowTension,
                new[] { Line(S.Initiator, "I drifted through their camp last night. They sharpen blades.", 0),
                        Line(S.Responder, "Blades cannot cut what has already died.", 1) },
                "faction_ghost", ConversationPredicates.RequireTensionUneasy));
            templates.Add(CreateFaction("hostility_low_ghost_05", ConversationTopicTag.HostilityLowTension,
                new[] { Line(S.Initiator, "The air grows heavy near {DISTRICT}. The {FACTION_OTHER} bring unease.", 0),
                        Line(S.Responder, "We have weathered worse. The veil holds.", 1) },
                "faction_ghost", ConversationPredicates.RequireTensionUneasy));

            // GOBLIN
            templates.Add(CreateFaction("hostility_low_goblin_01", ConversationTopicTag.HostilityLowTension,
                new[] { Line(S.Initiator, "Them {FACTION_OTHER} been eyeballin' our stuff again. Keep yer shivs close.", 0),
                        Line(S.Responder, "Always do, boss. Always do.", 1) },
                "faction_goblin", ConversationPredicates.RequireTensionUneasy));
            templates.Add(CreateFaction("hostility_low_goblin_02", ConversationTopicTag.HostilityLowTension,
                new[] { Line(S.Initiator, "Boss says watch the {FACTION_OTHER}. I says we already are.", 0),
                        Line(S.Responder, "Watch harder then. Boss ain't wrong.", 1) },
                "faction_goblin", ConversationPredicates.RequireTensionUneasy));
            templates.Add(CreateFaction("hostility_low_goblin_03", ConversationTopicTag.HostilityLowTension,
                new[] { Line(S.Initiator, "Oi, a {FACTION_OTHER} patrol went past our stash twice today.", 0),
                        Line(S.Responder, "Move the stash. Move it now.", 1) },
                "faction_goblin", ConversationPredicates.RequireTensionUneasy));
            templates.Add(CreateFaction("hostility_low_goblin_04", ConversationTopicTag.HostilityLowTension,
                new[] { Line(S.Initiator, "I don't like the way them {FACTION_OTHER} smell lately. Smells like trouble.", 0),
                        Line(S.Responder, "Everything smells like trouble to you.", 1),
                        Line(S.Initiator, "And I'm usually right!", 2) },
                "faction_goblin", ConversationPredicates.RequireTensionUneasy));
            templates.Add(CreateFaction("hostility_low_goblin_05", ConversationTopicTag.HostilityLowTension,
                new[] { Line(S.Initiator, "Someone nicked three shinies from the pile. Bet it was them {FACTION_OTHER}.", 0),
                        Line(S.Responder, "Could've been rats.", 1),
                        Line(S.Initiator, "{FACTION_OTHER} rats.", 2) },
                "faction_goblin", ConversationPredicates.RequireTensionUneasy));

            // DEMON
            templates.Add(CreateFaction("hostility_low_demon_01", ConversationTopicTag.HostilityLowTension,
                new[] { Line(S.Initiator, "The {FACTION_OTHER} grow restless. How tedious.", 0),
                        Line(S.Responder, "Shall we remind them of their station?", 1),
                        Line(S.Initiator, "Not yet. Let them fester.", 2) },
                "faction_demon", ConversationPredicates.RequireTensionUneasy));
            templates.Add(CreateFaction("hostility_low_demon_02", ConversationTopicTag.HostilityLowTension,
                new[] { Line(S.Initiator, "I sense defiance from {DISTRICT}. The {FACTION_OTHER} forget their place.", 0),
                        Line(S.Responder, "They will be reminded. In time.", 1) },
                "faction_demon", ConversationPredicates.RequireTensionUneasy));
            templates.Add(CreateFaction("hostility_low_demon_03", ConversationTopicTag.HostilityLowTension,
                new[] { Line(S.Initiator, "The {FACTION_OTHER} whisper behind closed doors. Plotting.", 0),
                        Line(S.Responder, "Let them plot. Insects cannot harm the inferno.", 1) },
                "faction_demon", ConversationPredicates.RequireTensionUneasy));
            templates.Add(CreateFaction("hostility_low_demon_04", ConversationTopicTag.HostilityLowTension,
                new[] { Line(S.Initiator, "I tasted fear on the wind from {DISTRICT}. The {FACTION_OTHER} are afraid.", 0),
                        Line(S.Responder, "Good. Fear is the foundation of obedience.", 1) },
                "faction_demon", ConversationPredicates.RequireTensionUneasy));
            templates.Add(CreateFaction("hostility_low_demon_05", ConversationTopicTag.HostilityLowTension,
                new[] { Line(S.Initiator, "A {FACTION_OTHER} courier crossed our territory without tribute.", 0),
                        Line(S.Responder, "An oversight, surely. We shall collect... with interest.", 1) },
                "faction_demon", ConversationPredicates.RequireTensionUneasy));

            // SNAKE
            templates.Add(CreateFaction("hostility_low_snake_01", ConversationTopicTag.HostilityLowTension,
                new[] { Line(S.Initiator, "The {FACTION_OTHER} trespasss more boldly each day. Thisss will not ssstand.", 0),
                        Line(S.Responder, "Patience, sssibling. We watch and wait.", 1) },
                "faction_snake", ConversationPredicates.RequireTensionUneasy));
            templates.Add(CreateFaction("hostility_low_snake_02", ConversationTopicTag.HostilityLowTension,
                new[] { Line(S.Initiator, "Tasste the air. The {FACTION_OTHER} carry sssteel beneath their cloaks.", 0),
                        Line(S.Responder, "We carry venom beneath our ssscales. Let them come.", 1) },
                "faction_snake", ConversationPredicates.RequireTensionUneasy));
            templates.Add(CreateFaction("hostility_low_snake_03", ConversationTopicTag.HostilityLowTension,
                new[] { Line(S.Initiator, "The {FACTION_OTHER} crussshed a nest near the border.", 0),
                        Line(S.Responder, "An accident, they claim. We know better.", 1) },
                "faction_snake", ConversationPredicates.RequireTensionUneasy));
            templates.Add(CreateFaction("hostility_low_snake_04", ConversationTopicTag.HostilityLowTension,
                new[] { Line(S.Initiator, "Their ssscouts move through our hunting grounds. Unannounced.", 0),
                        Line(S.Responder, "Mark their pathsss. Knowledge before vengeance.", 1) },
                "faction_snake", ConversationPredicates.RequireTensionUneasy));
            templates.Add(CreateFaction("hostility_low_snake_05", ConversationTopicTag.HostilityLowTension,
                new[] { Line(S.Initiator, "I sssensed a {FACTION_OTHER} watching our warren entrance.", 0),
                        Line(S.Responder, "Post guardsss. The Nest must be protected.", 1) },
                "faction_snake", ConversationPredicates.RequireTensionUneasy));

            // --- Warning (Tense): verbal threats, ultimatums ---
            // INKGUARD
            templates.Add(CreateFaction("hostility_warn_inkguard_01", ConversationTopicTag.HostilityWarning,
                new[] { Line(S.Initiator, "By the Codex, withdraw from {DISTRICT} or face the chapter's judgment.", 0) },
                "faction_inkguard", ConversationPredicates.RequireTensionTense));
            templates.Add(CreateFaction("hostility_warn_inkguard_02", ConversationTopicTag.HostilityWarning,
                new[] { Line(S.Initiator, "This is your final warning, {FACTION_OTHER}. The Codex demands order.", 0),
                        Line(S.Responder, "Your codex means nothing here.", 1),
                        Line(S.Initiator, "Then steel will speak where law cannot.", 2) },
                "faction_inkguard", ConversationPredicates.RequireTensionTense));
            templates.Add(CreateFaction("hostility_warn_inkguard_03", ConversationTopicTag.HostilityWarning,
                new[] { Line(S.Initiator, "The chapter has noted your transgressions, {FACTION_OTHER}.", 0),
                        Line(S.Responder, "Note all you wish. We are not afraid.", 1) },
                "faction_inkguard", ConversationPredicates.RequireTensionTense));
            templates.Add(CreateFaction("hostility_warn_inkguard_04", ConversationTopicTag.HostilityWarning,
                new[] { Line(S.Initiator, "Withdraw your forces from {DISTRICT}. This will not be asked again.", 0) },
                "faction_inkguard", ConversationPredicates.RequireTensionTense));
            templates.Add(CreateFaction("hostility_warn_inkguard_05", ConversationTopicTag.HostilityWarning,
                new[] { Line(S.Initiator, "The Codex is patient. We are not. Leave {DISTRICT}.", 0) },
                "faction_inkguard", ConversationPredicates.RequireTensionTense));

            // INKBOUND
            templates.Add(CreateFaction("hostility_warn_inkbound_01", ConversationTopicTag.HostilityWarning,
                new[] { Line(S.Initiator, "Our scribes have documented your transgressions, {FACTION_OTHER}. The record is not in your favor.", 0) },
                "faction_inkbound", ConversationPredicates.RequireTensionTense));
            templates.Add(CreateFaction("hostility_warn_inkbound_02", ConversationTopicTag.HostilityWarning,
                new[] { Line(S.Initiator, "The archives will remember what you have done. History judges harshly.", 0) },
                "faction_inkbound", ConversationPredicates.RequireTensionTense));
            templates.Add(CreateFaction("hostility_warn_inkbound_03", ConversationTopicTag.HostilityWarning,
                new[] { Line(S.Initiator, "We have binding wards older than your faction. Do not test us.", 0) },
                "faction_inkbound", ConversationPredicates.RequireTensionTense));
            templates.Add(CreateFaction("hostility_warn_inkbound_04", ConversationTopicTag.HostilityWarning,
                new[] { Line(S.Initiator, "Consider this a formal notice: cease operations in {DISTRICT} or face sanctions.", 0) },
                "faction_inkbound", ConversationPredicates.RequireTensionTense));
            templates.Add(CreateFaction("hostility_warn_inkbound_05", ConversationTopicTag.HostilityWarning,
                new[] { Line(S.Initiator, "Every debt is recorded. Every transgression catalogued. You owe much, {FACTION_OTHER}.", 0) },
                "faction_inkbound", ConversationPredicates.RequireTensionTense));

            // SKELETON
            templates.Add(CreateFaction("hostility_warn_skeleton_01", ConversationTopicTag.HostilityWarning,
                new[] { Line(S.Initiator, "Keep pushing, {FACTION_OTHER}. We have all the time in the world. You do not.", 0) },
                "faction_skeleton", ConversationPredicates.RequireTensionTense));
            templates.Add(CreateFaction("hostility_warn_skeleton_02", ConversationTopicTag.HostilityWarning,
                new[] { Line(S.Initiator, "The dead are patient, {FACTION_OTHER}. But patience has its limits.", 0) },
                "faction_skeleton", ConversationPredicates.RequireTensionTense));
            templates.Add(CreateFaction("hostility_warn_skeleton_03", ConversationTopicTag.HostilityWarning,
                new[] { Line(S.Initiator, "You think bones cannot hold a grudge? Try us.", 0) },
                "faction_skeleton", ConversationPredicates.RequireTensionTense));
            templates.Add(CreateFaction("hostility_warn_skeleton_04", ConversationTopicTag.HostilityWarning,
                new[] { Line(S.Initiator, "We remember every slight, {FACTION_OTHER}. Every single one.", 0) },
                "faction_skeleton", ConversationPredicates.RequireTensionTense));
            templates.Add(CreateFaction("hostility_warn_skeleton_05", ConversationTopicTag.HostilityWarning,
                new[] { Line(S.Initiator, "Your flesh will rot. Your bones will join ours. Think on that.", 0) },
                "faction_skeleton", ConversationPredicates.RequireTensionTense));

            // GHOST
            templates.Add(CreateFaction("hostility_warn_ghost_01", ConversationTopicTag.HostilityWarning,
                new[] { Line(S.Initiator, "We remember the last ones who threatened this place. Do you know where they are now? With us.", 0) },
                "faction_ghost", ConversationPredicates.RequireTensionTense));
            templates.Add(CreateFaction("hostility_warn_ghost_02", ConversationTopicTag.HostilityWarning,
                new[] { Line(S.Initiator, "Turn back, {FACTION_OTHER}. The veil is thin here, and we are many.", 0) },
                "faction_ghost", ConversationPredicates.RequireTensionTense));
            templates.Add(CreateFaction("hostility_warn_ghost_03", ConversationTopicTag.HostilityWarning,
                new[] { Line(S.Initiator, "You cannot fight what you cannot touch. Leave this place.", 0) },
                "faction_ghost", ConversationPredicates.RequireTensionTense));
            templates.Add(CreateFaction("hostility_warn_ghost_04", ConversationTopicTag.HostilityWarning,
                new[] { Line(S.Initiator, "We have watched empires rise and fall. You are no empire.", 0) },
                "faction_ghost", ConversationPredicates.RequireTensionTense));
            templates.Add(CreateFaction("hostility_warn_ghost_05", ConversationTopicTag.HostilityWarning,
                new[] { Line(S.Initiator, "The echoes grow louder, {FACTION_OTHER}. That is not a good sign. For you.", 0) },
                "faction_ghost", ConversationPredicates.RequireTensionTense));

            // GOBLIN
            templates.Add(CreateFaction("hostility_warn_goblin_01", ConversationTopicTag.HostilityWarning,
                new[] { Line(S.Initiator, "Oi! One more step and we start chuckin' rocks. Big ones.", 0) },
                "faction_goblin", ConversationPredicates.RequireTensionTense));
            templates.Add(CreateFaction("hostility_warn_goblin_02", ConversationTopicTag.HostilityWarning,
                new[] { Line(S.Initiator, "Back off, {FACTION_OTHER}! This is goblin turf and we ain't sharin'!", 0) },
                "faction_goblin", ConversationPredicates.RequireTensionTense));
            templates.Add(CreateFaction("hostility_warn_goblin_03", ConversationTopicTag.HostilityWarning,
                new[] { Line(S.Initiator, "I got a shiv with yer name on it, {FACTION_OTHER}. Want a closer look?", 0) },
                "faction_goblin", ConversationPredicates.RequireTensionTense));
            templates.Add(CreateFaction("hostility_warn_goblin_04", ConversationTopicTag.HostilityWarning,
                new[] { Line(S.Initiator, "Boss says if you lot don't scram, we get to keep what's left of ya.", 0) },
                "faction_goblin", ConversationPredicates.RequireTensionTense));
            templates.Add(CreateFaction("hostility_warn_goblin_05", ConversationTopicTag.HostilityWarning,
                new[] { Line(S.Initiator, "You think goblins are funny? We'll see who's laughin' when the stabbin' starts.", 0) },
                "faction_goblin", ConversationPredicates.RequireTensionTense));

            // DEMON
            templates.Add(CreateFaction("hostility_warn_demon_01", ConversationTopicTag.HostilityWarning,
                new[] { Line(S.Initiator, "You dare approach our domain with such insolence? Flee now, worm.", 0) },
                "faction_demon", ConversationPredicates.RequireTensionTense));
            templates.Add(CreateFaction("hostility_warn_demon_02", ConversationTopicTag.HostilityWarning,
                new[] { Line(S.Initiator, "Your defiance amuses me, {FACTION_OTHER}. It will not save you.", 0) },
                "faction_demon", ConversationPredicates.RequireTensionTense));
            templates.Add(CreateFaction("hostility_warn_demon_03", ConversationTopicTag.HostilityWarning,
                new[] { Line(S.Initiator, "I will give you one chance to grovel. Use it wisely.", 0) },
                "faction_demon", ConversationPredicates.RequireTensionTense));
            templates.Add(CreateFaction("hostility_warn_demon_04", ConversationTopicTag.HostilityWarning,
                new[] { Line(S.Initiator, "The flames await those who defy us. This is not a metaphor.", 0) },
                "faction_demon", ConversationPredicates.RequireTensionTense));
            templates.Add(CreateFaction("hostility_warn_demon_05", ConversationTopicTag.HostilityWarning,
                new[] { Line(S.Initiator, "We have tormented souls for eons. You are nothing special, {FACTION_OTHER}.", 0) },
                "faction_demon", ConversationPredicates.RequireTensionTense));

            // SNAKE
            templates.Add(CreateFaction("hostility_warn_snake_01", ConversationTopicTag.HostilityWarning,
                new[] { Line(S.Initiator, "Crosss our border again and we ssshall not be ssso merciful.", 0) },
                "faction_snake", ConversationPredicates.RequireTensionTense));
            templates.Add(CreateFaction("hostility_warn_snake_02", ConversationTopicTag.HostilityWarning,
                new[] { Line(S.Initiator, "The Nest hasss decreed: your pressence in {DISTRICT} isss no longer tolerated.", 0) },
                "faction_snake", ConversationPredicates.RequireTensionTense));
            templates.Add(CreateFaction("hostility_warn_snake_03", ConversationTopicTag.HostilityWarning,
                new[] { Line(S.Initiator, "One bite. That isss all it takesss. Remember that, {FACTION_OTHER}.", 0) },
                "faction_snake", ConversationPredicates.RequireTensionTense));
            templates.Add(CreateFaction("hostility_warn_snake_04", ConversationTopicTag.HostilityWarning,
                new[] { Line(S.Initiator, "We ssslither where you cannot sssee. Leave before we prove it.", 0) },
                "faction_snake", ConversationPredicates.RequireTensionTense));
            templates.Add(CreateFaction("hostility_warn_snake_05", ConversationTopicTag.HostilityWarning,
                new[] { Line(S.Initiator, "The venom isss prepared, {FACTION_OTHER}. Do not make usss ussse it.", 0) },
                "faction_snake", ConversationPredicates.RequireTensionTense));

            // --- Grievance (Tense+): incident complaints ---
            // INKGUARD
            templates.Add(CreateFaction("hostility_griev_inkguard_01", ConversationTopicTag.HostilityGrievance,
                new[] { Line(S.Initiator, "Your people committed {INCIDENT_TYPE} in broad daylight. The chapter demands reparation.", 0) },
                "faction_inkguard", ConversationPredicates.RequireRecentIncident));
            templates.Add(CreateFaction("hostility_griev_inkguard_02", ConversationTopicTag.HostilityGrievance,
                new[] { Line(S.Initiator, "The {INCIDENT_TYPE} will not be forgotten. The Codex records all.", 0) },
                "faction_inkguard", ConversationPredicates.RequireRecentIncident));
            templates.Add(CreateFaction("hostility_griev_inkguard_03", ConversationTopicTag.HostilityGrievance,
                new[] { Line(S.Initiator, "Do you deny the {INCIDENT_TYPE}? We have witnesses.", 0) },
                "faction_inkguard", ConversationPredicates.RequireRecentIncident));
            templates.Add(CreateFaction("hostility_griev_inkguard_04", ConversationTopicTag.HostilityGrievance,
                new[] { Line(S.Initiator, "First the {INCIDENT_TYPE}, now this. Your faction tests our patience.", 0) },
                "faction_inkguard", ConversationPredicates.RequireRecentIncident));
            templates.Add(CreateFaction("hostility_griev_inkguard_05", ConversationTopicTag.HostilityGrievance,
                new[] { Line(S.Initiator, "Justice will answer for the {INCIDENT_TYPE}. One way or another.", 0) },
                "faction_inkguard", ConversationPredicates.RequireRecentIncident));

            // INKBOUND
            templates.Add(CreateFaction("hostility_griev_inkbound_01", ConversationTopicTag.HostilityGrievance,
                new[] { Line(S.Initiator, "Our records show seventeen instances of {INCIDENT_TYPE} this season alone.", 0) },
                "faction_inkbound", ConversationPredicates.RequireRecentIncident));
            templates.Add(CreateFaction("hostility_griev_inkbound_02", ConversationTopicTag.HostilityGrievance,
                new[] { Line(S.Initiator, "The {INCIDENT_TYPE} has been filed. Your debt grows, {FACTION_OTHER}.", 0) },
                "faction_inkbound", ConversationPredicates.RequireRecentIncident));
            templates.Add(CreateFaction("hostility_griev_inkbound_03", ConversationTopicTag.HostilityGrievance,
                new[] { Line(S.Initiator, "Page forty-seven, clause nine: {INCIDENT_TYPE} carries severe penalties.", 0) },
                "faction_inkbound", ConversationPredicates.RequireRecentIncident));
            templates.Add(CreateFaction("hostility_griev_inkbound_04", ConversationTopicTag.HostilityGrievance,
                new[] { Line(S.Initiator, "We documented the {INCIDENT_TYPE} in triplicate. The archives demand restitution.", 0) },
                "faction_inkbound", ConversationPredicates.RequireRecentIncident));
            templates.Add(CreateFaction("hostility_griev_inkbound_05", ConversationTopicTag.HostilityGrievance,
                new[] { Line(S.Initiator, "The ink dries on your {INCIDENT_TYPE} report. It is not flattering.", 0) },
                "faction_inkbound", ConversationPredicates.RequireRecentIncident));

            // SKELETON
            templates.Add(CreateFaction("hostility_griev_skeleton_01", ConversationTopicTag.HostilityGrievance,
                new[] { Line(S.Initiator, "You desecrate our graves and call it {INCIDENT_TYPE}? We call it war.", 0) },
                "faction_skeleton", ConversationPredicates.RequireRecentIncident));
            templates.Add(CreateFaction("hostility_griev_skeleton_02", ConversationTopicTag.HostilityGrievance,
                new[] { Line(S.Initiator, "The {INCIDENT_TYPE} disturbed the resting. They are not pleased.", 0) },
                "faction_skeleton", ConversationPredicates.RequireRecentIncident));
            templates.Add(CreateFaction("hostility_griev_skeleton_03", ConversationTopicTag.HostilityGrievance,
                new[] { Line(S.Initiator, "Even the dead have honor. Your {INCIDENT_TYPE} insults it.", 0) },
                "faction_skeleton", ConversationPredicates.RequireRecentIncident));
            templates.Add(CreateFaction("hostility_griev_skeleton_04", ConversationTopicTag.HostilityGrievance,
                new[] { Line(S.Initiator, "We felt the {INCIDENT_TYPE} in our bones. Literally.", 0) },
                "faction_skeleton", ConversationPredicates.RequireRecentIncident));
            templates.Add(CreateFaction("hostility_griev_skeleton_05", ConversationTopicTag.HostilityGrievance,
                new[] { Line(S.Initiator, "Your {INCIDENT_TYPE} woke three of our eldest. They are not happy.", 0) },
                "faction_skeleton", ConversationPredicates.RequireRecentIncident));

            // GHOST
            templates.Add(CreateFaction("hostility_griev_ghost_01", ConversationTopicTag.HostilityGrievance,
                new[] { Line(S.Initiator, "The echoes of your {INCIDENT_TYPE} still haunt these halls.", 0) },
                "faction_ghost", ConversationPredicates.RequireRecentIncident));
            templates.Add(CreateFaction("hostility_griev_ghost_02", ConversationTopicTag.HostilityGrievance,
                new[] { Line(S.Initiator, "Your {INCIDENT_TYPE} left scars on the veil. We feel every one.", 0) },
                "faction_ghost", ConversationPredicates.RequireRecentIncident));
            templates.Add(CreateFaction("hostility_griev_ghost_03", ConversationTopicTag.HostilityGrievance,
                new[] { Line(S.Initiator, "The memories of your {INCIDENT_TYPE} will linger for centuries.", 0) },
                "faction_ghost", ConversationPredicates.RequireRecentIncident));
            templates.Add(CreateFaction("hostility_griev_ghost_04", ConversationTopicTag.HostilityGrievance,
                new[] { Line(S.Initiator, "We witnessed the {INCIDENT_TYPE}. We always witness.", 0) },
                "faction_ghost", ConversationPredicates.RequireRecentIncident));
            templates.Add(CreateFaction("hostility_griev_ghost_05", ConversationTopicTag.HostilityGrievance,
                new[] { Line(S.Initiator, "Your {INCIDENT_TYPE} has created new ghosts. They cry for justice.", 0) },
                "faction_ghost", ConversationPredicates.RequireRecentIncident));

            // GOBLIN
            templates.Add(CreateFaction("hostility_griev_goblin_01", ConversationTopicTag.HostilityGrievance,
                new[] { Line(S.Initiator, "You stole our stuff! That {INCIDENT_TYPE} cost us three barrels of grog!", 0) },
                "faction_goblin", ConversationPredicates.RequireRecentIncident));
            templates.Add(CreateFaction("hostility_griev_goblin_02", ConversationTopicTag.HostilityGrievance,
                new[] { Line(S.Initiator, "The boys are MAD about the {INCIDENT_TYPE}. Like, really mad.", 0) },
                "faction_goblin", ConversationPredicates.RequireRecentIncident));
            templates.Add(CreateFaction("hostility_griev_goblin_03", ConversationTopicTag.HostilityGrievance,
                new[] { Line(S.Initiator, "That {INCIDENT_TYPE} was the last straw! We're countin' to ten and then it's STABBIN' time!", 0) },
                "faction_goblin", ConversationPredicates.RequireRecentIncident));
            templates.Add(CreateFaction("hostility_griev_goblin_04", ConversationTopicTag.HostilityGrievance,
                new[] { Line(S.Initiator, "Oi, the {INCIDENT_TYPE} broke our best trap! Do you know how long that took to build?!", 0) },
                "faction_goblin", ConversationPredicates.RequireRecentIncident));
            templates.Add(CreateFaction("hostility_griev_goblin_05", ConversationTopicTag.HostilityGrievance,
                new[] { Line(S.Initiator, "Boss is furious about the {INCIDENT_TYPE}. And when boss is furious, everybody suffers.", 0) },
                "faction_goblin", ConversationPredicates.RequireRecentIncident));

            // DEMON
            templates.Add(CreateFaction("hostility_griev_demon_01", ConversationTopicTag.HostilityGrievance,
                new[] { Line(S.Initiator, "Your pitiful {INCIDENT_TYPE} has consequences you cannot fathom, insect.", 0) },
                "faction_demon", ConversationPredicates.RequireRecentIncident));
            templates.Add(CreateFaction("hostility_griev_demon_02", ConversationTopicTag.HostilityGrievance,
                new[] { Line(S.Initiator, "The {INCIDENT_TYPE} was... noticed. We do not forget.", 0) },
                "faction_demon", ConversationPredicates.RequireRecentIncident));
            templates.Add(CreateFaction("hostility_griev_demon_03", ConversationTopicTag.HostilityGrievance,
                new[] { Line(S.Initiator, "Your {INCIDENT_TYPE} disrupted the ritual. You will pay in ways you cannot imagine.", 0) },
                "faction_demon", ConversationPredicates.RequireRecentIncident));
            templates.Add(CreateFaction("hostility_griev_demon_04", ConversationTopicTag.HostilityGrievance,
                new[] { Line(S.Initiator, "Mortals commit {INCIDENT_TYPE} and expect no retribution? How quaint.", 0) },
                "faction_demon", ConversationPredicates.RequireRecentIncident));
            templates.Add(CreateFaction("hostility_griev_demon_05", ConversationTopicTag.HostilityGrievance,
                new[] { Line(S.Initiator, "Each {INCIDENT_TYPE} adds fuel to the fire. You would not like us at full burn.", 0) },
                "faction_demon", ConversationPredicates.RequireRecentIncident));

            // SNAKE
            templates.Add(CreateFaction("hostility_griev_snake_01", ConversationTopicTag.HostilityGrievance,
                new[] { Line(S.Initiator, "Your {INCIDENT_TYPE} poissonss the peace between uss.", 0) },
                "faction_snake", ConversationPredicates.RequireRecentIncident));
            templates.Add(CreateFaction("hostility_griev_snake_02", ConversationTopicTag.HostilityGrievance,
                new[] { Line(S.Initiator, "The Nest demandsss anssswer for the {INCIDENT_TYPE}.", 0) },
                "faction_snake", ConversationPredicates.RequireRecentIncident));
            templates.Add(CreateFaction("hostility_griev_snake_03", ConversationTopicTag.HostilityGrievance,
                new[] { Line(S.Initiator, "We have catalogued each {INCIDENT_TYPE}. The tally growsss long.", 0) },
                "faction_snake", ConversationPredicates.RequireRecentIncident));
            templates.Add(CreateFaction("hostility_griev_snake_04", ConversationTopicTag.HostilityGrievance,
                new[] { Line(S.Initiator, "Your {INCIDENT_TYPE} dessstroyed eggsss. EGGSSS. Thisss isss unforgivable.", 0) },
                "faction_snake", ConversationPredicates.RequireRecentIncident));
            templates.Add(CreateFaction("hostility_griev_snake_05", ConversationTopicTag.HostilityGrievance,
                new[] { Line(S.Initiator, "The eldersss hissss with rage over the {INCIDENT_TYPE}. Rightly ssso.", 0) },
                "faction_snake", ConversationPredicates.RequireRecentIncident));

            // --- Escalation (Volatile): weapons drawn ---
            // INKGUARD
            templates.Add(CreateFaction("hostility_esc_inkguard_01", ConversationTopicTag.HostilityEscalation,
                new[] { Line(S.Initiator, "Draw steel, brothers. The Codex sanctions righteous force.", 0) },
                "faction_inkguard", ConversationPredicates.RequireTensionVolatile));
            templates.Add(CreateFaction("hostility_esc_inkguard_02", ConversationTopicTag.HostilityEscalation,
                new[] { Line(S.Initiator, "Form ranks! The {FACTION_OTHER} have pushed us too far!", 0) },
                "faction_inkguard", ConversationPredicates.RequireTensionVolatile));
            templates.Add(CreateFaction("hostility_esc_inkguard_03", ConversationTopicTag.HostilityEscalation,
                new[] { Line(S.Initiator, "The chapter has authorized the use of force in {DISTRICT}. Ready yourselves.", 0) },
                "faction_inkguard", ConversationPredicates.RequireTensionVolatile));
            templates.Add(CreateFaction("hostility_esc_inkguard_04", ConversationTopicTag.HostilityEscalation,
                new[] { Line(S.Initiator, "No more words. Blades out. The {FACTION_OTHER} understand nothing else.", 0) },
                "faction_inkguard", ConversationPredicates.RequireTensionVolatile));
            templates.Add(CreateFaction("hostility_esc_inkguard_05", ConversationTopicTag.HostilityEscalation,
                new[] { Line(S.Initiator, "By the Codex and by steel, we will hold {DISTRICT}!", 0) },
                "faction_inkguard", ConversationPredicates.RequireTensionVolatile));

            // INKBOUND
            templates.Add(CreateFaction("hostility_esc_inkbound_01", ConversationTopicTag.HostilityEscalation,
                new[] { Line(S.Initiator, "Seal the archives. Prepare the binding wards. This is no longer a dispute of words.", 0) },
                "faction_inkbound", ConversationPredicates.RequireTensionVolatile));
            templates.Add(CreateFaction("hostility_esc_inkbound_02", ConversationTopicTag.HostilityEscalation,
                new[] { Line(S.Initiator, "Activate the defensive glyphs. The {FACTION_OTHER} have forced our hand.", 0) },
                "faction_inkbound", ConversationPredicates.RequireTensionVolatile));
            templates.Add(CreateFaction("hostility_esc_inkbound_03", ConversationTopicTag.HostilityEscalation,
                new[] { Line(S.Initiator, "The ink runs red today. Prepare the war ledgers.", 0) },
                "faction_inkbound", ConversationPredicates.RequireTensionVolatile));
            templates.Add(CreateFaction("hostility_esc_inkbound_04", ConversationTopicTag.HostilityEscalation,
                new[] { Line(S.Initiator, "Knowledge is power, and power will be wielded. Ready the wards.", 0) },
                "faction_inkbound", ConversationPredicates.RequireTensionVolatile));
            templates.Add(CreateFaction("hostility_esc_inkbound_05", ConversationTopicTag.HostilityEscalation,
                new[] { Line(S.Initiator, "The binding circle is drawn. When they cross it, they will learn.", 0) },
                "faction_inkbound", ConversationPredicates.RequireTensionVolatile));

            // SKELETON
            templates.Add(CreateFaction("hostility_esc_skeleton_01", ConversationTopicTag.HostilityEscalation,
                new[] { Line(S.Initiator, "Rise, brothers. The living need a reminder of what death looks like.", 0) },
                "faction_skeleton", ConversationPredicates.RequireTensionVolatile));
            templates.Add(CreateFaction("hostility_esc_skeleton_02", ConversationTopicTag.HostilityEscalation,
                new[] { Line(S.Initiator, "Assemble the boneguard. Tonight we march.", 0) },
                "faction_skeleton", ConversationPredicates.RequireTensionVolatile));
            templates.Add(CreateFaction("hostility_esc_skeleton_03", ConversationTopicTag.HostilityEscalation,
                new[] { Line(S.Initiator, "Sharpen your bones, brothers. The {FACTION_OTHER} come for us.", 0) },
                "faction_skeleton", ConversationPredicates.RequireTensionVolatile));
            templates.Add(CreateFaction("hostility_esc_skeleton_04", ConversationTopicTag.HostilityEscalation,
                new[] { Line(S.Initiator, "The crypt doors open. Let them see what sleeps within.", 0) },
                "faction_skeleton", ConversationPredicates.RequireTensionVolatile));
            templates.Add(CreateFaction("hostility_esc_skeleton_05", ConversationTopicTag.HostilityEscalation,
                new[] { Line(S.Initiator, "Form the wall. We do not tire. We do not flee. We endure.", 0) },
                "faction_skeleton", ConversationPredicates.RequireTensionVolatile));

            // GHOST
            templates.Add(CreateFaction("hostility_esc_ghost_01", ConversationTopicTag.HostilityEscalation,
                new[] { Line(S.Initiator, "The veil thins. We are coming through. All of us.", 0) },
                "faction_ghost", ConversationPredicates.RequireTensionVolatile));
            templates.Add(CreateFaction("hostility_esc_ghost_02", ConversationTopicTag.HostilityEscalation,
                new[] { Line(S.Initiator, "Open the gates between worlds. The {FACTION_OTHER} must see our full numbers.", 0) },
                "faction_ghost", ConversationPredicates.RequireTensionVolatile));
            templates.Add(CreateFaction("hostility_esc_ghost_03", ConversationTopicTag.HostilityEscalation,
                new[] { Line(S.Initiator, "Let the temperature drop. Let them feel the cold of the grave.", 0) },
                "faction_ghost", ConversationPredicates.RequireTensionVolatile));
            templates.Add(CreateFaction("hostility_esc_ghost_04", ConversationTopicTag.HostilityEscalation,
                new[] { Line(S.Initiator, "Manifest fully, brothers. No more hiding in shadow.", 0) },
                "faction_ghost", ConversationPredicates.RequireTensionVolatile));
            templates.Add(CreateFaction("hostility_esc_ghost_05", ConversationTopicTag.HostilityEscalation,
                new[] { Line(S.Initiator, "The wailing begins tonight. None in {DISTRICT} will sleep.", 0) },
                "faction_ghost", ConversationPredicates.RequireTensionVolatile));

            // GOBLIN
            templates.Add(CreateFaction("hostility_esc_goblin_01", ConversationTopicTag.HostilityEscalation,
                new[] { Line(S.Initiator, "That's IT! Get the stabbies! Get the burny things! We're done talkin'!", 0) },
                "faction_goblin", ConversationPredicates.RequireTensionVolatile));
            templates.Add(CreateFaction("hostility_esc_goblin_02", ConversationTopicTag.HostilityEscalation,
                new[] { Line(S.Initiator, "Light the signal fire! All goblins to battle stations! This is NOT a drill!", 0) },
                "faction_goblin", ConversationPredicates.RequireTensionVolatile));
            templates.Add(CreateFaction("hostility_esc_goblin_03", ConversationTopicTag.HostilityEscalation,
                new[] { Line(S.Initiator, "Bring out the catapult! The rusty one! The one that mostly works!", 0) },
                "faction_goblin", ConversationPredicates.RequireTensionVolatile));
            templates.Add(CreateFaction("hostility_esc_goblin_04", ConversationTopicTag.HostilityEscalation,
                new[] { Line(S.Initiator, "Arm the traps! All of 'em! Even the ones we forgot where we put!", 0) },
                "faction_goblin", ConversationPredicates.RequireTensionVolatile));
            templates.Add(CreateFaction("hostility_esc_goblin_05", ConversationTopicTag.HostilityEscalation,
                new[] { Line(S.Initiator, "Everyone grab a weapon! If you can't find one, grab a rock! Rocks is good too!", 0) },
                "faction_goblin", ConversationPredicates.RequireTensionVolatile));

            // DEMON
            templates.Add(CreateFaction("hostility_esc_demon_01", ConversationTopicTag.HostilityEscalation,
                new[] { Line(S.Initiator, "ENOUGH. Unleash the flames. Let them burn.", 0) },
                "faction_demon", ConversationPredicates.RequireTensionVolatile));
            templates.Add(CreateFaction("hostility_esc_demon_02", ConversationTopicTag.HostilityEscalation,
                new[] { Line(S.Initiator, "Summon the lesser fiends. It is time to teach the {FACTION_OTHER} true fear.", 0) },
                "faction_demon", ConversationPredicates.RequireTensionVolatile));
            templates.Add(CreateFaction("hostility_esc_demon_03", ConversationTopicTag.HostilityEscalation,
                new[] { Line(S.Initiator, "The pact is invoked. Hellfire answers our call.", 0) },
                "faction_demon", ConversationPredicates.RequireTensionVolatile));
            templates.Add(CreateFaction("hostility_esc_demon_04", ConversationTopicTag.HostilityEscalation,
                new[] { Line(S.Initiator, "Open the ash circles. Let the {FACTION_OTHER} see what waits within.", 0) },
                "faction_demon", ConversationPredicates.RequireTensionVolatile));
            templates.Add(CreateFaction("hostility_esc_demon_05", ConversationTopicTag.HostilityEscalation,
                new[] { Line(S.Initiator, "The time for restraint has passed. Prepare the infernal rites.", 0) },
                "faction_demon", ConversationPredicates.RequireTensionVolatile));

            // SNAKE
            templates.Add(CreateFaction("hostility_esc_snake_01", ConversationTopicTag.HostilityEscalation,
                new[] { Line(S.Initiator, "Bare your fangsss! The time for wordsss isss over!", 0) },
                "faction_snake", ConversationPredicates.RequireTensionVolatile));
            templates.Add(CreateFaction("hostility_esc_snake_02", ConversationTopicTag.HostilityEscalation,
                new[] { Line(S.Initiator, "Prepare the venom reservoirsss. Coat every blade.", 0) },
                "faction_snake", ConversationPredicates.RequireTensionVolatile));
            templates.Add(CreateFaction("hostility_esc_snake_03", ConversationTopicTag.HostilityEscalation,
                new[] { Line(S.Initiator, "The Nest mobilizesss. Every tunnel, every burrow, poissoned and trapped.", 0) },
                "faction_snake", ConversationPredicates.RequireTensionVolatile));
            templates.Add(CreateFaction("hostility_esc_snake_04", ConversationTopicTag.HostilityEscalation,
                new[] { Line(S.Initiator, "Sssurround them from the grasss. They will not sssee usss coming.", 0) },
                "faction_snake", ConversationPredicates.RequireTensionVolatile));
            templates.Add(CreateFaction("hostility_esc_snake_05", ConversationTopicTag.HostilityEscalation,
                new[] { Line(S.Initiator, "The ssseason of sssilence isss over. Now comesss the ssseason of fang.", 0) },
                "faction_snake", ConversationPredicates.RequireTensionVolatile));

            // --- BrawlStart (Explosive): combat cries ---
            // INKGUARD
            templates.Add(CreateFaction("hostility_brawl_inkguard_01", ConversationTopicTag.HostilityBrawlStart,
                new[] { Line(S.Initiator, "For the Codex! For the Inkguard!", 0) },
                "faction_inkguard", ConversationPredicates.RequireTensionExplosive));
            templates.Add(CreateFaction("hostility_brawl_inkguard_02", ConversationTopicTag.HostilityBrawlStart,
                new[] { Line(S.Initiator, "Hold the line! Not one step back!", 0) },
                "faction_inkguard", ConversationPredicates.RequireTensionExplosive));
            templates.Add(CreateFaction("hostility_brawl_inkguard_03", ConversationTopicTag.HostilityBrawlStart,
                new[] { Line(S.Initiator, "Chapter, advance! Drive them from {DISTRICT}!", 0) },
                "faction_inkguard", ConversationPredicates.RequireTensionExplosive));
            templates.Add(CreateFaction("hostility_brawl_inkguard_04", ConversationTopicTag.HostilityBrawlStart,
                new[] { Line(S.Initiator, "Steel and scripture! Show no quarter!", 0) },
                "faction_inkguard", ConversationPredicates.RequireTensionExplosive));
            templates.Add(CreateFaction("hostility_brawl_inkguard_05", ConversationTopicTag.HostilityBrawlStart,
                new[] { Line(S.Initiator, "The Codex wills it! CHARGE!", 0) },
                "faction_inkguard", ConversationPredicates.RequireTensionExplosive));

            // INKBOUND
            templates.Add(CreateFaction("hostility_brawl_inkbound_01", ConversationTopicTag.HostilityBrawlStart,
                new[] { Line(S.Initiator, "The binding holds! Strike now, while the wards protect us!", 0) },
                "faction_inkbound", ConversationPredicates.RequireTensionExplosive));
            templates.Add(CreateFaction("hostility_brawl_inkbound_02", ConversationTopicTag.HostilityBrawlStart,
                new[] { Line(S.Initiator, "By the weight of all recorded knowledge, we strike!", 0) },
                "faction_inkbound", ConversationPredicates.RequireTensionExplosive));
            templates.Add(CreateFaction("hostility_brawl_inkbound_03", ConversationTopicTag.HostilityBrawlStart,
                new[] { Line(S.Initiator, "Activate the war glyphs! Let history record our victory!", 0) },
                "faction_inkbound", ConversationPredicates.RequireTensionExplosive));
            templates.Add(CreateFaction("hostility_brawl_inkbound_04", ConversationTopicTag.HostilityBrawlStart,
                new[] { Line(S.Initiator, "The quill is mightier! But today the sword suffices!", 0) },
                "faction_inkbound", ConversationPredicates.RequireTensionExplosive));
            templates.Add(CreateFaction("hostility_brawl_inkbound_05", ConversationTopicTag.HostilityBrawlStart,
                new[] { Line(S.Initiator, "For the archives! For the Inkbound!", 0) },
                "faction_inkbound", ConversationPredicates.RequireTensionExplosive));

            // SKELETON
            templates.Add(CreateFaction("hostility_brawl_skeleton_01", ConversationTopicTag.HostilityBrawlStart,
                new[] { Line(S.Initiator, "DEATH COMES! ...Again!", 0) },
                "faction_skeleton", ConversationPredicates.RequireTensionExplosive));
            templates.Add(CreateFaction("hostility_brawl_skeleton_02", ConversationTopicTag.HostilityBrawlStart,
                new[] { Line(S.Initiator, "Rattle their bones! Break their spirits!", 0) },
                "faction_skeleton", ConversationPredicates.RequireTensionExplosive));
            templates.Add(CreateFaction("hostility_brawl_skeleton_03", ConversationTopicTag.HostilityBrawlStart,
                new[] { Line(S.Initiator, "The undead march! The grave opens for the {FACTION_OTHER}!", 0) },
                "faction_skeleton", ConversationPredicates.RequireTensionExplosive));
            templates.Add(CreateFaction("hostility_brawl_skeleton_04", ConversationTopicTag.HostilityBrawlStart,
                new[] { Line(S.Initiator, "No rest for the wicked! And we are very, very wicked!", 0) },
                "faction_skeleton", ConversationPredicates.RequireTensionExplosive));
            templates.Add(CreateFaction("hostility_brawl_skeleton_05", ConversationTopicTag.HostilityBrawlStart,
                new[] { Line(S.Initiator, "Bones against flesh! We know who wins!", 0) },
                "faction_skeleton", ConversationPredicates.RequireTensionExplosive));

            // GHOST
            templates.Add(CreateFaction("hostility_brawl_ghost_01", ConversationTopicTag.HostilityBrawlStart,
                new[] { Line(S.Initiator, "THE VEIL OPENS! COME, BROTHERS! COME!", 0) },
                "faction_ghost", ConversationPredicates.RequireTensionExplosive));
            templates.Add(CreateFaction("hostility_brawl_ghost_02", ConversationTopicTag.HostilityBrawlStart,
                new[] { Line(S.Initiator, "Haunt them! Drive them mad with memory and fear!", 0) },
                "faction_ghost", ConversationPredicates.RequireTensionExplosive));
            templates.Add(CreateFaction("hostility_brawl_ghost_03", ConversationTopicTag.HostilityBrawlStart,
                new[] { Line(S.Initiator, "We are the chorus of the dead! HEAR US WAIL!", 0) },
                "faction_ghost", ConversationPredicates.RequireTensionExplosive));
            templates.Add(CreateFaction("hostility_brawl_ghost_04", ConversationTopicTag.HostilityBrawlStart,
                new[] { Line(S.Initiator, "Through wall and ward we pass! Nothing stops the dead!", 0) },
                "faction_ghost", ConversationPredicates.RequireTensionExplosive));
            templates.Add(CreateFaction("hostility_brawl_ghost_05", ConversationTopicTag.HostilityBrawlStart,
                new[] { Line(S.Initiator, "Remember us, {FACTION_OTHER}! For we will haunt you forever!", 0) },
                "faction_ghost", ConversationPredicates.RequireTensionExplosive));

            // GOBLIN
            templates.Add(CreateFaction("hostility_brawl_goblin_01", ConversationTopicTag.HostilityBrawlStart,
                new[] { Line(S.Initiator, "WAAAGH! Get 'em, boys!", 0) },
                "faction_goblin", ConversationPredicates.RequireTensionExplosive));
            templates.Add(CreateFaction("hostility_brawl_goblin_02", ConversationTopicTag.HostilityBrawlStart,
                new[] { Line(S.Initiator, "Stab stab stab! For the hoard!", 0) },
                "faction_goblin", ConversationPredicates.RequireTensionExplosive));
            templates.Add(CreateFaction("hostility_brawl_goblin_03", ConversationTopicTag.HostilityBrawlStart,
                new[] { Line(S.Initiator, "CHARGE! And try not to trip this time!", 0) },
                "faction_goblin", ConversationPredicates.RequireTensionExplosive));
            templates.Add(CreateFaction("hostility_brawl_goblin_04", ConversationTopicTag.HostilityBrawlStart,
                new[] { Line(S.Initiator, "FOR SHINIES AND GLORY! Mostly shinies!", 0) },
                "faction_goblin", ConversationPredicates.RequireTensionExplosive));
            templates.Add(CreateFaction("hostility_brawl_goblin_05", ConversationTopicTag.HostilityBrawlStart,
                new[] { Line(S.Initiator, "GOGOGO! Hit 'em with everything! Even the kitchen sink! Especially the kitchen sink!", 0) },
                "faction_goblin", ConversationPredicates.RequireTensionExplosive));

            // DEMON
            templates.Add(CreateFaction("hostility_brawl_demon_01", ConversationTopicTag.HostilityBrawlStart,
                new[] { Line(S.Initiator, "BURN! Let the world know our wrath!", 0) },
                "faction_demon", ConversationPredicates.RequireTensionExplosive));
            templates.Add(CreateFaction("hostility_brawl_demon_02", ConversationTopicTag.HostilityBrawlStart,
                new[] { Line(S.Initiator, "Kneel or be broken! There is no third choice!", 0) },
                "faction_demon", ConversationPredicates.RequireTensionExplosive));
            templates.Add(CreateFaction("hostility_brawl_demon_03", ConversationTopicTag.HostilityBrawlStart,
                new[] { Line(S.Initiator, "THE INFERNO DESCENDS! COWER, MORTALS!", 0) },
                "faction_demon", ConversationPredicates.RequireTensionExplosive));
            templates.Add(CreateFaction("hostility_brawl_demon_04", ConversationTopicTag.HostilityBrawlStart,
                new[] { Line(S.Initiator, "By ash and ember, we annihilate! For the Burning Throne!", 0) },
                "faction_demon", ConversationPredicates.RequireTensionExplosive));
            templates.Add(CreateFaction("hostility_brawl_demon_05", ConversationTopicTag.HostilityBrawlStart,
                new[] { Line(S.Initiator, "SCREAM for us, {FACTION_OTHER}! Your agony fuels our fire!", 0) },
                "faction_demon", ConversationPredicates.RequireTensionExplosive));

            // SNAKE
            templates.Add(CreateFaction("hostility_brawl_snake_01", ConversationTopicTag.HostilityBrawlStart,
                new[] { Line(S.Initiator, "SSSSTRIKE! Leave none ssstanding!", 0) },
                "faction_snake", ConversationPredicates.RequireTensionExplosive));
            templates.Add(CreateFaction("hostility_brawl_snake_02", ConversationTopicTag.HostilityBrawlStart,
                new[] { Line(S.Initiator, "Fang and venom! For the Nest!", 0) },
                "faction_snake", ConversationPredicates.RequireTensionExplosive));
            templates.Add(CreateFaction("hostility_brawl_snake_03", ConversationTopicTag.HostilityBrawlStart,
                new[] { Line(S.Initiator, "The grasss movesss! We are everywhere! SSSTRIKE!", 0) },
                "faction_snake", ConversationPredicates.RequireTensionExplosive));
            templates.Add(CreateFaction("hostility_brawl_snake_04", ConversationTopicTag.HostilityBrawlStart,
                new[] { Line(S.Initiator, "Consssstrict! Crussssh! No essscape!", 0) },
                "faction_snake", ConversationPredicates.RequireTensionExplosive));
            templates.Add(CreateFaction("hostility_brawl_snake_05", ConversationTopicTag.HostilityBrawlStart,
                new[] { Line(S.Initiator, "The hunt beginsss! Devour the {FACTION_OTHER}!", 0) },
                "faction_snake", ConversationPredicates.RequireTensionExplosive));

            // --- DeEscalation: stand down ---
            // INKGUARD
            templates.Add(CreateFaction("hostility_deesc_inkguard_01", ConversationTopicTag.HostilityDeEscalation,
                new[] { Line(S.Initiator, "Stand down. The chapter calls for restraint... for now.", 0) },
                "faction_inkguard", ConversationPredicates.RequireDeEscalation));
            templates.Add(CreateFaction("hostility_deesc_inkguard_02", ConversationTopicTag.HostilityDeEscalation,
                new[] { Line(S.Initiator, "Sheathe your blades. The Codex demands we seek peace first.", 0) },
                "faction_inkguard", ConversationPredicates.RequireDeEscalation));
            templates.Add(CreateFaction("hostility_deesc_inkguard_03", ConversationTopicTag.HostilityDeEscalation,
                new[] { Line(S.Initiator, "Fall back to defensive positions. We have made our point.", 0) },
                "faction_inkguard", ConversationPredicates.RequireDeEscalation));
            templates.Add(CreateFaction("hostility_deesc_inkguard_04", ConversationTopicTag.HostilityDeEscalation,
                new[] { Line(S.Initiator, "Enough blood has been spilled. The Codex values mercy too.", 0) },
                "faction_inkguard", ConversationPredicates.RequireDeEscalation));
            templates.Add(CreateFaction("hostility_deesc_inkguard_05", ConversationTopicTag.HostilityDeEscalation,
                new[] { Line(S.Initiator, "Lower weapons. We watch, but we wait.", 0) },
                "faction_inkguard", ConversationPredicates.RequireDeEscalation));

            // INKBOUND
            templates.Add(CreateFaction("hostility_deesc_inkbound_01", ConversationTopicTag.HostilityDeEscalation,
                new[] { Line(S.Initiator, "Deactivate the wards. We shall attempt diplomacy once more.", 0) },
                "faction_inkbound", ConversationPredicates.RequireDeEscalation));
            templates.Add(CreateFaction("hostility_deesc_inkbound_02", ConversationTopicTag.HostilityDeEscalation,
                new[] { Line(S.Initiator, "The records show a path to peace. We should take it.", 0) },
                "faction_inkbound", ConversationPredicates.RequireDeEscalation));
            templates.Add(CreateFaction("hostility_deesc_inkbound_03", ConversationTopicTag.HostilityDeEscalation,
                new[] { Line(S.Initiator, "War is expensive in coin and knowledge. Cease fire.", 0) },
                "faction_inkbound", ConversationPredicates.RequireDeEscalation));
            templates.Add(CreateFaction("hostility_deesc_inkbound_04", ConversationTopicTag.HostilityDeEscalation,
                new[] { Line(S.Initiator, "Enough. Open the negotiation ledger. There is a better way.", 0) },
                "faction_inkbound", ConversationPredicates.RequireDeEscalation));
            templates.Add(CreateFaction("hostility_deesc_inkbound_05", ConversationTopicTag.HostilityDeEscalation,
                new[] { Line(S.Initiator, "Stand down. The archives teach that wars end. Best to end them early.", 0) },
                "faction_inkbound", ConversationPredicates.RequireDeEscalation));

            // SKELETON
            templates.Add(CreateFaction("hostility_deesc_skeleton_01", ConversationTopicTag.HostilityDeEscalation,
                new[] { Line(S.Initiator, "Hold. Even the dead tire of killing. Sometimes.", 0) },
                "faction_skeleton", ConversationPredicates.RequireDeEscalation));
            templates.Add(CreateFaction("hostility_deesc_skeleton_02", ConversationTopicTag.HostilityDeEscalation,
                new[] { Line(S.Initiator, "Return to the crypts. We have shown our strength.", 0) },
                "faction_skeleton", ConversationPredicates.RequireDeEscalation));
            templates.Add(CreateFaction("hostility_deesc_skeleton_03", ConversationTopicTag.HostilityDeEscalation,
                new[] { Line(S.Initiator, "Enough. Let the dust settle. We have eternity to fight.", 0) },
                "faction_skeleton", ConversationPredicates.RequireDeEscalation));
            templates.Add(CreateFaction("hostility_deesc_skeleton_04", ConversationTopicTag.HostilityDeEscalation,
                new[] { Line(S.Initiator, "Fall back. We are patient. We can wait for a better moment.", 0) },
                "faction_skeleton", ConversationPredicates.RequireDeEscalation));
            templates.Add(CreateFaction("hostility_deesc_skeleton_05", ConversationTopicTag.HostilityDeEscalation,
                new[] { Line(S.Initiator, "Cease. There is no honor in grinding bone against stone endlessly.", 0) },
                "faction_skeleton", ConversationPredicates.RequireDeEscalation));

            // GHOST
            templates.Add(CreateFaction("hostility_deesc_ghost_01", ConversationTopicTag.HostilityDeEscalation,
                new[] { Line(S.Initiator, "Withdraw behind the veil. We have haunted them enough.", 0) },
                "faction_ghost", ConversationPredicates.RequireDeEscalation));
            templates.Add(CreateFaction("hostility_deesc_ghost_02", ConversationTopicTag.HostilityDeEscalation,
                new[] { Line(S.Initiator, "The echoes fade. Let silence speak now.", 0) },
                "faction_ghost", ConversationPredicates.RequireDeEscalation));
            templates.Add(CreateFaction("hostility_deesc_ghost_03", ConversationTopicTag.HostilityDeEscalation,
                new[] { Line(S.Initiator, "Enough manifestation. Conserve your essence.", 0) },
                "faction_ghost", ConversationPredicates.RequireDeEscalation));
            templates.Add(CreateFaction("hostility_deesc_ghost_04", ConversationTopicTag.HostilityDeEscalation,
                new[] { Line(S.Initiator, "The living fear what they cannot see. Our absence speaks loudest.", 0) },
                "faction_ghost", ConversationPredicates.RequireDeEscalation));
            templates.Add(CreateFaction("hostility_deesc_ghost_05", ConversationTopicTag.HostilityDeEscalation,
                new[] { Line(S.Initiator, "Rest now. There will be other hauntings.", 0) },
                "faction_ghost", ConversationPredicates.RequireDeEscalation));

            // GOBLIN
            templates.Add(CreateFaction("hostility_deesc_goblin_01", ConversationTopicTag.HostilityDeEscalation,
                new[] { Line(S.Initiator, "Aight aight, fall back! Save yer shanks for later!", 0) },
                "faction_goblin", ConversationPredicates.RequireDeEscalation));
            templates.Add(CreateFaction("hostility_deesc_goblin_02", ConversationTopicTag.HostilityDeEscalation,
                new[] { Line(S.Initiator, "Retreat! But grab the shinies on the way out!", 0) },
                "faction_goblin", ConversationPredicates.RequireDeEscalation));
            templates.Add(CreateFaction("hostility_deesc_goblin_03", ConversationTopicTag.HostilityDeEscalation,
                new[] { Line(S.Initiator, "We'll get 'em next time, boys. When they ain't expectin' it.", 0) },
                "faction_goblin", ConversationPredicates.RequireDeEscalation));
            templates.Add(CreateFaction("hostility_deesc_goblin_04", ConversationTopicTag.HostilityDeEscalation,
                new[] { Line(S.Initiator, "Stop fighting! I mean it! Boss says ceasefire! Whatever that means!", 0) },
                "faction_goblin", ConversationPredicates.RequireDeEscalation));
            templates.Add(CreateFaction("hostility_deesc_goblin_05", ConversationTopicTag.HostilityDeEscalation,
                new[] { Line(S.Initiator, "Back to the warrens! We need a new plan. And more grog.", 0) },
                "faction_goblin", ConversationPredicates.RequireDeEscalation));

            // DEMON
            templates.Add(CreateFaction("hostility_deesc_demon_01", ConversationTopicTag.HostilityDeEscalation,
                new[] { Line(S.Initiator, "Cease. They are beneath our continued attention.", 0) },
                "faction_demon", ConversationPredicates.RequireDeEscalation));
            templates.Add(CreateFaction("hostility_deesc_demon_02", ConversationTopicTag.HostilityDeEscalation,
                new[] { Line(S.Initiator, "Withdraw. Let them rebuild. Then we burn it again.", 0) },
                "faction_demon", ConversationPredicates.RequireDeEscalation));
            templates.Add(CreateFaction("hostility_deesc_demon_03", ConversationTopicTag.HostilityDeEscalation,
                new[] { Line(S.Initiator, "The fire dims. But embers remember.", 0) },
                "faction_demon", ConversationPredicates.RequireDeEscalation));
            templates.Add(CreateFaction("hostility_deesc_demon_04", ConversationTopicTag.HostilityDeEscalation,
                new[] { Line(S.Initiator, "Recall the fiends. We have made our displeasure... clear.", 0) },
                "faction_demon", ConversationPredicates.RequireDeEscalation));
            templates.Add(CreateFaction("hostility_deesc_demon_05", ConversationTopicTag.HostilityDeEscalation,
                new[] { Line(S.Initiator, "Enough for now. Let them stew in the ashes of their pride.", 0) },
                "faction_demon", ConversationPredicates.RequireDeEscalation));

            // SNAKE
            templates.Add(CreateFaction("hostility_deesc_snake_01", ConversationTopicTag.HostilityDeEscalation,
                new[] { Line(S.Initiator, "Coil back. We sshall wait for a better moment to ssstrike.", 0) },
                "faction_snake", ConversationPredicates.RequireDeEscalation));
            templates.Add(CreateFaction("hostility_deesc_snake_02", ConversationTopicTag.HostilityDeEscalation,
                new[] { Line(S.Initiator, "Retreat to the burrowsss. Patience isss our greatessst weapon.", 0) },
                "faction_snake", ConversationPredicates.RequireDeEscalation));
            templates.Add(CreateFaction("hostility_deesc_snake_03", ConversationTopicTag.HostilityDeEscalation,
                new[] { Line(S.Initiator, "The venom savesss for when it mattersss mossst. Withdraw.", 0) },
                "faction_snake", ConversationPredicates.RequireDeEscalation));
            templates.Add(CreateFaction("hostility_deesc_snake_04", ConversationTopicTag.HostilityDeEscalation,
                new[] { Line(S.Initiator, "Sssilence now. We watch from the grasss. They will forget we are here.", 0) },
                "faction_snake", ConversationPredicates.RequireDeEscalation));
            templates.Add(CreateFaction("hostility_deesc_snake_05", ConversationTopicTag.HostilityDeEscalation,
                new[] { Line(S.Initiator, "Pull back. A ssserpent that overextendsss getsss ssstomped.", 0) },
                "faction_snake", ConversationPredicates.RequireDeEscalation));

            // --- Aftermath: mourning, vows ---
            // INKGUARD
            templates.Add(CreateFaction("hostility_after_inkguard_01", ConversationTopicTag.HostilityAftermath,
                new[] { Line(S.Initiator, "Record their names. Every knight who fell today will be remembered in the Codex.", 0),
                        Line(S.Responder, "It shall be done. May their sacrifice guide us.", 1) },
                "faction_inkguard", ConversationPredicates.RequireAftermath));
            templates.Add(CreateFaction("hostility_after_inkguard_02", ConversationTopicTag.HostilityAftermath,
                new[] { Line(S.Initiator, "The chapter lost good soldiers. This will not go unanswered.", 0) },
                "faction_inkguard", ConversationPredicates.RequireAftermath));
            templates.Add(CreateFaction("hostility_after_inkguard_03", ConversationTopicTag.HostilityAftermath,
                new[] { Line(S.Initiator, "Clean your blades and tend the wounded. The Codex asks no more today.", 0) },
                "faction_inkguard", ConversationPredicates.RequireAftermath));
            templates.Add(CreateFaction("hostility_after_inkguard_04", ConversationTopicTag.HostilityAftermath,
                new[] { Line(S.Initiator, "We held. But at great cost. Remember what they took from us.", 0) },
                "faction_inkguard", ConversationPredicates.RequireAftermath));
            templates.Add(CreateFaction("hostility_after_inkguard_05", ConversationTopicTag.HostilityAftermath,
                new[] { Line(S.Initiator, "Light a candle for the fallen. The Codex remembers.", 0) },
                "faction_inkguard", ConversationPredicates.RequireAftermath));

            // INKBOUND
            templates.Add(CreateFaction("hostility_after_inkbound_01", ConversationTopicTag.HostilityAftermath,
                new[] { Line(S.Initiator, "We lost three scribes. Three lifetimes of knowledge, gone.", 0),
                        Line(S.Responder, "Their works endure in the archive. They are not truly lost.", 1) },
                "faction_inkbound", ConversationPredicates.RequireAftermath));
            templates.Add(CreateFaction("hostility_after_inkbound_02", ConversationTopicTag.HostilityAftermath,
                new[] { Line(S.Initiator, "Catalogue the damage. Every broken ward, every burned page.", 0) },
                "faction_inkbound", ConversationPredicates.RequireAftermath));
            templates.Add(CreateFaction("hostility_after_inkbound_03", ConversationTopicTag.HostilityAftermath,
                new[] { Line(S.Initiator, "The cost of this conflict will be measured in ink and blood. Record it all.", 0) },
                "faction_inkbound", ConversationPredicates.RequireAftermath));
            templates.Add(CreateFaction("hostility_after_inkbound_04", ConversationTopicTag.HostilityAftermath,
                new[] { Line(S.Initiator, "Restore the damaged archives first. Knowledge must be preserved.", 0) },
                "faction_inkbound", ConversationPredicates.RequireAftermath));
            templates.Add(CreateFaction("hostility_after_inkbound_05", ConversationTopicTag.HostilityAftermath,
                new[] { Line(S.Initiator, "Write this day into the war chronicles. Let future scribes learn from our losses.", 0) },
                "faction_inkbound", ConversationPredicates.RequireAftermath));

            // SKELETON
            templates.Add(CreateFaction("hostility_after_skeleton_01", ConversationTopicTag.HostilityAftermath,
                new[] { Line(S.Initiator, "Gather the fallen. Ours and theirs. All are welcome in death.", 0) },
                "faction_skeleton", ConversationPredicates.RequireAftermath));
            templates.Add(CreateFaction("hostility_after_skeleton_02", ConversationTopicTag.HostilityAftermath,
                new[] { Line(S.Initiator, "The dust settles. We remain. We always remain.", 0) },
                "faction_skeleton", ConversationPredicates.RequireAftermath));
            templates.Add(CreateFaction("hostility_after_skeleton_03", ConversationTopicTag.HostilityAftermath,
                new[] { Line(S.Initiator, "Rebuild the bone wall. Let it be thicker this time.", 0) },
                "faction_skeleton", ConversationPredicates.RequireAftermath));
            templates.Add(CreateFaction("hostility_after_skeleton_04", ConversationTopicTag.HostilityAftermath,
                new[] { Line(S.Initiator, "Some of our brothers fell to pieces. Help me reassemble them.", 0) },
                "faction_skeleton", ConversationPredicates.RequireAftermath));
            templates.Add(CreateFaction("hostility_after_skeleton_05", ConversationTopicTag.HostilityAftermath,
                new[] { Line(S.Initiator, "The living think they won. They only delayed the inevitable.", 0) },
                "faction_skeleton", ConversationPredicates.RequireAftermath));

            // GHOST
            templates.Add(CreateFaction("hostility_after_ghost_01", ConversationTopicTag.HostilityAftermath,
                new[] { Line(S.Initiator, "More echoes in these halls now. More memories that will never rest.", 0) },
                "faction_ghost", ConversationPredicates.RequireAftermath));
            templates.Add(CreateFaction("hostility_after_ghost_02", ConversationTopicTag.HostilityAftermath,
                new[] { Line(S.Initiator, "The newly dead drift confused. Guide them. They are ours now.", 0) },
                "faction_ghost", ConversationPredicates.RequireAftermath));
            templates.Add(CreateFaction("hostility_after_ghost_03", ConversationTopicTag.HostilityAftermath,
                new[] { Line(S.Initiator, "The veil is thick with grief. Even we feel it.", 0) },
                "faction_ghost", ConversationPredicates.RequireAftermath));
            templates.Add(CreateFaction("hostility_after_ghost_04", ConversationTopicTag.HostilityAftermath,
                new[] { Line(S.Initiator, "What was lost in flesh endures in spirit. We grow stronger through suffering.", 0) },
                "faction_ghost", ConversationPredicates.RequireAftermath));
            templates.Add(CreateFaction("hostility_after_ghost_05", ConversationTopicTag.HostilityAftermath,
                new[] { Line(S.Initiator, "Remember this day. We ghosts are nothing if not memory.", 0) },
                "faction_ghost", ConversationPredicates.RequireAftermath));

            // GOBLIN
            templates.Add(CreateFaction("hostility_after_goblin_01", ConversationTopicTag.HostilityAftermath,
                new[] { Line(S.Initiator, "Bury the dead ones. Loot the other dead ones.", 0),
                        Line(S.Responder, "Which is which?", 1),
                        Line(S.Initiator, "Loot first, bury later.", 2) },
                "faction_goblin", ConversationPredicates.RequireAftermath));
            templates.Add(CreateFaction("hostility_after_goblin_02", ConversationTopicTag.HostilityAftermath,
                new[] { Line(S.Initiator, "We lost Griknak. And Blobbo. And the one nobody knew the name of.", 0),
                        Line(S.Responder, "Pour some grog out for 'em.", 1) },
                "faction_goblin", ConversationPredicates.RequireAftermath));
            templates.Add(CreateFaction("hostility_after_goblin_03", ConversationTopicTag.HostilityAftermath,
                new[] { Line(S.Initiator, "Count the survivors. Then count the loot. Loot first, actually.", 0) },
                "faction_goblin", ConversationPredicates.RequireAftermath));
            templates.Add(CreateFaction("hostility_after_goblin_04", ConversationTopicTag.HostilityAftermath,
                new[] { Line(S.Initiator, "That was rough. But we got some good stuff. Look at this dagger!", 0) },
                "faction_goblin", ConversationPredicates.RequireAftermath));
            templates.Add(CreateFaction("hostility_after_goblin_05", ConversationTopicTag.HostilityAftermath,
                new[] { Line(S.Initiator, "Rebuild the walls. Use their bones. They'd do the same to us.", 0) },
                "faction_goblin", ConversationPredicates.RequireAftermath));

            // DEMON
            templates.Add(CreateFaction("hostility_after_demon_01", ConversationTopicTag.HostilityAftermath,
                new[] { Line(S.Initiator, "Let their ashes serve as inscription upon this ground.", 0) },
                "faction_demon", ConversationPredicates.RequireAftermath));
            templates.Add(CreateFaction("hostility_after_demon_02", ConversationTopicTag.HostilityAftermath,
                new[] { Line(S.Initiator, "The flames claimed both sides. But we are forged in fire. We endure.", 0) },
                "faction_demon", ConversationPredicates.RequireAftermath));
            templates.Add(CreateFaction("hostility_after_demon_03", ConversationTopicTag.HostilityAftermath,
                new[] { Line(S.Initiator, "Gather the essence of the fallen. Nothing is wasted in the inferno.", 0) },
                "faction_demon", ConversationPredicates.RequireAftermath));
            templates.Add(CreateFaction("hostility_after_demon_04", ConversationTopicTag.HostilityAftermath,
                new[] { Line(S.Initiator, "Mark the ground with brimstone. This is our domain. Let none forget.", 0) },
                "faction_demon", ConversationPredicates.RequireAftermath));
            templates.Add(CreateFaction("hostility_after_demon_05", ConversationTopicTag.HostilityAftermath,
                new[] { Line(S.Initiator, "The weak perished. The strong survived. This is as it should be.", 0) },
                "faction_demon", ConversationPredicates.RequireAftermath));

            // SNAKE
            templates.Add(CreateFaction("hostility_after_snake_01", ConversationTopicTag.HostilityAftermath,
                new[] { Line(S.Initiator, "Count the fallen. Ssswear the blood oath. We will have our revenge.", 0) },
                "faction_snake", ConversationPredicates.RequireAftermath));
            templates.Add(CreateFaction("hostility_after_snake_02", ConversationTopicTag.HostilityAftermath,
                new[] { Line(S.Initiator, "The dead ssshall be coiled into the earth with honor.", 0) },
                "faction_snake", ConversationPredicates.RequireAftermath));
            templates.Add(CreateFaction("hostility_after_snake_03", ConversationTopicTag.HostilityAftermath,
                new[] { Line(S.Initiator, "Shed your damaged ssscales. We heal. We grow ssstronger.", 0) },
                "faction_snake", ConversationPredicates.RequireAftermath));
            templates.Add(CreateFaction("hostility_after_snake_04", ConversationTopicTag.HostilityAftermath,
                new[] { Line(S.Initiator, "The Nest mournsss. But the Nest persssistsss.", 0) },
                "faction_snake", ConversationPredicates.RequireAftermath));
            templates.Add(CreateFaction("hostility_after_snake_05", ConversationTopicTag.HostilityAftermath,
                new[] { Line(S.Initiator, "Remember their ssscent. When the time comesss, we will know them.", 0) },
                "faction_snake", ConversationPredicates.RequireAftermath));
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
            bool requireRankDiff = false,
            ConversationPredicate predicate = null,
            int cooldownTurns = 0)
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
            template.predicate = predicate;
            template.cooldownTurns = cooldownTurns;
            return template;
        }

        /// <summary>
        /// Overload with requiredFactionId for faction-gated hostility dialogue.
        /// </summary>
        private static ConversationTemplate CreateFaction(
            string id,
            ConversationTopicTag topic,
            ConversationLine[] lines,
            string requiredFactionId,
            ConversationPredicate predicate,
            int cooldownTurns = 8)
        {
            var template = Create(id, topic, lines,
                crossFaction: true,
                predicate: predicate,
                cooldownTurns: cooldownTurns);
            template.requiredInitiatorFactionId = requiredFactionId;
            return template;
        }
    }
}
