namespace InkSim
{
    /// <summary>
    /// Reusable predicate functions for conversation templates.
    /// Each returns true if the dialogue line's claimed world-state is actually true.
    /// Signature matches <see cref="ConversationPredicate"/> delegate.
    /// </summary>
    public static class ConversationPredicates
    {
        // ================================================================
        // P0 — Blocks factually false claims
        // ================================================================

        /// <summary>
        /// "Reinforcements arrived this morning" —
        /// requires DynamicSpawnService spawned reinforcements in this district within 2 days.
        /// </summary>
        public static bool RequireRecentReinforcements(
            FactionMember initiator, FactionMember responder, DistrictState ds)
        {
            if (ds == null) return false;
            var dcs = DistrictControlService.Instance;
            if (dcs == null) return false;
            return DynamicSpawnService.LastReinforcementDistrictId == ds.Id
                && (dcs.CurrentDay - DynamicSpawnService.LastReinforcementDay) <= 2;
        }

        /// <summary>
        /// "Supply lines stretched thin" + "raiding caravans" —
        /// requires low supply in district AND a recent raid event anywhere.
        /// </summary>
        public static bool RequireSupplyStressAndRaid(
            FactionMember initiator, FactionMember responder, DistrictState ds)
        {
            if (ds == null) return false;
            // Check supply: any item below 0.5 or low prosperity
            bool supplyStressed = ds.prosperity < 0.6f;
            if (!supplyStressed && ds.itemSupply != null)
            {
                foreach (var kvp in ds.itemSupply)
                {
                    if (kvp.Value < 0.5f)
                    {
                        supplyStressed = true;
                        break;
                    }
                }
            }
            if (!supplyStressed) return false;
            // Check recent raid
            var dcs = DistrictControlService.Instance;
            if (dcs == null) return false;
            return !string.IsNullOrEmpty(DynamicSpawnService.LastRaidDistrictId)
                && (dcs.CurrentDay - DynamicSpawnService.LastRaidDay) <= 3;
        }

        /// <summary>
        /// "Heard the {FACTION_OTHER} lost control" —
        /// requires any faction to have lossStreak >= 3 in any district.
        /// </summary>
        public static bool RequireFactionLossEvent(
            FactionMember initiator, FactionMember responder, DistrictState ds)
        {
            var dcs = DistrictControlService.Instance;
            if (dcs == null) return false;
            for (int d = 0; d < dcs.States.Count; d++)
            {
                var state = dcs.States[d];
                if (state.lossStreak == null) continue;
                for (int f = 0; f < state.lossStreak.Length; f++)
                {
                    if (state.lossStreak[f] >= 3) return true;
                }
            }
            return false;
        }

        /// <summary>
        /// "They hit the supply wagons last night" —
        /// requires a raid in THIS district within the last 2 days.
        /// </summary>
        public static bool RequireRecentRaidInDistrict(
            FactionMember initiator, FactionMember responder, DistrictState ds)
        {
            if (ds == null) return false;
            var dcs = DistrictControlService.Instance;
            if (dcs == null) return false;
            return DynamicSpawnService.LastRaidDistrictId == ds.Id
                && (dcs.CurrentDay - DynamicSpawnService.LastRaidDay) <= 2;
        }

        /// <summary>
        /// "We saw what you did to the village" —
        /// requires the responder's faction to be the attacker in a recent raid or skirmish.
        /// </summary>
        public static bool RequireRecentAttackByResponderFaction(
            FactionMember initiator, FactionMember responder, DistrictState ds)
        {
            if (responder?.faction == null) return false;
            string responderFactionId = responder.faction.id;
            var dcs = DistrictControlService.Instance;
            if (dcs == null) return false;
            // Check raid by responder's faction (within 5 days)
            bool raidMatch = DynamicSpawnService.LastRaidFactionId == responderFactionId
                && (dcs.CurrentDay - DynamicSpawnService.LastRaidDay) <= 5;
            // Check skirmish by responder's faction
            bool skirmishMatch = FactionStrategyService.LastSkirmishAttackerFactionId == responderFactionId;
            return raidMatch || skirmishMatch;
        }

        /// <summary>
        /// "Three days we've held this ground" —
        /// requires THIS district to be contested for >= 3 days.
        /// </summary>
        public static bool RequireContestedThreePlusDays(
            FactionMember initiator, FactionMember responder, DistrictState ds)
        {
            if (ds == null) return false;
            return FactionStrategyService.ContestedDistricts.TryGetValue(ds.Id, out int days)
                && days >= 3;
        }

        // ================================================================
        // P1 — Blocks implied-but-unverified events
        // ================================================================

        /// <summary>
        /// "Talk of an embargo on ink" —
        /// requires any embargo to exist worldwide.
        /// </summary>
        public static bool RequireAnyEmbargoExists(
            FactionMember initiator, FactionMember responder, DistrictState ds)
        {
            var all = TradeRelationRegistry.GetAll();
            if (all == null) return false;
            for (int i = 0; i < all.Count; i++)
            {
                if (all[i].status == TradeStatus.Embargo) return true;
            }
            return false;
        }

        /// <summary>
        /// "Someone inscribed a truce marker near the border" —
        /// requires an active TRUCE palimpsest overlay in the district.
        /// </summary>
        public static bool RequireTruceInscriptionNearby(
            FactionMember initiator, FactionMember responder, DistrictState ds)
        {
            if (ds?.Definition == null) return false;
            int cx = (ds.Definition.minX + ds.Definition.maxX) / 2;
            int cy = (ds.Definition.minY + ds.Definition.maxY) / 2;
            var rules = OverlayResolver.GetRulesAt(cx, cy);
            return rules.truce;
        }

        /// <summary>
        /// "The demons are massing" —
        /// requires faction_demon control > 0.3 in any district.
        /// </summary>
        public static bool RequireDemonThreat(
            FactionMember initiator, FactionMember responder, DistrictState ds)
        {
            var dcs = DistrictControlService.Instance;
            if (dcs == null) return false;
            int demonIdx = FactionStrategyService.GetFactionIndex(dcs, "faction_demon");
            if (demonIdx < 0) return false;
            for (int d = 0; d < dcs.States.Count; d++)
            {
                if (dcs.States[d].control[demonIdx] > 0.3f) return true;
            }
            return false;
        }

        /// <summary>
        /// "Three soldiers deserted last night" —
        /// requires desperate conditions (prosperity &lt; 0.3).
        /// </summary>
        public static bool RequireLowMorale(
            FactionMember initiator, FactionMember responder, DistrictState ds)
        {
            if (ds == null) return false;
            return ds.prosperity < 0.3f;
        }

        /// <summary>
        /// "Offering coin for demon hides" —
        /// requires an active demon-related quest.
        /// </summary>
        public static bool RequireActiveDemonQuest(
            FactionMember initiator, FactionMember responder, DistrictState ds)
        {
            return DynamicQuestService.HasActiveQuestWithPrefix("dyn_demon_");
        }

        /// <summary>
        /// "The embargo is strangling the market" —
        /// requires an embargo specifically between these two factions.
        /// </summary>
        public static bool RequireEmbargoBetweenFactions(
            FactionMember initiator, FactionMember responder, DistrictState ds)
        {
            if (initiator?.faction == null || responder?.faction == null) return false;
            var rel = TradeRelationRegistry.GetRelation(initiator.faction.id, responder.faction.id);
            return rel != null && rel.status == TradeStatus.Embargo;
        }

        /// <summary>
        /// "They pushed us back from the outer wall" —
        /// requires this district to be contested AND a skirmish happened here.
        /// </summary>
        public static bool RequireContestedWithSkirmish(
            FactionMember initiator, FactionMember responder, DistrictState ds)
        {
            if (ds == null) return false;
            bool contested = FactionStrategyService.ContestedDistricts.ContainsKey(ds.Id);
            bool skirmishHere = FactionStrategyService.LastSkirmishDistrictId == ds.Id;
            return contested && skirmishHere;
        }

        /// <summary>
        /// "Heat's been rising in {DISTRICT}" —
        /// requires elevated heat (> 0.4) for the initiator's faction.
        /// </summary>
        public static bool RequireElevatedHeat(
            FactionMember initiator, FactionMember responder, DistrictState ds)
        {
            if (ds == null || initiator?.faction == null) return false;
            var dcs = DistrictControlService.Instance;
            if (dcs == null) return false;
            int fIdx = FactionStrategyService.GetFactionIndex(dcs, initiator.faction.id);
            if (fIdx < 0 || ds.heat == null || fIdx >= ds.heat.Length) return false;
            return ds.heat[fIdx] > 0.4f;
        }

        // ================================================================
        // P2 — Fixes token/response contradictions
        // ================================================================

        /// <summary>
        /// "We hold the line" — should not fire when control is "lost" or "unclaimed".
        /// Requires initiator's faction control >= 0.2.
        /// </summary>
        public static bool RequireNotLostControl(
            FactionMember initiator, FactionMember responder, DistrictState ds)
        {
            if (ds == null || initiator?.faction == null) return false;
            var dcs = DistrictControlService.Instance;
            if (dcs == null) return false;
            int fIdx = FactionStrategyService.GetFactionIndex(dcs, initiator.faction.id);
            if (fIdx < 0 || ds.control == null || fIdx >= ds.control.Length) return false;
            return ds.control[fIdx] >= 0.2f;
        }

        /// <summary>
        /// "Double the watch" — only meaningful when things are bad.
        /// Requires prosperity &lt; 0.5.
        /// </summary>
        public static bool RequirePoorProsperity(
            FactionMember initiator, FactionMember responder, DistrictState ds)
        {
            if (ds == null) return false;
            return ds.prosperity < 0.5f;
        }

        /// <summary>
        /// "Supplies came in from {DISTRICT}" — implies successful supply delivery.
        /// Requires prosperity >= 0.5 (stable enough for supplies to flow).
        /// </summary>
        public static bool RequireStableProsperity(
            FactionMember initiator, FactionMember responder, DistrictState ds)
        {
            if (ds == null) return false;
            return ds.prosperity >= 0.5f;
        }

        // ================================================================
        // Hostility Pipeline — Stage-gated predicates
        // ================================================================

        /// <summary>
        /// Requires tension between initiator's and responder's factions to be >= Uneasy
        /// in the current district.
        /// </summary>
        public static bool RequireTensionUneasy(
            FactionMember initiator, FactionMember responder, DistrictState ds)
        {
            return GetPairStage(initiator, responder, ds) >= EscalationStage.Uneasy;
        }

        /// <summary>
        /// Requires tension >= Tense.
        /// </summary>
        public static bool RequireTensionTense(
            FactionMember initiator, FactionMember responder, DistrictState ds)
        {
            return GetPairStage(initiator, responder, ds) >= EscalationStage.Tense;
        }

        /// <summary>
        /// Requires tension >= Volatile.
        /// </summary>
        public static bool RequireTensionVolatile(
            FactionMember initiator, FactionMember responder, DistrictState ds)
        {
            return GetPairStage(initiator, responder, ds) >= EscalationStage.Volatile;
        }

        /// <summary>
        /// Requires tension >= Explosive.
        /// </summary>
        public static bool RequireTensionExplosive(
            FactionMember initiator, FactionMember responder, DistrictState ds)
        {
            return GetPairStage(initiator, responder, ds) >= EscalationStage.Explosive;
        }

        /// <summary>
        /// Requires tension was Explosive recently but has decayed below it (de-escalation).
        /// Uses Volatile as proxy: stage is exactly Volatile and there's a recent incident.
        /// </summary>
        public static bool RequireDeEscalation(
            FactionMember initiator, FactionMember responder, DistrictState ds)
        {
            var record = GetPairTension(initiator, responder, ds);
            // De-escalation = was high but dropping. Volatile with incidents means it was worse.
            return record.stage == EscalationStage.Volatile && record.incidentCount > 0;
        }

        /// <summary>
        /// Requires a specific incident type occurred recently (any stage).
        /// Used for grievance dialogue that references the incident.
        /// </summary>
        public static bool RequireRecentIncident(
            FactionMember initiator, FactionMember responder, DistrictState ds)
        {
            var record = GetPairTension(initiator, responder, ds);
            if (record.incidentCount == 0) return false;
            int currentTurn = TurnManager.Instance != null ? TurnManager.Instance.TurnNumber : 0;
            // Within last 40 turns (2 economic days)
            return (currentTurn - record.lastIncidentTurn) <= 40;
        }

        /// <summary>
        /// Requires tension has dropped back to Calm after incidents (aftermath).
        /// Stage is Calm but incidentCount > 0 means there was conflict.
        /// </summary>
        public static bool RequireAftermath(
            FactionMember initiator, FactionMember responder, DistrictState ds)
        {
            var record = GetPairTension(initiator, responder, ds);
            return record.stage <= EscalationStage.Uneasy && record.incidentCount >= 3;
        }

        // --- Helpers ---

        private static EscalationStage GetPairStage(FactionMember a, FactionMember b, DistrictState ds)
        {
            if (a?.faction == null || b?.faction == null) return EscalationStage.Calm;
            string districtId = ds != null ? ds.Id : "";
            return HostilityPipeline.GetStage(a.faction.id, b.faction.id, districtId);
        }

        private static TensionRecord GetPairTension(FactionMember a, FactionMember b, DistrictState ds)
        {
            if (a?.faction == null || b?.faction == null) return default;
            string districtId = ds != null ? ds.Id : "";
            return HostilityPipeline.GetTension(a.faction.id, b.faction.id, districtId);
        }
    }
}
