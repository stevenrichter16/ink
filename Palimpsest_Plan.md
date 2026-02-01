# Palimpsest Feature Rollout Plan

## Phase 1 — MVP Loop (Inscribe → Effect → Decay)
- **Surfaces & Ink**
  - Add `InscribableSurface` MonoBehaviour to walls/signs/parchment with radius/allowedTokens.
  - Create “Palimpsest Ink” consumable + use action to spawn a `PalimpsestLayer` at the surface and register with `OverlayResolver`.
  - Add an erase/clean action to unregister or halve `turnsRemaining`.
- **Tick & Decay**
  - Call `OverlayResolver.TickDecay()` from the world/turn loop with a tunable turns-per-tick.
  - Clamp max active layers and log creation/expiration.
- **Token Registry**
  - Introduce `PalimpsestTokenRegistry` ScriptableObject mapping token → rule effect (truce/ally/hunt, etc.).
  - Move token parsing out of `PalimpsestLayer` into the registry.
- **Feedback**
  - Add a “Layer Viewer” debug overlay (hotkey) that shows layers at cursor, tokens, remaining turns, and effective rules.
  - User-facing logs when layers are created, decay, or conflict.

## Phase 2 — Safety & Conflict
- **Anchors**
  - Tag critical areas/factions with `AnchorZone` (allowedTokens, resistance).
  - In `OverlayResolver`, dampen/ignore layer effects that intersect anchors.
- **Conflict & Heat MVP**
  - Track per-chunk heat when overlapping layers assert incompatible rules (e.g., TRUCE vs HUNT).
  - On heat threshold, trigger a small anomaly or accelerated decay.
- **Save/Load**
  - Serialize active `PalimpsestLayer`s (id, pos, radius, tokens, priority, turnsRemaining) and re-register on load.
  - Derive RNG seeds from (worldSeed, layerId) for determinism.

## Phase 3 — Cross-System Proof
- **Extra Soft Knob**
  - Add one non-hostility token (e.g., `TAX` price multiplier or `CALM_ZONE` to shrink aggression radius).
- **Authoring Aids**
  - Scriptable “Decree” items that ship with preset token sets (ALLY:PLAYER, TRUCE, etc.).
  - Inspector button on `InscribableSurface` to preview effective rules in play mode.
- **UI Polish**
  - Overlay color legends, tooltip with top contributing layers and source provenance.

## Phase 4 — Testing & Hardening
- **Unit Tests**
  - `OverlayResolver` stacking, priority, anchor clipping, decay timing.
  - Token registry mapping correctness.
  - Conflict → heat threshold behavior.
- **Integration Test**
  - Inscribe TRUCE near hostiles; verify `HostilityService` changes; allow decay and confirm aggression returns.
- **Perf & Limits**
  - Load-test layer counts; profile decay/update frequency; tune caps and pooling.

## Implementation Order (bite-sized tasks)
1. Data: `PalimpsestTokenRegistry` asset + wire `PalimpsestLayer` to use it.
2. Runtime: `InscribableSurface`, ink item actions, layer registration/erase.
3. Tick: hook `TickDecay` into turn/world loop; add max-layer guard + logging.
4. Debug UI: layer viewer overlay + log lines.
5. Anchors + conflict/heat pass.
6. Save/load of layers.
7. Extra soft knob (economy or calm zone) + decree items.
8. Tests (unit + integration) and perf pass.

## Definitions (for reference)
- **Layer**: center, radius, tokens, priority, turnsRemaining.
- **Rule resolution**: `OverlayResolver.GetRulesAt(x, y)` aggregates layers by priority, consults token registry, applies anchor constraints, returns rules (truce/ally/hunt + future knobs).
