# Object Pooling Ideas

## High ROI (combat/runtime hot path)
- Damage numbers: pool `DamageNumber` objects (Canvas + Text) instead of creating/destroying per hit.
  - Paths: `Assets/Ink/Gameplay/DamageNumber.cs`
- Projectiles: pool `Fireball` (and its Glow child + TrailRenderer) and reuse instead of destroying after impact.
  - Paths: `Assets/Ink/Gameplay/Spells/Fireball.cs`, `Assets/Ink/Gameplay/Spells/Projectile.cs`
- Impact/particle effects: pool impact particles and ink splatter/drip particles.
  - Paths: `Assets/Ink/Gameplay/Spells/SpellVisuals.cs`, `Assets/Ink/Gameplay/Spells/InkVisuals.cs`
- InkStream: pool the `InkStream` and its TipBlob, plus its drip/splatter particles.
  - Paths: `Assets/Ink/Gameplay/Spells/InkStream.cs`
- InkPuddle: pool puddles and avoid `FindObjectsOfType` in `Create`.
  - Paths: `Assets/Ink/Gameplay/Spells/InkPuddle.cs`

## Medium ROI (if frequent)
- Item drops: pool item pickup GameObjects when lots of loot is spawned/destroyed.
  - Paths: `Assets/Ink/Gameplay/Items/ItemPickup.cs`

## Low ROI (UI opened occasionally)
- UI canvases that are created/destroyed on open/close: keep cached and toggle active.
  - Paths: `Assets/Ink/Gameplay/InventoryUI.cs`, `Assets/Ink/Gameplay/MerchantUI.cs`,
    `Assets/Ink/Gameplay/SaveLoad/SaveLoadMenu.cs`

## Already pooled
- Reputation toasts already use pooling.
  - Paths: `Assets/Ink/Gameplay/UI/ReputationToastManager.cs`
