using System.Collections.Generic;
using UnityEngine;

namespace InkSim
{
    /// <summary>
    /// Central hostility pipeline. All combat authorization flows through AuthorizeFight().
    /// Tracks inter-faction tension per district and escalates through stages.
    ///
    /// Consumers:
    ///   - EnemyAI.CanAttack, NpcAI.CanAttack  (fight authorization)
    ///   - Projectile.DamageAtTile              (friendly-fire gate)
    ///   - DamageUtils counter-attacks           (via CanAttack)
    ///   - ConversationManager                   (dialogue topic weights)
    ///   - ConversationPredicates                (stage-gated templates)
    ///
    /// Producers:
    ///   - FactionCombatService                  (Assault, Murder)
    ///   - FactionStrategyService                (TerritorySeized)
    ///   - DynamicSpawnService                   (PropertyDamage)
    ///   - InscriptionPoliticsService            (InscriptionDefaced)
    /// </summary>
    public static class HostilityPipeline
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            _tensionMap.Clear();
            _retaliationLog.Clear();
            _keyBuffer.Clear();
        }

        // ─── Configuration ───────────────────────────────────────────────
        /// <summary>Tension decay applied per economic day (20 turns).</summary>
        public const float TensionDecayPerDay = 0.05f;

        /// <summary>Turns within which a hit counts as recent for retaliation auth.</summary>
        public const int RetaliationWindowTurns = 3;

        /// <summary>
        /// Turns after last incident before a zeroed tension record can be cleaned up.
        /// Keeps records alive so aftermath/grievance predicates (RequireAftermath,
        /// RequireRecentIncident) can still read incidentCount and lastIncidentTurn.
        /// 100 turns = 5 economic days.
        /// </summary>
        public const int AftermathGraceTurns = 100;

        // ─── State ──────────────────────────────────────────────────────
        // Key: normalized (factionA, factionB, districtId) where factionA < factionB lexicographically
        private static readonly Dictionary<(string, string, string), TensionRecord> _tensionMap
            = new Dictionary<(string, string, string), TensionRecord>();

        // Key: defender instanceID → (attackerInstanceID, turn)
        // Tracks recent attacks for retaliation authorization
        private static readonly Dictionary<int, RetaliationEntry> _retaliationLog
            = new Dictionary<int, RetaliationEntry>();

        private struct RetaliationEntry
        {
            public int attackerInstanceId;
            public int turn;
        }

        // Reusable list for iteration (avoids alloc during EvaluateEscalation)
        private static readonly List<(string, string, string)> _keyBuffer
            = new List<(string, string, string)>();

        // ─── Fight Authorization ─────────────────────────────────────────

        /// <summary>
        /// The single gate for all combat. Every damage path must call this.
        /// Decision tree:
        ///   1. Null / self / same-faction → deny
        ///   2. Palimpsest truce → deny
        ///   3. HostilityService.IsHostile() → authorize (backward compat)
        ///   4. Pipeline tension >= Explosive → authorize
        ///   5. Recent retaliation → authorize
        ///   6. Otherwise → deny
        /// </summary>
        public static FightAuthorization AuthorizeFight(GridEntity attacker, GridEntity target)
        {
            // 1. Null / self / same-faction
            if (attacker == null || target == null)
                return FightAuthorization.Denied("null_entity");
            if (attacker == target)
                return FightAuthorization.Denied("self_target");
            if (HostilityService.AreSameFaction(attacker, target))
                return FightAuthorization.Denied("same_faction");

            // 2. Palimpsest truce (check both positions)
            var rulesAtTarget = OverlayResolver.GetRulesAt(target.gridX, target.gridY);
            if (rulesAtTarget.truce)
                return FightAuthorization.Denied("truce_zone");
            var rulesAtAttacker = OverlayResolver.GetRulesAt(attacker.gridX, attacker.gridY);
            if (rulesAtAttacker.truce)
                return FightAuthorization.Denied("truce_zone");

            // 3. Backward-compat: existing hostility system
            if (HostilityService.IsHostile(attacker, target))
            {
                // Look up tension for context (non-critical if missing)
                var stage = GetStageForPair(attacker, target);
                return FightAuthorization.Authorized("hostile_rep", stage.stage, stage.tension);
            }

            // 4. Pipeline tension >= Explosive
            var tension = GetStageForPair(attacker, target);
            if (tension.stage >= EscalationStage.Explosive)
                return FightAuthorization.Authorized("pipeline_explosive", tension.stage, tension.tension);

            // 5. Recent retaliation (attacker recently hit by target)
            if (HasRecentRetaliation(attacker, target))
                return FightAuthorization.Authorized("retaliation", tension.stage, tension.tension);

            // 6. Default deny
            return FightAuthorization.Denied("no_authorization");
        }

        // ─── Incident Reporting ──────────────────────────────────────────

        /// <summary>
        /// Report a hostile incident between two factions at a grid position.
        /// The district is auto-resolved from grid coordinates.
        /// </summary>
        public static void ReportIncident(IncidentType type, int gridX, int gridY,
                                           string factionA, string factionB)
        {
            if (string.IsNullOrEmpty(factionA) || string.IsNullOrEmpty(factionB)) return;
            if (factionA == factionB) return;

            // Resolve district from position
            string districtId = ResolveDistrictId(gridX, gridY);

            var key = NormalizeKey(factionA, factionB, districtId);
            float delta = IncidentTensionDeltas.GetDelta(type);
            int currentTurn = GetCurrentTurn();

            if (_tensionMap.TryGetValue(key, out var record))
            {
                record.tension = Mathf.Clamp01(record.tension + delta);
                record.stage = StageFromTension(record.tension);
                record.lastIncidentTurn = currentTurn;
                record.lastIncidentType = type;
                record.incidentCount++;
                _tensionMap[key] = record;
            }
            else
            {
                var newRecord = new TensionRecord
                {
                    factionA = key.Item1,
                    factionB = key.Item2,
                    districtId = key.Item3,
                    tension = Mathf.Clamp01(delta),
                    lastIncidentTurn = currentTurn,
                    lastIncidentType = type,
                    incidentCount = 1
                };
                newRecord.stage = StageFromTension(newRecord.tension);
                _tensionMap[key] = newRecord;
            }

            Debug.Log($"[HostilityPipeline] Incident {type} between {factionA}/{factionB} in {districtId}: " +
                      $"tension now {_tensionMap[key].tension:F2} ({_tensionMap[key].stage})");
        }

        // ─── Tick (called per economic day) ──────────────────────────────

        /// <summary>
        /// Decay all tension records. Called once per economic day from WorldSimulationService.
        /// </summary>
        public static void EvaluateEscalation(int dayNumber)
        {
            _keyBuffer.Clear();
            foreach (var kvp in _tensionMap)
                _keyBuffer.Add(kvp.Key);

            for (int i = 0; i < _keyBuffer.Count; i++)
            {
                var key = _keyBuffer[i];
                var record = _tensionMap[key];

                record.tension = Mathf.Max(0f, record.tension - TensionDecayPerDay);
                record.stage = StageFromTension(record.tension);
                _tensionMap[key] = record;

                // Clean up zeroed records to prevent unbounded growth.
                // Records with significant history (3+ incidents) are kept alive
                // for AftermathGraceTurns so aftermath/grievance predicates can fire.
                if (record.tension <= 0f)
                {
                    int turnsSinceLastIncident = GetCurrentTurn() - record.lastIncidentTurn;
                    if (record.incidentCount < 3 || turnsSinceLastIncident > AftermathGraceTurns)
                        _tensionMap.Remove(key);
                }
            }
        }

        // ─── Retaliation Tracking ────────────────────────────────────────

        /// <summary>
        /// Record that attacker just hit defender. Allows defender to retaliate
        /// within RetaliationWindowTurns without needing rep-based hostility.
        /// </summary>
        public static void RecordRetaliation(GridEntity attacker, GridEntity defender)
        {
            if (attacker == null || defender == null) return;

            _retaliationLog[defender.GetInstanceID()] = new RetaliationEntry
            {
                attackerInstanceId = attacker.GetInstanceID(),
                turn = GetCurrentTurn()
            };
        }

        /// <summary>
        /// Check if 'retaliator' was recently attacked by 'originalAttacker'.
        /// If so, retaliator is authorized to fight back.
        /// </summary>
        private static bool HasRecentRetaliation(GridEntity retaliator, GridEntity originalAttacker)
        {
            if (retaliator == null || originalAttacker == null) return false;

            if (_retaliationLog.TryGetValue(retaliator.GetInstanceID(), out var entry))
            {
                if (entry.attackerInstanceId == originalAttacker.GetInstanceID())
                {
                    int currentTurn = GetCurrentTurn();
                    return (currentTurn - entry.turn) <= RetaliationWindowTurns;
                }
            }
            return false;
        }

        // ─── Queries ─────────────────────────────────────────────────────

        /// <summary>
        /// Get tension record between two factions in a specific district.
        /// Returns a default (Calm, 0 tension) record if none exists.
        /// </summary>
        public static TensionRecord GetTension(string factionA, string factionB, string districtId)
        {
            if (string.IsNullOrEmpty(factionA) || string.IsNullOrEmpty(factionB))
                return default;

            var key = NormalizeKey(factionA, factionB, districtId ?? "");
            if (_tensionMap.TryGetValue(key, out var record))
                return record;
            return default;
        }

        /// <summary>
        /// Get the escalation stage between two factions in a specific district.
        /// </summary>
        public static EscalationStage GetStage(string factionA, string factionB, string districtId)
        {
            return GetTension(factionA, factionB, districtId).stage;
        }

        /// <summary>
        /// Get the peak (highest tension) record between two factions across ALL districts.
        /// Useful for dialogue that isn't district-specific.
        /// </summary>
        public static TensionRecord GetPeakTension(string factionA, string factionB)
        {
            if (string.IsNullOrEmpty(factionA) || string.IsNullOrEmpty(factionB))
                return default;

            string normA, normB;
            NormalizePair(factionA, factionB, out normA, out normB);

            TensionRecord peak = default;
            foreach (var kvp in _tensionMap)
            {
                if (kvp.Key.Item1 == normA && kvp.Key.Item2 == normB)
                {
                    if (kvp.Value.tension > peak.tension)
                        peak = kvp.Value;
                }
            }
            return peak;
        }

        // ─── Reset ───────────────────────────────────────────────────────

        /// <summary>
        /// Clear all pipeline state. Called on game restart and save load.
        /// </summary>
        public static void ClearAll()
        {
            _tensionMap.Clear();
            _retaliationLog.Clear();
        }

        // ─── Internal Helpers ────────────────────────────────────────────

        /// <summary>
        /// Get the combined stage info for a pair of entities (looks up their factions
        /// and the district at the target's position).
        /// </summary>
        private static TensionRecord GetStageForPair(GridEntity a, GridEntity b)
        {
            var factionA = a != null ? a.GetComponent<FactionMember>()?.faction : null;
            var factionB = b != null ? b.GetComponent<FactionMember>()?.faction : null;
            if (factionA == null || factionB == null) return default;

            string districtId = ResolveDistrictId(b.gridX, b.gridY);
            return GetTension(factionA.id, factionB.id, districtId);
        }

        private static (string, string, string) NormalizeKey(string factionA, string factionB, string districtId)
        {
            string a = factionA.ToLowerInvariant();
            string b = factionB.ToLowerInvariant();
            string d = districtId ?? "";

            // Lexicographic ordering so (A,B) == (B,A)
            if (string.CompareOrdinal(a, b) > 0)
            {
                var tmp = a;
                a = b;
                b = tmp;
            }
            return (a, b, d);
        }

        private static void NormalizePair(string factionA, string factionB, out string normA, out string normB)
        {
            normA = factionA.ToLowerInvariant();
            normB = factionB.ToLowerInvariant();
            if (string.CompareOrdinal(normA, normB) > 0)
            {
                var tmp = normA;
                normA = normB;
                normB = tmp;
            }
        }

        public static EscalationStage StageFromTension(float tension)
        {
            if (tension < 0.2f) return EscalationStage.Calm;
            if (tension < 0.4f) return EscalationStage.Uneasy;
            if (tension < 0.6f) return EscalationStage.Tense;
            if (tension < 0.8f) return EscalationStage.Volatile;
            return EscalationStage.Explosive;
        }

        private static string ResolveDistrictId(int gridX, int gridY)
        {
            var dcs = DistrictControlService.Instance;
            if (dcs != null)
            {
                var state = dcs.GetStateByPosition(gridX, gridY);
                if (state != null)
                    return state.Id;
            }
            return "";
        }

        private static int GetCurrentTurn()
        {
            return TurnManager.Instance != null ? TurnManager.Instance.TurnNumber : 0;
        }
    }
}
