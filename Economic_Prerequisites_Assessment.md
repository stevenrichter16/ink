# Economic System Prerequisites Assessment

## Evaluation of Recommended Prerequisites vs. Current Codebase

This document evaluates the proposed prerequisite plan against what actually exists in your codebase, identifying what's done, what's partial, and what's missing.

---

## 1. Stable Faction Identity & Relations

### Proposed:
> Finalize faction IDs, display names, and dispositions; ensure FactionRegistry and Species → defaultFaction assignment are consistent. Decide how inter-faction relationships are stored.

### Current State: ✅ **DONE**

| Requirement | Status | Evidence |
|-------------|--------|----------|
| Faction IDs finalized | ✅ Done | 8 factions in `Resources/Factions/` |
| Display names set | ✅ Done | `FactionDefinition.displayName` on all |
| Dispositions defined | ✅ Done | `FactionDefinition.FactionDisposition` (Calm/Aggressive) |
| FactionRegistry works | ✅ Done | `FactionRegistry.cs:1-114` with `GetById()`, `GetByName()`, `GetOrCreate()` |
| Species → defaultFaction | ✅ Done | `SpeciesDefinition.defaultFaction` + `SpeciesMember.EnsureDefaultFaction()` |
| Inter-faction rep storage | ✅ Done | `ReputationSystem.GetInterRep()` / `SetInterRep()` |
| Events for changes | ✅ Done | `OnRepChanged`, `OnInterRepChanged` events |

**Existing Factions:**
- Inkguard, Inkbound, InkboundScribes, Ghost, Slime, Snake, Unknown, TestFaction

**Verdict:** This prerequisite is complete. No work needed.

---

## 2. Territory & District Baseline

### Proposed:
> Lock down district definitions (IDs, bounds, names). Have a minimal DistrictState with prosperity/scarcity placeholders. Reliable lookup GetDistrictAt(position).

### Current State: ⚠️ **PARTIAL**

| Requirement | Status | Evidence |
|-------------|--------|----------|
| District IDs/names | ✅ Done | 3 districts: MarketRow, OuterSlums, TempleWard |
| District bounds | ✅ Done | `DistrictDefinition.minX/maxX/minY/maxY` + `Contains(x,y)` |
| DistrictState exists | ✅ Done | `DistrictState.cs` with control/patrol/heat arrays |
| Prosperity field | ⚠️ Missing | `economicValue` exists on Definition but no runtime `prosperity` |
| Scarcity placeholder | ❌ Missing | No per-item supply tracking |
| GetDistrictAt(position) | ✅ Done | `DistrictControlService.GetStateByPosition(x, y)` |

**Gap Analysis:**
```csharp
// Current DistrictState.cs has:
public float[] control;
public float[] patrol;
public float[] heat;
public int[] lossStreak;

// Missing for economy:
public float prosperity;           // Runtime prosperity level
public Dictionary<string, float> itemSupply;  // Per-item supply levels
```

**Work Needed:** ~15 lines to add `prosperity` and basic supply tracking to `DistrictState`.

---

## 3. Item & Merchant Schema

### Proposed:
> Items: unique IDs, base value, category, rarity, weight. Merchants: profile with buy/sell multipliers, accepted categories, faction, home district.

### Current State: ⚠️ **PARTIAL**

| Requirement | Status | Evidence |
|-------------|--------|----------|
| Item unique IDs | ✅ Done | `ItemData.id` on all 11 items |
| Base value | ✅ Done | `ItemData.value` (sword=50, potion=15, etc.) |
| Category/type | ✅ Done | `ItemType` enum (Weapon, Armor, Consumable, Currency, KeyItem, Accessory) |
| Rarity | ❌ Missing | No `rarity` field |
| Weight | ❌ Missing | No `weight` field |
| Merchant buy/sell multipliers | ✅ Done | `MerchantProfile.buyMultiplier`, `sellMultiplier` |
| Accepted categories | ❌ Missing | No category filtering |
| Merchant faction | ❌ Missing | No faction link on MerchantProfile |
| Merchant home district | ❌ Missing | No district link on MerchantProfile |
| Inventory | ✅ Done | Full `Inventory.cs` with all operations |

**Gap Analysis:**
```csharp
// Current ItemData.cs has:
public int value;
public ItemType type;

// Missing for full economy:
public ItemRarity rarity;  // Common, Uncommon, Rare, etc.
public float weight;       // For encumbrance (optional)

// Current MerchantProfile.cs has:
public float buyMultiplier;
public float sellMultiplier;

// Missing for full economy:
public string factionId;              // Which faction owns this merchant
public string homeDistrictId;         // Which district they operate in
public List<ItemType> acceptedTypes;  // Categories they buy/sell
```

**Work Needed:**
- ItemData: ~5 lines (rarity enum + field) - **optional for simple version**
- MerchantProfile: ~10 lines (faction, district, categories) - **recommended**

---

## 4. Price Resolver Contract

### Proposed:
> Freeze the modifier order and clamping rules. Define API signatures so callers don't change later.

### Current State: ❌ **NOT STARTED**

| Requirement | Status | Evidence |
|-------------|--------|----------|
| Modifier order defined | ❌ Missing | No central resolver |
| Clamping rules | ⚠️ Partial | `Mathf.Max(1, ...)` in `MerchantProfile` only |
| API signatures | ❌ Missing | No `EconomicPriceResolver` |

**Current Flow:**
```csharp
// MerchantProfile.GetBuyPrice() - VERY simple
public int GetBuyPrice(string itemId)
{
    var data = ItemDatabase.Get(itemId);
    return Mathf.Max(1, Mathf.RoundToInt(data.value * buyMultiplier));
}
```

**Needed Flow:**
```csharp
// EconomicPriceResolver.ResolveBuyPrice() - Full pipeline
BasePrice → MerchantMultiplier → DistrictProsperity → TaxRate
→ FactionDiscount → PalimpsestModifiers → SupplyDemand → Clamp
```

**Work Needed:** ~60 lines for `EconomicPriceResolver.cs` with defined modifier order.

**Recommendation:** This is a **critical prerequisite**. Define the contract before implementing features.

---

## 5. Palimpsest Integration Hooks

### Proposed:
> Token registry entries for economic effects. OverlayResolver entry point that returns economic modifiers.

### Current State: ⚠️ **PARTIAL**

| Requirement | Status | Evidence |
|-------------|--------|----------|
| Token registry exists | ✅ Done | `PalimpsestTokenRegistry.cs` ScriptableObject |
| Token parsing works | ✅ Done | `OverlayResolver.ParseTokens()` |
| Economic tokens | ❌ Missing | Only TRUCE, ALLY:, HUNT: exist |
| Economic modifiers returned | ❌ Missing | `PalimpsestRules` has no economic fields |
| District dirty marking | ❌ Missing | No cache invalidation |

**Current PalimpsestRules:**
```csharp
public struct PalimpsestRules
{
    public bool truce;
    public string allyFactionId;
    public string huntFactionId;
    // NO economic fields
}
```

**Needed:**
```csharp
public struct PalimpsestRules
{
    // Existing
    public bool truce;
    public string allyFactionId;
    public string huntFactionId;

    // NEW: Economic
    public float taxModifier;        // +/- to base tax
    public float priceMultiplier;    // Applied to all prices
    public string subsidizedItemId;  // Specific item discount
    public float subsidyRate;        // Amount of discount
}
```

**Work Needed:**
- Add fields to `PalimpsestRules`: ~10 lines
- Add parsing in `OverlayResolver.ParseTokens()`: ~40 lines
- Add token definitions to registry asset: Inspector work

---

## 6. Officials/Policies Skeleton

### Proposed:
> A minimal EconomicPolicy data structure. An OfficialDefinition list. A daily tick that could decay temporary policy effects.

### Current State: ❌ **NOT STARTED**

| Requirement | Status | Evidence |
|-------------|--------|----------|
| EconomicPolicy struct | ❌ Missing | Does not exist |
| OfficialDefinition | ❌ Missing | Does not exist |
| Daily tick for decay | ⚠️ Partial | `DistrictControlService.AdvanceDay()` exists but no policy decay |

**Work Needed:**

```csharp
// New: EconomicPolicy.cs (~20 lines)
public class EconomicPolicy
{
    public string id;
    public PolicyType type;        // Tax, Subsidy, Embargo, etc.
    public float value;            // Rate/amount
    public string districtId;      // null = global
    public string factionId;       // null = all factions
    public int turnsRemaining;     // -1 = permanent
}

// New: OfficialDefinition.cs (~30 lines)
[CreateAssetMenu(menuName = "Ink/Economy/Official")]
public class OfficialDefinition : ScriptableObject
{
    public string id;
    public string title;           // "Tax Collector"
    public string displayName;     // "Marcus"
    public string districtId;
    public string factionId;
    public float corruptibility;
    public List<EconomicPolicy> policies;
}

// New: PolicyRegistry.cs (~40 lines)
public static class PolicyRegistry
{
    private static List<EconomicPolicy> _policies;
    public static void TickDecay();
    public static float GetTaxRate(string districtId);
}
```

**Work Needed:** ~90 lines total for skeleton

**Recommendation:** For a **simple version**, skip officials and just have `TaxRegistry` (~30 lines).

---

## 7. Supply/Demand Placeholder

### Proposed:
> Per-item per-district availability scalar with a daily decay/regeneration loop, even if it's just constant 1.0 initially.

### Current State: ❌ **NOT STARTED**

| Requirement | Status | Evidence |
|-------------|--------|----------|
| Per-item supply tracking | ❌ Missing | No supply system |
| Per-district tracking | ❌ Missing | DistrictState has no item data |
| Daily regeneration | ❌ Missing | No supply tick |

**Simple Placeholder (~30 lines):**
```csharp
public static class SupplyService
{
    // districtId -> itemId -> supply level (0.0 to 2.0, 1.0 = normal)
    private static Dictionary<string, Dictionary<string, float>> _supply = new();

    public static float GetSupply(string districtId, string itemId)
    {
        if (_supply.TryGetValue(districtId, out var items))
            if (items.TryGetValue(itemId, out var level))
                return level;
        return 1.0f;  // Default: normal supply
    }

    public static void ModifySupply(string districtId, string itemId, float delta)
    {
        // ... modify and clamp
    }

    public static void TickRegeneration()
    {
        // Slowly return all supplies toward 1.0
    }
}
```

**Recommendation:** For simple version, **skip this** and add later. Use constant 1.0.

---

## 8. Data Access & Persistence

### Proposed:
> Decide where economic state lives (ScriptableObject defaults + runtime state) and how it's saved/loaded. Event hooks to invalidate cached prices.

### Current State: ⚠️ **PARTIAL**

| Requirement | Status | Evidence |
|-------------|--------|----------|
| Save/load system | ✅ Done | `GameState.cs`, `SaveSystem.cs` |
| Economic state in save | ❌ Missing | `GameState` has no economic data |
| Cache invalidation | ❌ Missing | No price caching exists yet |

**Current GameState includes:**
- Player position, stats, inventory, equipment
- Enemy positions and states
- Ground items
- Quest progress

**Missing:**
```csharp
// Add to GameState.cs
public List<EconomicPolicySaveData> policies;
public Dictionary<string, float> districtProsperity;
public Dictionary<string, Dictionary<string, float>> itemSupply;
public List<PalimpsestLayerSaveData> palimpsestLayers;
```

**Work Needed:**
- Add save/load for economic state: ~50 lines
- For simple version: **defer persistence** (in-memory only, like reputation currently)

---

## 9. Debug/Inspection Tools

### Proposed:
> A simple "Price Breakdown" debug UI for a selected item/location. Logs or overlays to visualize district prosperity/taxes.

### Current State: ⚠️ **PARTIAL**

| Requirement | Status | Evidence |
|-------------|--------|----------|
| Debug panel infrastructure | ✅ Done | `TerritoryDebugPanel.cs` shows C/P/H |
| Price breakdown UI | ❌ Missing | No price inspection |
| District prosperity overlay | ❌ Missing | Only control/patrol/heat shown |

**TerritoryDebugPanel already shows:**
- Control, Patrol, Heat per faction per district
- Buttons for Next Day, Apply Edit, Cleanup
- Auto-refresh every 0.5s

**Would need to add:**
- Tax rate display per district
- Prosperity display
- Price breakdown for selected item

**Work Needed:** ~100 lines to extend `TerritoryDebugPanel` with economic info.

---

## Summary Assessment

| Prerequisite | Status | Work Needed |
|--------------|--------|-------------|
| 1. Faction Identity | ✅ Done | 0 lines |
| 2. Territory Baseline | ⚠️ Partial | ~15 lines (add prosperity) |
| 3. Item/Merchant Schema | ⚠️ Partial | ~15 lines (faction/district on merchant) |
| 4. Price Resolver Contract | ❌ Missing | **~60 lines (critical)** |
| 5. Palimpsest Hooks | ⚠️ Partial | ~50 lines (economic tokens) |
| 6. Officials/Policies | ❌ Missing | ~90 lines (or ~30 for simple TaxRegistry) |
| 7. Supply/Demand | ❌ Missing | ~30 lines (or skip for simple) |
| 8. Persistence | ⚠️ Partial | ~50 lines (or defer) |
| 9. Debug Tools | ⚠️ Partial | ~100 lines (extend existing panel) |

---

## My Recommendation: Phased Approach

### Phase 0: Define the Contract (Do First)
**~80 lines total**

1. **Create `EconomicPriceResolver.cs`** with stub implementations
   - Define the modifier order
   - Define clamping rules
   - Return base price for now

```csharp
public static class EconomicPriceResolver
{
    public static int ResolveBuyPrice(string itemId, Merchant merchant, Vector2Int pos)
    {
        float price = ItemDatabase.Get(itemId).value;

        // 1. Merchant markup
        price *= merchant.Profile.buyMultiplier;

        // 2. District prosperity (stub: 1.0)
        price *= GetProsperityModifier(pos);

        // 3. Tax rate (stub: 0.1)
        price *= (1f + GetTaxRate(pos));

        // 4. Faction discount
        price *= GetFactionModifier(merchant);

        // 5. Palimpsest effects (stub: 1.0)
        price *= GetPalimpsestModifier(pos);

        // 6. Supply/demand (stub: 1.0)
        price *= GetSupplyModifier(pos, itemId);

        return Mathf.Clamp(Mathf.RoundToInt(price), 1, 99999);
    }

    // Stub implementations that return 1.0 or 0.1
    private static float GetProsperityModifier(Vector2Int pos) => 1.0f;
    private static float GetTaxRate(Vector2Int pos) => 0.1f;
    private static float GetFactionModifier(Merchant m) => 1.0f;
    private static float GetPalimpsestModifier(Vector2Int pos) => 1.0f;
    private static float GetSupplyModifier(Vector2Int pos, string itemId) => 1.0f;
}
```

2. **Wire `Merchant.GetBuyPrice()` to use it** (~5 lines change)

This locks in the API so all future work fills in stubs.

### Phase 1: Palimpsest Economic Tokens
**~60 lines**

1. Add economic fields to `PalimpsestRules`
2. Add token parsing for TAX:, TAX_BREAK:, SUBSIDY:
3. Implement `GetPalimpsestModifier()` in resolver

**Result:** Players can inscribe economic effects

### Phase 2: Tax System
**~40 lines**

1. Create simple `TaxRegistry` (no officials)
2. Implement `GetTaxRate()` in resolver
3. Add tax display to debug panel

**Result:** Districts have tax rates

### Phase 3: Faction Discounts
**~20 lines**

1. Implement `GetFactionModifier()` using `ReputationSystem.GetRep()`

**Result:** Friendly factions give discounts

### Phase 4: UI Integration
**~200 lines**

1. Add Economy tab to Ledger
2. Add inscription dialog
3. Hook up ink costs

---

## Critical Path

```
                    ┌────────────────────────┐
                    │  EconomicPriceResolver │  ◄── DO THIS FIRST
                    │  (define contract)     │
                    └───────────┬────────────┘
                                │
            ┌───────────────────┼───────────────────┐
            ▼                   ▼                   ▼
   ┌─────────────────┐ ┌─────────────────┐ ┌─────────────────┐
   │ Palimpsest      │ │ TaxRegistry     │ │ FactionModifier │
   │ (tokens/rules)  │ │ (simple rates)  │ │ (rep-based)     │
   └────────┬────────┘ └────────┬────────┘ └────────┬────────┘
            │                   │                   │
            └───────────────────┼───────────────────┘
                                │
                                ▼
                    ┌────────────────────────┐
                    │  Ledger UI             │
                    │  (inscriptions)        │
                    └────────────────────────┘
```

---

## Verdict on the Proposed Plan

The proposed prerequisite plan is **sound and well-structured**. My assessment:

| Aspect | Verdict |
|--------|---------|
| **Scope** | Comprehensive, maybe too much for "simple" version |
| **Order** | Good - contracts before features |
| **Gaps identified** | Accurate - officials/supply are real gaps |
| **Risk** | Low if done incrementally |

**My modification:** For a **simple version**, I'd skip:
- Rarity/weight on items (not needed for pricing)
- Full OfficialDefinition (use TaxRegistry stub)
- Supply/Demand (constant 1.0 is fine)
- Persistence (in-memory like reputation)

This reduces ~350 lines down to ~200 lines while still delivering:
- Palimpsest-driven price modifiers ✓
- District tax rates ✓
- Faction discounts ✓
- Ledger inscription UI ✓

The plan is good. Start with **Phase 0 (contract)** and iterate.
