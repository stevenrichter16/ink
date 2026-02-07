using System.Collections.Generic;
using UnityEngine;

namespace InkSim
{
    /// <summary>
    /// Factions use palimpsest inscriptions as tools of territorial control.
    /// Each economic day, factions write and erase inscriptions based on their
    /// control level, economic philosophy, and diplomatic state.
    /// </summary>
    public static class InscriptionPoliticsService
    {
        // Inscription duration in turns (2 economic days at 20 turns/day)
        private const int InscriptionDuration = 40;
        private const int InscriptionRadius = 6;

        // Track active inscription layer IDs per faction per district
        // Key = "factionId:districtId", Value = layer ID from OverlayResolver
        private static Dictionary<string, int> _activeLayerIds = new Dictionary<string, int>();

        public static void Execute(int dayNumber)
        {
            var dcs = DistrictControlService.Instance;
            if (dcs == null) return;

            Debug.Log($"[InscriptionPolitics] Day {dayNumber}: Evaluating faction inscriptions.");

            EraseRivalInscriptions(dcs);
            WriteFactionInscriptions(dcs);
        }

        /// <summary>
        /// When a faction gains control, erase inscriptions from rival factions in that district.
        /// </summary>
        private static void EraseRivalInscriptions(DistrictControlService dcs)
        {
            List<string> toRemove = new List<string>();

            foreach (var kvp in _activeLayerIds)
            {
                // Parse key: "factionId:districtId"
                string[] parts = kvp.Key.Split(':');
                if (parts.Length != 2) continue;

                string factionId = parts[0];
                string districtId = parts[1];

                // Find this district's controlling faction
                for (int d = 0; d < dcs.States.Count; d++)
                {
                    var state = dcs.States[d];
                    if (state.Id != districtId) continue;

                    int ownerIdx = state.ControllingFactionIndex;
                    if (ownerIdx < 0) break;

                    string ownerId = dcs.Factions[ownerIdx].id;

                    // If controlling faction is different and has strong control, erase rival inscriptions
                    if (ownerId != factionId && state.control[ownerIdx] > 0.6f)
                    {
                        int factionIdx = FactionStrategyService.GetFactionIndex(dcs, factionId);
                        if (factionIdx >= 0 && state.control[factionIdx] < 0.2f)
                        {
                            OverlayResolver.UnregisterLayer(kvp.Value);
                            toRemove.Add(kvp.Key);
                            Debug.Log($"[InscriptionPolitics] ERASED inscription by {factionId} in {districtId} (controlled by {ownerId})");
                        }
                    }
                    break;
                }
            }

            foreach (var key in toRemove)
                _activeLayerIds.Remove(key);
        }

        /// <summary>
        /// Factions write inscriptions in districts they control, based on their economic philosophy.
        /// </summary>
        private static void WriteFactionInscriptions(DistrictControlService dcs)
        {
            for (int d = 0; d < dcs.States.Count; d++)
            {
                var state = dcs.States[d];
                var districtDef = state.Definition;
                if (districtDef == null) continue;

                for (int f = 0; f < dcs.Factions.Count; f++)
                {
                    float control = state.control[f];
                    if (control < 0.2f) continue; // Not enough presence

                    var faction = dcs.Factions[f];
                    if (faction.economicPolicy == null) continue;

                    // Generate tokens based on faction state
                    List<string> tokens = GetTokensForFaction(faction, state, control);
                    if (tokens.Count == 0) continue;

                    // Check if we already have an active inscription for this faction+district
                    string key = $"{faction.id}:{state.Id}";
                    if (_activeLayerIds.ContainsKey(key))
                    {
                        // Already inscribed — skip (will naturally decay and be renewed next cycle)
                        continue;
                    }

                    // Calculate inscription center (district center)
                    int centerX = (districtDef.minX + districtDef.maxX) / 2;
                    int centerY = (districtDef.minY + districtDef.maxY) / 2;

                    // Priority scales with control level
                    int priority = Mathf.FloorToInt(control * 10f);

                    var layer = new PalimpsestLayer
                    {
                        center = new Vector2Int(centerX, centerY),
                        radius = InscriptionRadius,
                        priority = priority,
                        turnsRemaining = InscriptionDuration,
                        tokens = new List<string>(tokens)
                    };

                    int layerId = OverlayResolver.RegisterLayer(layer);
                    _activeLayerIds[key] = layerId;

                    Debug.Log($"[InscriptionPolitics] {faction.id} inscribed [{string.Join(", ", tokens)}] in {state.Id} (control={control:F2}, priority={priority})");

                    // Player-visible toast for notable inscriptions
                    string primaryToken = tokens[0];
                    if (primaryToken.StartsWith("TRUCE") || primaryToken.StartsWith("BLOCKADE")
                        || primaryToken.StartsWith("FREE_TRADE") || primaryToken.StartsWith("TRADE_BAN"))
                    {
                        string factionName = faction.displayName;
                        string tokenDisplay = FormatTokenForDisplay(primaryToken);
                        SimulationEventLog.ToastAtGrid($"{factionName}: {tokenDisplay}", SimulationEventLog.ColorInscription, centerX, centerY);
                    }
                }
            }
        }

        /// <summary>
        /// Determine what tokens a faction should inscribe based on its state and philosophy.
        /// </summary>
        private static List<string> GetTokensForFaction(FactionDefinition faction, DistrictState state, float control)
        {
            var tokens = new List<string>();
            var policy = faction.economicPolicy;

            // Strong control — mark territory as friendly
            if (control > 0.7f)
            {
                tokens.Add($"ALLY:{faction.id}");
            }

            // Under contest — hunt invaders
            bool isContested = FactionStrategyService.ContestedDistricts.ContainsKey(state.Id);
            if (isContested && control > 0.3f)
            {
                // Find the rival faction to hunt
                int ownerIdx = state.ControllingFactionIndex;
                var dcs = DistrictControlService.Instance;
                if (dcs != null && ownerIdx >= 0 && dcs.Factions[ownerIdx].id == faction.id)
                {
                    // We're the defender — hunt the attacker
                    string attackerId = FactionStrategyService.LastSkirmishAttackerFactionId;
                    if (!string.IsNullOrEmpty(attackerId))
                        tokens.Add($"HUNT:{attackerId}");
                }
            }

            // Desperate — try truce
            if (control < 0.3f && control > 0.1f)
            {
                tokens.Add("TRUCE");
            }

            // Economic philosophy inscriptions
            switch (policy.philosophy)
            {
                case TradePhilosophy.Exploitative:
                    if (control > 0.5f)
                        tokens.Add("INFLATE:0.15");
                    break;

                case TradePhilosophy.Cooperative:
                    if (control > 0.4f)
                        tokens.Add("DEFLATE:0.10");
                    break;

                case TradePhilosophy.Mercantile:
                    if (control > 0.5f)
                        tokens.Add("FREE_TRADE");
                    break;

                case TradePhilosophy.Isolationist:
                    // Isolationists block outside trade
                    if (control > 0.6f)
                        tokens.Add("BLOCKADE");
                    break;

                case TradePhilosophy.Aggressive:
                    // Aggressive factions ban rival trade
                    break;
            }

            // Trade bans from embargoes
            var relations = TradeRelationRegistry.GetForFaction(faction.id);
            if (relations != null)
            {
                for (int i = 0; i < relations.Count; i++)
                {
                    if (relations[i].status == TradeStatus.Embargo && control > 0.4f)
                    {
                        string rivalId = relations[i].sourceFactionId == faction.id
                            ? relations[i].targetFactionId
                            : relations[i].sourceFactionId;
                        tokens.Add($"TRADE_BAN:{rivalId}");
                    }
                }
            }

            return tokens;
        }

        /// <summary>Format a palimpsest token for player-readable display.</summary>
        private static string FormatTokenForDisplay(string token)
        {
            if (token.StartsWith("TRUCE")) return "Truce declared!";
            if (token.StartsWith("BLOCKADE")) return "Blockade imposed!";
            if (token.StartsWith("FREE_TRADE")) return "Free trade zone!";
            if (token.StartsWith("TRADE_BAN"))
            {
                string target = token.Length > 10 ? token.Substring(10) : "rivals";
                return $"Trade ban on {target}";
            }
            if (token.StartsWith("INFLATE")) return "Prices rising!";
            if (token.StartsWith("DEFLATE")) return "Prices lowered!";
            if (token.StartsWith("ALLY")) return "Territory claimed";
            if (token.StartsWith("HUNT")) return "Hunt decree!";
            return token;
        }

        /// <summary>Clear state for testing / game restart.</summary>
        public static void Clear()
        {
            _activeLayerIds.Clear();
        }
    }
}
