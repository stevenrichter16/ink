# Economic System Prerequisites — Foundation Plan

This document lists the groundwork to finish before implementing the economic system. Each prerequisite is scoped so we can layer pricing, taxes, and palimpsest edicts on top without rework.

---

## 1) Faction Identity & Relations
- Finalize faction IDs/display names/dispositions; ensure `FactionRegistry` loads all.
- Verify Species → defaultFaction is set across species assets; fallback path in `FactionMember` remains intact.
- Inter‑faction reputation: add an API (e.g., `ReputationSystem.SetInterRep(src,dst,val)`) and storage; keep player↔faction rep separate. Tests per `TDD.md`.
- Decide how faction standing affects prices (multiplier curve, thresholds) and document it.

## 2) Territory & District Baseline
- Lock district definitions (IDs, bounds, names) in `Resources/Districts`.
- Provide `DistrictControlService.GetDistrictAt(position)` (or equivalent) that is reliable.
- Add baseline economic fields to `DistrictState`: prosperity (float), scarcity per category (map), tax override slots (sales/import/export).
- Daily tick exists and is called; hooks to mark districts dirty.

## 3) Item & Merchant Schema
- Items: unique id, base value, category (weapon/armor/food/mat), rarity, weight.
- Merchants: `MerchantProfile` with buy/sell multipliers, accepted categories, faction, home district, inventory stub.
- Ensure item DB and merchant data are accessible without scene dependencies (ScriptableObjects in Resources).

## 4) Price Resolver Contract
- Fix the modifier order and clamping: Merchant → District econ → Taxes → Faction standing → Palimpsest → Supply/Demand.
- Define an interface:
  ```csharp
  int ResolveBuyPrice(string itemId, Merchant m, Vector2Int location);
  int ResolveSellPrice(...);
  PriceBreakdown ResolveDetailed(...); // for debug UI
  ```
- Clamp ranges per modifier (e.g., 0.5–2.0 per layer) and a final clamp (min 1).

## 5) Palimpsest Hooks
- Token registry entries for economy: `TAX:<pct>`, `DUTY:<pct>`, `DISCOUNT:<faction>`, `PRICEFLOOR:<pct>`, `DEMAND:<cat:+/-x%>`, etc.
- OverlayResolver should expose an economic query returning aggregated modifiers for a tile/district.
- Mark district econ dirty when layers are added/removed; decay handled via `OverlayResolver.TickDecay`.

## 6) Officials / Policies Skeleton
- Define `EconomicPolicy` struct (type, scope, value, duration/source).
- Define minimal `OfficialDefinition` assets (Tax Collector district-level) to own policies.
- Daily tick: decay temporary policies/influence; notify listeners when policies change.
- Bribery/influence can be stubbed but API should exist (`ApplyPolicyOverride`, `ClearOverride`).

## 7) Supply / Demand Placeholder
- Per-item (or per-category) availability scalar per district; default 1.0.
- Daily regeneration/decay stub; API `GetSupplyDemandModifier(district, itemId)`.
- Hook to consumption/production events later; for now manual/debug controls are fine.

## 8) Data Access & Persistence
- Decide runtime storage for econ state (policies, supply, taxes) separate from ScriptableObject defaults.
- Save/load: extend save system to serialize econ runtime state and inter-rep map.
- Invalidation events: when econ state changes, fire an event to flush price caches.

## 9) Debug / Inspection Tools
- Price breakdown debug UI for a selected item/location (shows each multiplier).
- District econ inspector (prosperity, taxes, supply) tied to F8 or a separate hotkey.
- Logging guards (enable/disable) to avoid spam in playtests.

## 10) Testing Readiness
- Unit tests for:
  - PriceResolver order/clamps
  - Inter-rep API
  - District tax/prosperity multipliers
  - Palimpsest econ token application
- Integration tests:
  - Overlay layer changes affect price in a district
  - Official policy change affects tax in price resolver
  - Save/load restores econ state

---

## Suggested First Implementation Slice (after prerequisites)
1) Implement inter-faction rep API + tests.
2) Add district econ fields and expose `GetDistrictAt`.
3) Create `MerchantProfile` and minimal price resolver with Merchant + District + Tax + Faction multipliers (no supply/palimpsest yet).
4) Stub supply/demand to 1.0 and add debug UI for price breakdown.
5) Wire palimpsest economic tokens to adjust a simple tax/discount multiplier in resolver.

This staged approach keeps early slices small and testable while leaving room to add officials, deeper supply/demand, and full palimpsest economy later.
