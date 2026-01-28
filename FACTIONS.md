## Faction System (current project)

### What was added
- New folder: `Assets/Ink/Gameplay/Factions/` (FactionDefinition.cs, FactionMember.cs, ReputationSystem.cs).
- New Resources folder entries: `Assets/Ink/Resources/Factions/Inkbound.asset`, `Inkguard.asset`.
- Code integrations into `DialogueRunner.cs`, `EnemyFactory.cs`, and `TestMapBuilder.cs` to be faction-aware.

### Core Scripts

- **Assets/Ink/Gameplay/Factions/FactionDefinition.cs**
  - ScriptableObject describing a faction: `id`, `displayName`, `defaultReputation`, `defaultLevelProfile`.
  - Contains `RankDefinition` list; each rank has `rankId`, `displayName`, `level`, optional `levelProfileOverride`, `spriteIndices`, default equipment IDs (`weaponId`, `armorId`, `accessoryId`), `defaultSpells` (List\<SpellData\>), and optional dialogue sequences (`neutral`, `friendly`, `hostile`).
  - Helper `GetRank(string rankId)` fetches a rank.

- **Assets/Ink/Gameplay/Factions/FactionMember.cs**
  - MonoBehaviour applied to NPC/enemy GOs to enforce faction/rank data.
  - `ApplyRank()`:
    - Ensures a reputation entry via `ReputationSystem.EnsureFaction`.
    - Configures `Levelable` (sets profile from rank override or faction default; sets level).
    - Sets sprite via `SpriteLibrary` using the first `spriteIndices` entry.
    - Ensures `Inventory`/`Equipment` exist; equips default weapon/armor/accessory IDs if present.
    - Wires dialogue on `DialogueRunner` (sets `factionId`, and rank-specified neutral/friendly/hostile sequences).
    - Adds default spells to an attached `SpellSystem` and resizes cooldowns.
  - Fields: `faction`, `rankId`, `reputationOffset`.

- **Assets/Ink/Gameplay/Factions/ReputationSystem.cs**
  - Static in-memory map `factionId -> int rep`.
  - API: `GetRep`, `EnsureFaction`, `SetRep`, `AddRep`.
  - Event: `OnRepChanged(factionId, newValue)`.
  - Not persisted (no save/load integration).

- **Assets/Ink/Gameplay/Dialogue/DialogueRunner.cs**
  - Modified: reputation-aware branching fields `factionId`, `friendlySequence`, `hostileSequence`, thresholds (`friendlyThreshold`, `hostileThreshold`).
  - `SelectSequence()` now swaps to friendly/hostile sequences based on rep, otherwise uses quest/default logic.

- **Assets/Ink/Gameplay/Enemies/EnemyFactory.cs**
  - Modified: spawn methods accept optional `FactionDefinition faction, string factionRankId`.
  - If provided, attaches `FactionMember` to the spawned enemy and calls `ApplyRank()` after base setup.

### Data Assets

- **Assets/Ink/Resources/Factions/Inkbound.asset**
  - id `faction_inkbound`; sprite index `9` (tile_0009). Ranks low/mid/high; no gear; each rank includes Fireball spell (`Spells/Fireball` GUID `8cf77dd05f89f4b5991182ca21ea627e`); default reputation 0.

- **Assets/Ink/Resources/Factions/Inkguard.asset**
  - id `faction_inkguard`; sprite index `15` (tile_0015). Ranks low/mid/high; gear: `sword` + `iron_armor`; no spells; default reputation 0.

### Scene Wiring (Test Map)

- **Assets/Ink/TestMapBuilder.cs**
  - Loads factions via `Resources.Load<FactionDefinition>("Factions/Inkbound")` and `"Factions/Inkguard"`.
  - NPC creation takes faction/rank; applies `FactionMember.ApplyRank()` when provided.
  - Forest merchant uses Inkbound rank `low`; dungeon weaponsmith uses Inkguard rank `mid`.

### Notes / Limitations

- Reputation is in-memory only (no save/load).
- Only the first sprite index is used (no randomization).
- Sample faction dialogues are null (except quest-specific overrides in `TestMapBuilder`).
- Spells apply only if a `SpellSystem` component is present on the entity.

### File Contents (for reference)

#### Assets/Ink/Gameplay/Factions/FactionDefinition.cs
```csharp
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
        [Header("Identity")]
        public string id = "faction_id";
        public string displayName = "Faction";
        public int defaultReputation = 0;
        public LevelProfile defaultLevelProfile;

        [Header("Ranks")]
        public List<RankDefinition> ranks = new List<RankDefinition>();

        [System.Serializable]
        public class RankDefinition
        {
            public string rankId = "low";
            public string displayName = "Acolyte";
            public int level = 1;
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
```

#### Assets/Ink/Gameplay/Factions/FactionMember.cs
```csharp
using UnityEngine;

namespace InkSim
{
    /// <summary>
    /// Applies faction/rank data to an entity: stats, sprite, equipment, dialogue, and spells.
    /// </summary>
    public class FactionMember : MonoBehaviour
    {
        public FactionDefinition faction;
        public string rankId = "low";
        public int reputationOffset = 0;

        private FactionDefinition.RankDefinition _rank;

        public void ApplyRank()
        {
            if (faction == null)
            {
                Debug.LogWarning($"[FactionMember] No faction set on {name}");
                return;
            }

            _rank = faction.GetRank(rankId);

            // Ensure reputation entry exists
            ReputationSystem.EnsureFaction(faction.id, faction.defaultReputation + reputationOffset);

            // Stats
            var levelable = GetComponent<Levelable>() ?? gameObject.AddComponent<Levelable>();
            var profileToUse = _rank?.levelProfileOverride ?? faction.defaultLevelProfile ?? levelable.profile;
            if (profileToUse != null)
                levelable.profile = profileToUse;

            if (_rank != null)
                levelable.SetLevel(_rank.level);
            else
                levelable.RecomputeStats();

            // Sprite
            var sr = GetComponent<SpriteRenderer>();
            if (sr != null && _rank != null && _rank.spriteIndices.Count > 0)
            {
                var sprite = SpriteLibrary.Instance?.GetSprite(_rank.spriteIndices[0]);
                if (sprite != null)
                    sr.sprite = sprite;
            }

            // Equipment
            EquipDefaults();

            // Dialogue
            WireDialogue();

            // Spells
            ApplySpells();
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
```

#### Assets/Ink/Gameplay/Factions/ReputationSystem.cs
```csharp
using System;
using System.Collections.Generic;

namespace InkSim
{
    /// <summary>
    /// Tracks player reputation per faction (in-memory).
    /// </summary>
    public static class ReputationSystem
    {
        private static readonly Dictionary<string, int> _rep = new Dictionary<string, int>();

        public static event Action<string, int> OnRepChanged;

        public static int GetRep(string factionId)
        {
            if (string.IsNullOrEmpty(factionId)) return 0;
            return _rep.TryGetValue(factionId, out var value) ? value : 0;
        }

        public static void EnsureFaction(string factionId, int defaultValue = 0)
        {
            if (string.IsNullOrEmpty(factionId)) return;
            if (!_rep.ContainsKey(factionId))
                _rep[factionId] = defaultValue;
        }

        public static void SetRep(string factionId, int value)
        {
            if (string.IsNullOrEmpty(factionId)) return;
            _rep[factionId] = value;
            OnRepChanged?.Invoke(factionId, value);
        }

        public static void AddRep(string factionId, int delta)
        {
            SetRep(factionId, GetRep(factionId) + delta);
        }
    }
}
```

#### Assets/Ink/Resources/Factions/Inkbound.asset (YAML)
```yaml
id: faction_inkbound
displayName: Inkbound
defaultReputation: 0
defaultLevelProfile: Assets/Ink/Data/Levels/DefaultLevelProfile.asset
ranks:
  - rankId: low
    displayName: Inkbound Novice
    level: 1
    spriteIndices: [9]        # tile_0009
    weaponId: ""
    armorId: ""
    accessoryId: ""
    defaultSpells: [Spells/Fireball]
    dialogues: neutral/friendly/hostile = null
  - rankId: mid
    displayName: Inkbound Adept
    level: 3
    spriteIndices: [9]
    weaponId: ""
    armorId: ""
    accessoryId: ""
    defaultSpells: [Spells/Fireball]
    dialogues: null
  - rankId: high
    displayName: Inkbound Magus
    level: 5
    spriteIndices: [9]
    weaponId: ""
    armorId: ""
    accessoryId: ""
    defaultSpells: [Spells/Fireball]
    dialogues: null
```

#### Assets/Ink/Resources/Factions/Inkguard.asset (YAML)
```yaml
id: faction_inkguard
displayName: Inkguard
defaultReputation: 0
defaultLevelProfile: Assets/Ink/Data/Levels/DefaultLevelProfile.asset
ranks:
  - rankId: low
    displayName: Inkguard Recruit
    level: 1
    spriteIndices: [15]       # tile_0015
    weaponId: sword
    armorId: iron_armor
    accessoryId: ""
    defaultSpells: []
    dialogues: null
  - rankId: mid
    displayName: Inkguard Soldier
    level: 3
    spriteIndices: [15]
    weaponId: sword
    armorId: iron_armor
    accessoryId: ""
    defaultSpells: []
    dialogues: null
  - rankId: high
    displayName: Inkguard Captain
    level: 5
    spriteIndices: [15]
    weaponId: sword
    armorId: iron_armor
    accessoryId: ""
    defaultSpells: []
    dialogues: null
```

#### Dialogue Runner reputation branching (excerpt)
`Assets/Ink/Gameplay/Dialogue/DialogueRunner.cs` uses:
- Fields: `factionId`, `friendlySequence`, `hostileSequence`, `friendlyThreshold`, `hostileThreshold`.
- In `SelectSequence()`, after quest checks:
```csharp
int rep = ReputationSystem.GetRep(factionId);
if (rep >= friendlyThreshold && friendlySequence != null) chosen = friendlySequence;
else if (rep <= hostileThreshold && hostileSequence != null) chosen = hostileSequence;
```
- Input handling uses the new Input System (`Keyboard.current`/`Mouse.current`) to advance, so dialogue remains compatible with the project input settings.

#### EnemyFactory faction-aware spawn (excerpt)
`Assets/Ink/Gameplay/Enemies/EnemyFactory.cs`:
- Signatures: `Spawn(..., FactionDefinition faction = null, string factionRankId = null)` and `SpawnFromData(..., FactionDefinition faction = null, string factionRankId = null)`.
- After basic enemy setup:
```csharp
if (faction != null)
{
    var member = go.AddComponent<FactionMember>();
    member.faction = faction;
    member.rankId = string.IsNullOrEmpty(factionRankId) ? "low" : factionRankId;
    member.ApplyRank();
}
```

#### TestMapBuilder integration (excerpt)
`Assets/Ink/TestMapBuilder.cs`:
- Loads factions: `Resources.Load<FactionDefinition>("Factions/Inkbound")`, `"Factions/Inkguard"`.
- `CreateNPC(..., string merchantId = null, FactionDefinition faction = null, string factionRankId = "low")` attaches `FactionMember` and calls `ApplyRank()` when `faction` is provided; otherwise uses quest dialogue fallback.
- Current NPCs: forest merchant → Inkbound low rank; dungeon weaponsmith → Inkguard mid rank.
- `CreateEnemy` still spawns without faction by default; to use factions, supply the optional parameters when calling `EnemyFactory.Spawn` or extend `CreateEnemy` similarly.

#### Other supporting assets/notes
- Default level profile referenced: `Assets/Ink/Data/Levels/DefaultLevelProfile.asset`.
- Fireball spell asset referenced by Inkbound: `Assets/Ink/Resources/Spells/Fireball.asset` (GUID `8cf77dd05f89f4b5991182ca21ea627e`).
- Sprite indices rely on `SpriteLibrary` populated in `TestMapBuilder.CreateSpriteLibrary()`.
- Merchant/quest dialogues that bypass faction defaults: `CreateNPC` still wires quest-specific dialogue sequences if no faction is provided; with faction present, dialogue is expected to be set on the rank (currently null in the sample assets).
- Reputation does not persist to `GameState`; if you need persistence, add rep fields to `GameState` and wire into `GameStateManager` collect/apply.
- Faction spells only apply when the entity has a `SpellSystem` component; most NPCs/enemies currently do not, so only entities with spells will benefit from the `defaultSpells` list.
- Equipment application assumes `ItemDatabase` IDs exist (`sword`, `iron_armor`, etc.); missing IDs are safely skipped.
- Sprites resolve through `SpriteLibrary.Instance`; ensure it is constructed (TestMapBuilder calls `CreateSpriteLibrary()` early).

### Additional context and integration points
- **Level profiles**: If no faction/rank profile override is set, `FactionMember` falls back to the faction’s `defaultLevelProfile` or the existing `Levelable.profile`, then calls `SetLevel` to recalc stats. `EnemyFactory` still assigns the default level profile (`Assets/Ink/Data/Levels/DefaultLevelProfile.asset`) when spawning; the faction/rank can override that.
- **Dialogue fallback**: When `CreateNPC` is called without a faction, it wires quest-specific dialogue (merchant offer/turn-in). When a faction is present, dialogue should be provided via the rank fields; otherwise the NPC will have no dialogue unless you set it manually on `DialogueRunner`.
- **Save/Load**: Reputation and faction membership are not serialized into `GameState`; saving/loading will reset reputation to defaults and won’t reapply faction/rank automatically unless you extend `GameStateManager` to store factionId/rankId and reputation values.
- **Spells and components**: `defaultSpells` only take effect if the entity has a `SpellSystem`. NPCs/enemies without `SpellSystem` will ignore the `defaultSpells` list.
- **Meta/assets**: Faction assets live under `Assets/Ink/Resources/Factions/` so they can be loaded at runtime via `Resources.Load`. Their `.meta` files are generated by Unity and not listed here, but are present alongside the assets.
- **Extending enemies/NPCs**: Currently `CreateEnemy` in `TestMapBuilder` doesn’t pass faction/rank; to use factions for enemies, update calls to `EnemyFactory.Spawn` with the optional faction parameters or extend `CreateEnemy` to accept and forward them.
- **Reputation-based behaviors**: Only dialogue branches by reputation right now. If you want AI stance, prices, or quest gating to depend on rep, add checks against `ReputationSystem.GetRep(factionId)` in those systems.
- **Error handling**: `FactionMember.ApplyRank` logs a warning if `faction` is missing; equipment/spell application silently skips invalid IDs/spells. DialogueRunner will fall back to defaults if rep-based sequences are unset.
- **Assets/Tile indices**: Inkbound uses sprite index 9 (`Assets/Ink/Tiles/tile_0009.png`); Inkguard uses index 15 (`Assets/Ink/Tiles/tile_0015.png`). Ensure these exist in `SpriteLibrary`.
- **Spell assets**: Fireball lives at `Assets/Ink/Resources/Spells/Fireball.asset` and is referenced in Inkbound ranks; if removed/renamed, update faction assets.
- **Meta files**: `.meta` files for factions/level profiles/spells are present alongside assets; Unity will regenerate if missing, but GUIDs are currently relied on in YAML.
- **Example usage (code)**:
  - Spawn an enemy with faction/rank: `EnemyFactory.Spawn("slime", x, y, GridWorld.Instance, 1, Resources.Load<FactionDefinition>("Factions/Inkbound"), "low");`
  - Attach to NPC in-scene: add `FactionMember`, assign `Inkguard` asset, set `rankId = "mid"`, then call `ApplyRank()` (or let your spawn/setup helper call it).
  - DialogueRunner faction-aware: set `factionId = faction.id` and assign `friendlySequence`/`hostileSequence` to enable rep branching.
