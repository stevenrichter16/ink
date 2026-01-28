# NPC–Faction Refactor Notes

## Recommendation
Keep composition and separate “species” from “faction.” This cleanly supports: any NPC can be hostile, and species ≠ faction (species provides defaults; faction is per-instance and overrideable). Making faction an IS-A NPC relationship makes this harder because you also want enemies to have factions.

This refactor is intended to remove strict distinctions between Enemies, NPCs, and Faction members. Instead, any entity can become hostile based on faction relations and reputation; “enemy” becomes a behavior/state, not a fixed class.

## Implications in the Codebase
- Keep `FactionMember` (or rename to `FactionAffiliation`) as a component that can live on any `GridEntity` (NPCs, enemies, even player).
- Note: rename `FactionMember` to `FactionAffiliation` for clarity once integration is stable.
- Add a species tag (string or `SpeciesDefinition` ScriptableObject). Species can suggest a default faction, but any instance can override its faction.
- Replace entity-type checks (Enemy vs NPC) with hostility checks (faction relations + optional reputation).
- Treat “enemy” as behavior (aggressive/hostile state), not a separate class of entity.

## Practical Shape (Minimal Refactor)
- `SpeciesDefinition` (new SO): `id`, `displayName`, `defaultFactionId`, maybe `baseAggression`.
- `FactionAffiliation` (current `FactionMember`): per-instance faction, rank, and defaults. Can override species defaults.
- `HostilityService` (new static): `IsHostile(attacker, target)` using faction relations + rep thresholds + overrides.
- AI targeting: `EnemyAI` and `NpcAI` both use `HostilityService` instead of `entityType`.
- Player attacks / projectiles: damage any `GridEntity` that is hostile, not just `EnemyAI`.

## Why This Works
- Slimes/demons can default to a “species faction.”
- Individual slimes/demons can join any other faction.
- Any NPC can become hostile without forcing a class hierarchy refactor.

## Future Consideration
- Add support for entities belonging to multiple factions simultaneously.
