# Economic System Implementation Prerequisites Analysis

## Executive Summary

After analyzing the codebase, implementing a **simple version** of the Economic System is highly feasible. Most foundational infrastructure already exists. This document identifies what's ready, what needs minor extension, and what's missing.

---

## 1. Systems Ready to Use (No Changes Needed)

### ✅ Item Value System
**Location:** `ItemData.cs:29`
```csharp
public int value;  // Already exists on all items
```
- All 11 items already have `value` set (sword=50, potion=15, etc.)
- This is the base price for all calculations

### ✅ Merchant Price Multipliers
**Location:** `MerchantProfile.cs:17-21`
```csharp
public float buyMultiplier = 1.0f;   // What player pays
public float sellMultiplier = 0.5f;  // What player receives
```
- Two merchants exist: General Store (1.0/0.5) and Weaponsmith (1.2/0.6)
- Perfect hook point for additional modifiers

### ✅ Transaction Service
**Location:** `MerchantService.cs:15-91`
- `TryBuy()` and `TrySell()` handle all transactions
- Easy to inject price modifiers before line 28 (`int unitPrice = merchant.GetBuyPrice(itemId)`)

### ✅ Inventory System
**Location:** `Inventory.cs:1-195`
- Full item tracking with `CountItem()`, `HasItem()`, `AddItem()`, `RemoveItem()`
- Events: `OnChanged`, `OnItemAdded`, `OnItemRemoved`
- Can track "ink" resource once added to ItemDatabase

### ✅ Reputation System
**Location:** `ReputationSystem.cs:1-77`
- Player↔Faction rep: `GetRep()`, `SetRep()`, `AddRep()`
- Inter-faction rep: `GetInterRep()`, `SetInterRep()`, `AddInterRep()`
- Events: `OnRepChanged`, `OnInterRepChanged`
- Can drive faction discounts directly

### ✅ District System
**Location:** `DistrictDefinition.cs:1-27`, `DistrictState.cs:1-55`
```csharp
public float economicValue;  // Already exists per district
public bool Contains(int x, int y);  // Position lookup works
```
- `DistrictControlService.GetStateByPosition(x, y)` returns district for any tile

### ✅ Palimpsest Layer Registration
**Location:** `OverlayResolver.cs:22-29`
```csharp
public static int RegisterLayer(PalimpsestLayer layer)
{
    layer.id = _nextId++;
    ParseTokens(layer);
    _layers.Add(layer);
    return layer.id;
}
```
- Layers auto-decay via `TickDecay()`
- Position lookup via `GetRulesAt(x, y)`

---

## 2. Systems Needing Minor Extension

### 🔧 PalimpsestRules Struct (Add Economic Fields)
**Location:** `OverlayResolver.cs:15-20`

**Current:**
```csharp
public struct PalimpsestRules
{
    public bool truce;
    public string allyFactionId;
    public string huntFactionId;
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

    // NEW: Economic effects
    public float taxModifier;        // Added to base tax (e.g., -0.10 = 10% reduction)
    public float priceMultiplier;    // Multiplied with prices (e.g., 0.9 = 10% discount)
}
```

**Effort:** ~10 lines of code

---

### 🔧 Token Parsing (Add Economic Tokens)
**Location:** `OverlayResolver.cs:74-110`

**Current:** Only parses TRUCE, ALLY:, HUNT:

**Needed:** Add parsing for:
```csharp
else if (token.StartsWith("TAX:"))
{
    // TAX:0.15 = set 15% tax
    layer.taxModifier = float.Parse(token.Substring(4));
}
else if (token.StartsWith("TAX_BREAK:"))
{
    // TAX_BREAK:0.10 = reduce tax by 10%
    layer.taxModifier = -float.Parse(token.Substring(10));
}
else if (token.StartsWith("SUBSIDY:"))
{
    // SUBSIDY:0.15 = 15% price reduction
    layer.priceMultiplier = 1f - float.Parse(token.Substring(8));
}
```

**Effort:** ~30 lines of code

---

### 🔧 PalimpsestLayer (Add Economic Derived Fields)
**Location:** `PalimpsestLayer.cs:17-21`

**Current:**
```csharp
public bool truce;
public string allyFactionId;
public string huntFactionId;
```

**Needed:**
```csharp
// Existing
public bool truce;
public string allyFactionId;
public string huntFactionId;

// NEW: Parsed economic effects
public float taxModifier;
public float priceMultiplier = 1f;
```

**Effort:** ~5 lines of code

---

### 🔧 Merchant.GetBuyPrice() (Add Modifier Chain)
**Location:** `Merchant.cs:132-135`

**Current:**
```csharp
public int GetBuyPrice(string itemId)
{
    return Profile?.GetBuyPrice(itemId) ?? 0;
}
```

**Needed:**
```csharp
public int GetBuyPrice(string itemId)
{
    int basePrice = Profile?.GetBuyPrice(itemId) ?? 0;

    // Get position
    var entity = GetComponent<GridEntity>();
    if (entity == null) return basePrice;

    // Apply palimpsest modifiers
    var rules = OverlayResolver.GetRulesAt(entity.gridX, entity.gridY);
    float modifier = rules.priceMultiplier;

    // Apply tax
    float taxRate = GetBaseTaxRate() + rules.taxModifier;
    modifier *= (1f + Mathf.Clamp(taxRate, 0f, 0.5f));

    // Apply faction discount
    var faction = GetComponent<FactionMember>()?.faction;
    if (faction != null)
    {
        int rep = ReputationSystem.GetRep(faction.id);
        modifier *= GetReputationPriceModifier(rep);
    }

    return Mathf.Max(1, Mathf.RoundToInt(basePrice * modifier));
}
```

**Effort:** ~25 lines of code

---

### 🔧 ItemDatabase (Add Ink Resource)
**Location:** `ItemDatabase.cs:86-108`

**Needed:** Add ink as a resource item:
```csharp
Register(new ItemData("ink", "Inscription Ink", ItemType.Currency, 143)
{
    stackable = true,
    maxStack = 999,
    value = 10
});
```

**Effort:** 6 lines of code

---

### 🔧 LedgerPanel (Add Economy Tab)
**Location:** `LedgerPanel.cs`

The existing pattern shows:
- Tab system can be added (see `BuildDetailPane()` pattern)
- Slider control exists and works
- Button creation is standardized via `CreateButton()`

**Needed:**
1. Add tab switching (Factions / Economy)
2. Add district dropdown
3. Add tax rate display/edit controls
4. Add "Inscribe" button that creates palimpsest layer

**Effort:** ~200 lines of code (but follows existing patterns)

---

## 3. Missing Systems (Must Be Created)

### ❌ TaxRegistry
**Purpose:** Track active tax policies per district

**Minimal Implementation:**
```csharp
public static class TaxRegistry
{
    private static Dictionary<string, float> _districtTaxRates = new();

    public static float GetTaxRate(string districtId)
    {
        return _districtTaxRates.TryGetValue(districtId, out var rate) ? rate : 0.1f;
    }

    public static void SetTaxRate(string districtId, float rate)
    {
        _districtTaxRates[districtId] = Mathf.Clamp(rate, 0f, 0.5f);
    }
}
```

**Effort:** ~30 lines of code

---

### ❌ EconomicPriceResolver
**Purpose:** Central calculation hub (optional for simple version)

For a simple version, this can be inlined into `Merchant.GetBuyPrice()`. For the full system, create a static service:

```csharp
public static class EconomicPriceResolver
{
    public static int ResolveBuyPrice(string itemId, Merchant merchant, int x, int y)
    {
        float price = ItemDatabase.Get(itemId).value;
        price *= merchant.Profile.buyMultiplier;
        price *= GetPalimpsestModifier(x, y);
        price *= (1f + GetEffectiveTaxRate(x, y));
        price *= GetFactionModifier(merchant);
        return Mathf.Max(1, Mathf.RoundToInt(price));
    }
}
```

**Effort:** ~60 lines of code

---

### ❌ Ink Cost Calculation
**Purpose:** Determine how much ink an inscription costs

**Minimal Implementation:**
```csharp
public static class InscriptionCostCalculator
{
    public static int Calculate(float effectMagnitude, int duration, int radius)
    {
        int cost = 5;  // Base
        cost += Mathf.RoundToInt(Mathf.Abs(effectMagnitude) * 20);
        cost += duration / 2;
        cost += radius * 2;
        return cost;
    }
}
```

**Effort:** ~15 lines of code

---

## 4. Integration Points Summary

| Existing System | Integration Method | Lines to Add |
|-----------------|-------------------|--------------|
| `Merchant.GetBuyPrice()` | Inject modifier chain | ~25 |
| `Merchant.GetSellPrice()` | Same pattern | ~20 |
| `OverlayResolver.GetRulesAt()` | Add economic fields | ~15 |
| `OverlayResolver.ParseTokens()` | Add economic token parsing | ~30 |
| `PalimpsestLayer` | Add economic derived fields | ~5 |
| `ItemDatabase` | Add ink item | ~6 |
| `LedgerController` | Add tab switching | ~20 |
| `LedgerPanel` | Add economy tab content | ~200 |

**Total for Simple Version:** ~350 lines of new code

---

## 5. Recommended Implementation Order

### Phase 1: Core Price Modifiers (Minimal Viable)
1. Add `taxModifier` and `priceMultiplier` to `PalimpsestRules`
2. Add parsing for `TAX:`, `TAX_BREAK:`, `SUBSIDY:` tokens
3. Modify `Merchant.GetBuyPrice()` to apply modifiers
4. Add `ink` to `ItemDatabase`

**Result:** Palimpsest layers can now affect merchant prices

### Phase 2: Simple Tax System
1. Create `TaxRegistry` (~30 lines)
2. Add base tax rate per district
3. Hook tax into price calculation

**Result:** Districts have tax rates that affect prices

### Phase 3: Faction Discounts
1. Add `GetReputationPriceModifier()` helper
2. Hook into `Merchant.GetBuyPrice()`

**Result:** Friendly factions give discounts

### Phase 4: Ledger Economy Tab
1. Add tab UI to `LedgerPanel`
2. Show current district's tax rate
3. Add "Inscribe Tax Edict" button
4. Create palimpsest layer on inscription

**Result:** Full player-facing economic manipulation

---

## 6. What Already Works Without Changes

| Feature | How It Works |
|---------|--------------|
| Base item prices | `ItemData.value` on all items |
| Merchant markup | `MerchantProfile.buyMultiplier` |
| Transaction flow | `MerchantService.TryBuy/TrySell` |
| Layer registration | `OverlayResolver.RegisterLayer()` |
| Layer decay | `OverlayResolver.TickDecay()` |
| Position lookup | `OverlayResolver.GetRulesAt(x, y)` |
| District lookup | `DistrictControlService.GetStateByPosition()` |
| Faction rep | `ReputationSystem.GetRep()` |
| Inter-faction rep | `ReputationSystem.GetInterRep()` |
| Inventory tracking | `Inventory.CountItem("ink")` |

---

## 7. Dependency Graph

```
┌─────────────────────────────────────────────────────────────┐
│                    IMPLEMENTATION ORDER                      │
├─────────────────────────────────────────────────────────────┤
│                                                              │
│  PHASE 1: Foundation (no dependencies)                       │
│  ─────────────────────────────────────                       │
│  ┌─────────────────┐  ┌─────────────────┐                   │
│  │ PalimpsestLayer │  │ ItemDatabase    │                   │
│  │ (add fields)    │  │ (add ink item)  │                   │
│  └────────┬────────┘  └────────┬────────┘                   │
│           │                    │                             │
│           ▼                    │                             │
│  ┌─────────────────┐          │                             │
│  │ OverlayResolver │◄─────────┘                             │
│  │ (parse tokens)  │                                        │
│  └────────┬────────┘                                        │
│           │                                                  │
│  PHASE 2: Price Resolution (depends on Phase 1)             │
│  ──────────────────────────────────────────────             │
│           │                                                  │
│           ▼                                                  │
│  ┌─────────────────┐  ┌─────────────────┐                   │
│  │ Merchant        │  │ TaxRegistry     │                   │
│  │ (modify prices) │◄─┤ (new, simple)   │                   │
│  └────────┬────────┘  └─────────────────┘                   │
│           │                                                  │
│  PHASE 3: Faction Integration (depends on Phase 2)          │
│  ─────────────────────────────────────────────────          │
│           │                                                  │
│           ▼                                                  │
│  ┌─────────────────┐                                        │
│  │ ReputationSystem│ (already exists, just call it)         │
│  │ GetRep() hook   │                                        │
│  └────────┬────────┘                                        │
│           │                                                  │
│  PHASE 4: UI (depends on Phase 1-3)                         │
│  ──────────────────────────────────                         │
│           │                                                  │
│           ▼                                                  │
│  ┌─────────────────┐  ┌─────────────────┐                   │
│  │ LedgerPanel     │  │ InscriptionCost │                   │
│  │ (economy tab)   │◄─┤ Calculator      │                   │
│  └─────────────────┘  └─────────────────┘                   │
│                                                              │
└─────────────────────────────────────────────────────────────┘
```

---

## 8. Risk Assessment

| Risk | Likelihood | Mitigation |
|------|------------|------------|
| Price calculation too slow | Low | Cache palimpsest lookups |
| Token parsing conflicts | Low | Use unique prefixes (TAX:, not T:) |
| Negative prices | Medium | Always `Mathf.Max(1, price)` |
| Stacking modifiers break economy | Medium | Clamp total modifier range |
| UI complexity | Medium | Start with debug-only, polish later |

---

## 9. Conclusion

**The simple economic system is highly implementable** with your current codebase. The key insight is that 80% of the infrastructure already exists:

- ✅ Item values exist
- ✅ Merchant multipliers exist
- ✅ Transaction service exists
- ✅ Palimpsest layers exist
- ✅ District system exists
- ✅ Reputation system exists
- ✅ Inventory system exists

**What's needed:**
1. ~5 lines: Add fields to `PalimpsestLayer`
2. ~15 lines: Add fields to `PalimpsestRules`
3. ~30 lines: Parse new tokens in `OverlayResolver`
4. ~25 lines: Modify `Merchant.GetBuyPrice()`
5. ~6 lines: Add ink item
6. ~30 lines: Create simple `TaxRegistry`
7. ~200 lines: Create economy tab in `LedgerPanel`

**Total: ~350 lines for a working simple economic system**

This can be done incrementally - Phase 1 alone (~50 lines) gives you palimpsest-driven price modifiers working immediately.
