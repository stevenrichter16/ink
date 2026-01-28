# TODO (Foundations for the ink-based roguelike)

- Data-driven content
  - Move spells, items, factions, NPC archetypes, tiles into ScriptableObjects/JSON with stable IDs.
  - Add content tags (e.g., toxic, fire, metallic, fungal) for generators/AI queries.

- Status/Effect framework
  - Central StatusEffect interface (duration, stacks, tick timing, conflict rules).
  - Integrate with GridEntity.TakeDamage and SpellSystem; avoid per-spell hardcoding.
  - Define key statuses: Burn, Wet, Freeze, Shock, Poison, Bleed, Slow, Stun.

- Sensing & AI awareness
  - Implement sight FOV with occlusion; sound and scent traces.
  - Feed PerceptionResult into AI to trigger Alert/Hostile on heard fights.

- Event/Combat log
  - Central dispatcher with categories (combat, reputation, loot, dialogue).
  - UI/log viewer; support filtering for debugging and player readability.

- Procedural map & biomes
  - BiomeDefinition (tileset, spawn tables, hazards, ambient effects).
  - Worldgen pipeline: layout → features (pools/vents) → props → faction seeding per biome.

- Object pooling & performance
  - Pool projectiles, damage numbers, spell VFX.
  - Cap simultaneous projectiles per source; keep perf counters visible in builds.

- Interaction verbs
  - Core verbs beyond attack: open, lockpick, harvest, dismantle, read, consume.
  - TileInfoPanel pulls from verb providers so new objects inject actions without UI rewrites.

- Lore & naming seeds
  - Procedural name generator (syllable lists per faction/biome).
  - “Placards”/lore blurbs attachable to tiles or items.

- Persistence hardening
  - Stable GUIDs for entities; versioned save schema; migration hooks.

- Progression tracks
  - “Ink mutations”/schools with passive nodes.
  - Spells can query unlocked nodes to scale/add keywords.
