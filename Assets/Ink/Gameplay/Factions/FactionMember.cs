using System.Collections.Generic;
using UnityEngine;

namespace InkSim
{
    /// <summary>
    /// Applies faction/rank data to an entity: stats, sprite, equipment, dialogue, and spells.
    /// </summary>
    public class FactionMember : MonoBehaviour
    {
        private static readonly List<FactionMember> _activeMembers = new List<FactionMember>();

        public static IReadOnlyList<FactionMember> ActiveMembers => _activeMembers;

        public enum AlertState
        {
            Calm,
            Alert,
            Hostile
        }

        public FactionDefinition faction;
        public string rankId = "low";
        public int reputationOffset = 0;
        public bool applyLevelFromRank = true;
        public AlertState state = AlertState.Calm;
        public int alertTurnsRemaining;
        private int turnsSincePlayerAttack;
        private bool lastAggroFromPlayer;

        private FactionDefinition.RankDefinition _rank;

        private void OnEnable()
        {
            if (!_activeMembers.Contains(this))
                _activeMembers.Add(this);
        }

        private void OnDisable()
        {
            _activeMembers.Remove(this);
        }

        void Start()
        {
            ApplyRank();
        }

        public void ApplyRank()
        {
            if (faction == null)
            {
                var speciesMember = GetComponent<SpeciesMember>();
                if (speciesMember != null && speciesMember.species != null)
                {
                    faction = speciesMember.species.defaultFaction;
                    if (string.IsNullOrEmpty(rankId))
                        rankId = speciesMember.species.defaultRankId;
                }
            }

            if (faction == null)
            {
                Debug.LogWarning($"[FactionMember] No faction set on {name}");
                return;
            }

            _rank = faction.GetRank(rankId);

            ReputationSystem.EnsureFaction(faction.id, faction.defaultReputation + reputationOffset);

            var levelable = GetComponent<Levelable>() ?? gameObject.AddComponent<Levelable>();
            var profileToUse = _rank?.levelProfileOverride ?? faction.defaultLevelProfile ?? levelable.profile;
            if (profileToUse != null)
                levelable.profile = profileToUse;

            if (_rank != null && applyLevelFromRank)
                levelable.SetLevel(_rank.level);
            else
                levelable.RecomputeStats();

            // Initialize health for NPCs and Enemies after stats are finalized
            var npc = GetComponent<NpcAI>();
            if (npc != null)
                npc.InitializeHealth();

            var enemy = GetComponent<EnemyAI>();
            if (enemy != null)
                enemy.InitializeHealth();

            var sr = GetComponent<SpriteRenderer>();
            if (sr != null && _rank != null && _rank.spriteIndices != null && _rank.spriteIndices.Count > 0)
            {
                var sprite = SpriteLibrary.Instance?.GetSprite(_rank.spriteIndices[0]);
                if (sprite != null)
                    sr.sprite = sprite;
            }

            EquipDefaults();
            WireDialogue();
            ApplySpells();
        }

        public void EnterAlert(bool allowFromHostile = false)
        {
            if (state == AlertState.Hostile && !allowFromHostile) return;
            state = AlertState.Alert;
            alertTurnsRemaining = faction != null ? Mathf.Max(1, faction.alertDurationTurns) : 2;
            turnsSincePlayerAttack = 0;
            Debug.Log($"[FactionMember] {name} entering ALERT for {alertTurnsRemaining} turns.");

            // Clear hostile targeting so AI stops attacking while alert
            var npc = GetComponent<NpcAI>();
            if (npc != null)
                npc.hostileTarget = null;

            var enemy = GetComponent<EnemyAI>();
            if (enemy != null)
                enemy.SetRetaliationTarget(null);
        }

        public void EnterHostile(GridEntity target = null)
        {
            state = AlertState.Hostile;
            alertTurnsRemaining = 0;
            lastAggroFromPlayer = lastAggroFromPlayer || target is PlayerController;
            if (target is PlayerController)
                turnsSincePlayerAttack = 0;
            Debug.Log($"[FactionMember] {name} entering HOSTILE target={target?.name}");

            var npc = GetComponent<NpcAI>();
            if (npc != null && target != null)
                npc.hostileTarget = target;

            var enemy = GetComponent<EnemyAI>();
            if (enemy != null && target != null)
                enemy.SetRetaliationTarget(target);
        }

        public void EnterCalm()
        {
            state = AlertState.Calm;
            alertTurnsRemaining = 0;
            lastAggroFromPlayer = false;
            turnsSincePlayerAttack = 0;
            Debug.Log($"[FactionMember] {name} entering CALM.");

            var npc = GetComponent<NpcAI>();
            if (npc != null)
                npc.hostileTarget = null;

            var enemy = GetComponent<EnemyAI>();
            if (enemy != null)
                enemy.SetRetaliationTarget(null);
        }

        public void TickAlert()
        {
            if (state != AlertState.Alert) return;
            Debug.Log($"[FactionMember] {name} ALERT ticking: {alertTurnsRemaining} -> {alertTurnsRemaining - 1} turns remaining.");
            alertTurnsRemaining--;
            if (alertTurnsRemaining <= 0)
            {
                Debug.Log($"[FactionMember] {name} Alert expired -> CALM.");
                EnterCalm();
            }
        }

        public void NotePlayerAttack()
        {
            lastAggroFromPlayer = true;
            turnsSincePlayerAttack = 0;
            Debug.Log($"[FactionMember] {name} noted player attack. lastAggroFromPlayer set, counter reset.");
        }

        /// <summary>
        /// Handles de-escalation after the player stops attacking: Hostile->Alert after 5 turns, Alert->Calm after 7.
        /// Only applies when aggression came from the player and faction rep is not hostile.
        /// </summary>
        public void TickAggroCooldown()
        {
            if (!lastAggroFromPlayer) return;
            turnsSincePlayerAttack++;

            // Require faction rep to be above hostile to allow forgiveness
            int rep = faction != null ? ReputationSystem.GetRep(faction.id) : 0;
            if (rep <= HostilityService.HostileThreshold)
            {
                Debug.Log($"[FactionMember] {name} cooldown paused (rep hostile {rep}) at {turnsSincePlayerAttack} turns.");
                return;
            }

            Debug.Log($"[FactionMember] {name} cooldown tick {turnsSincePlayerAttack} (state={state}).");

            if (state == AlertState.Hostile && turnsSincePlayerAttack >= 5)
            {
                Debug.Log($"[FactionMember] {name} cooling from HOSTILE -> ALERT after {turnsSincePlayerAttack} turns without player attacks.");
                EnterAlert(allowFromHostile: true);
            }

            if (state == AlertState.Alert && turnsSincePlayerAttack >= 7)
            {
                Debug.Log($"[FactionMember] {name} cooling from ALERT -> CALM after {turnsSincePlayerAttack} turns without player attacks.");
                EnterCalm();
            }
        }

        public static void ForAlliesInRadius(FactionMember origin, int radius, System.Action<FactionMember> action)
        {
            if (origin == null || origin.faction == null || action == null) return;
            int r2 = radius * radius;
            var originEntity = origin.GetComponent<GridEntity>();
            if (originEntity == null) return;

            var list = _activeMembers;
            for (int i = 0; i < list.Count; i++)
            {
                var member = list[i];
                if (member == null || member == origin) continue;
                if (member.faction == null || member.faction.id != origin.faction.id) continue;
                if (!member.isActiveAndEnabled) continue;

                var ent = member.GetComponent<GridEntity>();
                if (ent == null || !ent.gameObject.activeInHierarchy) continue;

                int dx = ent.gridX - originEntity.gridX;
                int dy = ent.gridY - originEntity.gridY;
                if (dx * dx + dy * dy > r2) continue;

                action(member);
            }
        }

        private void EquipDefaults()
        {
            if (_rank == null) return;

            var inventory = GetComponent<Inventory>() ?? gameObject.AddComponent<Inventory>();
            var equipment = GetComponent<Equipment>() ?? gameObject.AddComponent<Equipment>();

            TryEquip(_rank.weaponId, ItemType.Weapon, inventory, equipment);
            TryEquip(_rank.armorId, ItemType.Armor, inventory, equipment);
            TryEquip(_rank.accessoryId, ItemType.Accessory, inventory, equipment);
        }

        private void TryEquip(string itemId, ItemType slot, Inventory inventory, Equipment equipment)
        {
            if (string.IsNullOrEmpty(itemId)) return;
            if (!ItemDatabase.Exists(itemId)) return;

            inventory.AddItem(itemId, 1);
            var item = inventory.GetItem(itemId);
            if (item != null && item.data.type == slot)
            {
                equipment.Equip(item, inventory);
            }
        }

        private void WireDialogue()
        {
            var runner = GetComponent<DialogueRunner>();
            if (runner == null) return;

            runner.factionId = faction.id;

            if (_rank != null)
            {
                if (_rank.neutralDialogue != null)
                    runner.defaultSequence = _rank.neutralDialogue;
                if (_rank.friendlyDialogue != null)
                    runner.friendlySequence = _rank.friendlyDialogue;
                if (_rank.hostileDialogue != null)
                    runner.hostileSequence = _rank.hostileDialogue;
            }
        }

        private void ApplySpells()
        {
            if (_rank == null || _rank.defaultSpells == null || _rank.defaultSpells.Count == 0) return;

            var spellSystem = GetComponent<SpellSystem>();
            if (spellSystem == null) return;

            foreach (var spell in _rank.defaultSpells)
            {
                if (spell != null && !spellSystem.equippedSpells.Contains(spell))
                    spellSystem.equippedSpells.Add(spell);
            }

            spellSystem.cooldownTimers = new float[spellSystem.equippedSpells.Count];
        }
    }
}
