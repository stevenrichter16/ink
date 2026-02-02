# Economic System Design Document

## Overview

This document describes a comprehensive economic system for Ink that integrates with the existing **Palimpsest** layer system, **Ledger** UI, **Faction** relationships, and **Territorial Control** mechanics. The core innovation is that economic policies are **inscribed as palimpsest layers**, allowing players to manipulate markets, taxes, and trade through the same mystical writing system used for other world modifications.

---

## Table of Contents

1. [Core Economic Concepts](#1-core-economic-concepts)
2. [Price Resolution System](#2-price-resolution-system)
3. [Economic Officials](#3-economic-officials)
4. [Tax System](#4-tax-system)
5. [Trade Relationships](#5-trade-relationships)
6. [Market System](#6-market-system)
7. [Economic Palimpsest Tokens](#7-economic-palimpsest-tokens)
8. [Economic Ledger Integration](#8-economic-ledger-integration)
9. [District Economics](#9-district-economics)
10. [Faction Economic Behaviors](#10-faction-economic-behaviors)
11. [Supply and Demand](#11-supply-and-demand)
12. [Implementation Architecture](#12-implementation-architecture)
13. [Data Structures](#13-data-structures)
14. [Integration Points](#14-integration-points)
15. [Example Scenarios](#15-example-scenarios)

---

## 1. Core Economic Concepts

### 1.1 Price Resolution Pipeline

Every transaction flows through a multi-stage price calculation:

```
Base Price (ItemData.value)
    │
    ├── × Merchant Multiplier (MerchantProfile.buyMultiplier)
    │
    ├── × District Economic Modifier (prosperity, scarcity)
    │
    ├── × Tax Rate (sales tax, import/export duties)
    │
    ├── × Faction Standing Modifier (reputation-based discounts)
    │
    ├── × Palimpsest Layer Modifiers (inscribed economic edicts)
    │
    ├── × Supply/Demand Modifier (item availability)
    │
    └── = Final Transaction Price
```

### 1.2 Economic Actors

| Actor | Role | Editable Via |
|-------|------|--------------|
| **Player** | Consumer, trader, policy manipulator | Direct action |
| **Merchants** | Buy/sell goods, affected by policies | Palimpsest layers |
| **Officials** | Set and enforce economic policies | Influence, bribery, inscriptions |
| **Factions** | Control districts, set regional policies | Reputation, territorial control |
| **Markets** | Centralized trading hubs with dynamic pricing | Supply/demand, inscriptions |

### 1.3 Currencies

| Currency | Use | Notes |
|----------|-----|-------|
| **Coins** | Primary currency | All transactions |
| **Gems** | High-value currency | 1 gem = 50 coins base rate |
| **Ink** | Inscription fuel | Required for economic edicts |
| **Trade Tokens** | Faction-specific scrip | Earned through faction commerce |

---

## 2. Price Resolution System

### 2.1 EconomicPriceResolver

Central service that calculates final prices by aggregating all modifiers.

```csharp
// Core price resolution
public static class EconomicPriceResolver
{
    public static int ResolveBuyPrice(string itemId, Merchant merchant, Vector2Int location)
    {
        float price = ItemDatabase.Get(itemId).value;

        // Layer 1: Merchant markup
        price *= merchant.Profile.buyMultiplier;

        // Layer 2: District economics
        var district = DistrictControlService.GetDistrictAt(location);
        if (district != null)
            price *= GetDistrictPriceModifier(district, itemId);

        // Layer 3: Tax rate
        price *= (1f + GetEffectiveTaxRate(location, itemId));

        // Layer 4: Faction standing
        var merchantFaction = merchant.GetComponent<FactionMember>()?.factionId;
        if (merchantFaction != null)
            price *= GetFactionPriceModifier(merchantFaction);

        // Layer 5: Palimpsest economic layers
        price *= GetPalimpsestPriceModifier(location, itemId);

        // Layer 6: Supply/demand
        price *= GetSupplyDemandModifier(district, itemId);

        return Mathf.Max(1, Mathf.RoundToInt(price));
    }
}
```

### 2.2 Modifier Stacking Rules

| Modifier Type | Stacking | Range | Notes |
|---------------|----------|-------|-------|
| District prosperity | Multiplicative | 0.5 - 2.0 | Poor districts have lower prices |
| Tax rate | Additive | 0% - 50% | Sum of all applicable taxes |
| Faction discount | Multiplicative | 0.7 - 1.3 | Based on reputation |
| Palimpsest | Multiplicative | 0.1 - 3.0 | Inscribed effects |
| Supply/Demand | Multiplicative | 0.5 - 2.0 | Item scarcity |

---

## 3. Economic Officials

### 3.1 Official Types

Officials are special NPCs who control economic policies in their jurisdiction.

| Official Type | Jurisdiction | Controls | Location |
|---------------|--------------|----------|----------|
| **Tax Collector** | Single district | Sales tax rate | Tax office buildings |
| **Trade Minister** | Faction-wide | Import/export duties | Faction headquarters |
| **Market Overseer** | Single market | Price floors/ceilings | Market squares |
| **Customs Officer** | District borders | Border tariffs | District entry points |
| **Guild Master** | Item category | Crafting fees, licenses | Guild halls |
| **Treasurer** | Faction-wide | Currency exchange rates | Banks |

### 3.2 OfficialDefinition ScriptableObject

```csharp
[CreateAssetMenu(menuName = "Ink/Economy/Official Definition")]
public class OfficialDefinition : ScriptableObject
{
    [Header("Identity")]
    public string id;
    public string title;           // "Tax Collector", "Trade Minister"
    public string displayName;     // "Marcus the Tax Collector"

    [Header("Jurisdiction")]
    public OfficialJurisdiction jurisdiction;
    public string districtId;      // If district-level
    public string factionId;       // Controlling faction

    [Header("Economic Controls")]
    public List<EconomicPolicy> policies;

    [Header("Influence")]
    [Range(0f, 1f)] public float corruptibility;  // How easily bribed
    public int baseBribeCost;
    public int influenceDecayPerDay;              // How fast changes revert

    [Header("Personality")]
    public OfficialDisposition disposition;       // Greedy, Fair, Strict
}

public enum OfficialJurisdiction { District, Faction, Market, Guild }
public enum OfficialDisposition { Greedy, Fair, Strict, Corrupt, Zealous }
```

### 3.3 Influencing Officials

Players can modify official behavior through multiple methods:

| Method | Cost | Duration | Risk |
|--------|------|----------|------|
| **Bribery** | Coins | Temporary (decays) | May refuse, report to faction |
| **Reputation** | Faction standing | Permanent while standing held | None |
| **Blackmail** | Quest item | Permanent until discovered | Official may retaliate |
| **Palimpsest Inscription** | Ink | Until layer decays | Contradictions cause heat |
| **Assassination** | Moral cost | Until replacement | Faction hostility |

### 3.4 Official State Machine

```
                    ┌─────────────┐
                    │   NEUTRAL   │ ◄── Default state
                    └──────┬──────┘
                           │
         ┌─────────────────┼─────────────────┐
         ▼                 ▼                 ▼
    ┌─────────┐      ┌───────────┐     ┌──────────┐
    │ BRIBED  │      │ INFLUENCED│     │ COERCED  │
    └────┬────┘      └─────┬─────┘     └────┬─────┘
         │                 │                 │
         │ decay           │ rep change      │ discovery
         ▼                 ▼                 ▼
    ┌─────────┐      ┌───────────┐     ┌──────────┐
    │ NEUTRAL │      │  HOSTILE  │     │ HOSTILE  │
    └─────────┘      └───────────┘     └──────────┘
```

---

## 4. Tax System

### 4.1 Tax Types

| Tax Type | Applies To | Default Rate | Controlled By |
|----------|------------|--------------|---------------|
| **Sales Tax** | All purchases | 10% | District Tax Collector |
| **Import Duty** | Goods from other districts | 15% | Customs Officer |
| **Export Duty** | Goods leaving district | 5% | Customs Officer |
| **Luxury Tax** | High-value items (>100 base) | 20% | Trade Minister |
| **Guild Fee** | Crafted/specialty items | 5% | Guild Master |
| **War Tax** | All transactions during conflict | 25% | Faction leader |
| **Faction Tithe** | Faction-affiliated merchants | 10% | Faction |

### 4.2 TaxPolicy Data Structure

```csharp
[System.Serializable]
public class TaxPolicy
{
    public string id;
    public TaxType type;
    public float rate;                    // 0.0 to 1.0
    public string jurisdictionId;         // District or faction ID
    public List<string> exemptFactions;   // Factions immune to this tax
    public List<string> exemptItems;      // Item IDs exempt
    public List<string> targetItems;      // If set, only applies to these
    public int turnsRemaining;            // -1 for permanent
    public string sourceLayerId;          // If created by palimpsest
}

public enum TaxType
{
    Sales, Import, Export, Luxury, Guild, War, Tithe
}
```

### 4.3 Tax Resolution

```csharp
public static float GetEffectiveTaxRate(Vector2Int location, string itemId)
{
    float totalTax = 0f;
    var district = DistrictControlService.GetDistrictAt(location);
    var controllingFaction = district?.GetControllingFaction();

    // Get all applicable tax policies
    var policies = TaxRegistry.GetPoliciesFor(district?.id, controllingFaction);

    foreach (var policy in policies)
    {
        // Check exemptions
        if (policy.exemptItems.Contains(itemId)) continue;
        if (IsPlayerExempt(policy)) continue;

        // Check if item is targeted (if targeting enabled)
        if (policy.targetItems.Count > 0 && !policy.targetItems.Contains(itemId))
            continue;

        totalTax += policy.rate;
    }

    // Apply palimpsest tax modifiers
    var rules = OverlayResolver.GetRulesAt(location.x, location.y);
    totalTax += rules.taxModifier;           // Can be negative (tax break)
    totalTax *= rules.taxMultiplier;         // Usually 1.0, can reduce

    return Mathf.Clamp(totalTax, 0f, 0.9f);  // Cap at 90% tax
}
```

### 4.4 Tax Evasion Mechanics

Players can attempt to evade taxes through:

| Method | Skill Check | Consequence if Caught |
|--------|-------------|----------------------|
| **Smuggling** | Stealth vs. Patrol | Contraband confiscated, fine, reputation loss |
| **False Invoices** | Deception vs. Official | Double tax + fine |
| **Bribery** | Negotiation + coins | Official may report you |
| **Tax Haven Inscription** | Ink cost | Creates palimpsest heat |

---

## 5. Trade Relationships

### 5.1 Faction Trade Standings

Factions have trade relationships independent of hostility:

```csharp
public class FactionTradeRelation
{
    public string sourceFactionId;
    public string targetFactionId;

    public TradeStatus status;           // Open, Restricted, Embargo, Exclusive
    public float tariffRate;             // Additional cost for cross-faction trade
    public List<string> bannedItems;     // Items that cannot be traded
    public List<string> exclusiveItems;  // Items only this faction can buy
}

public enum TradeStatus
{
    Open,           // Normal trade, standard prices
    Restricted,     // Limited items, higher tariffs
    Embargo,        // No direct trade allowed
    Exclusive,      // Preferential pricing, exclusive goods
    Alliance        // Reduced tariffs, shared inventory
}
```

### 5.2 Trade Relationship Matrix

```
              │ Inkguard │ Inkbound │ Ghost │ Slime │
──────────────┼──────────┼──────────┼───────┼───────│
Inkguard      │    -     │ Restrict │Embargo│ Open  │
Inkbound      │ Restrict │    -     │ Open  │ Open  │
Ghost         │ Embargo  │  Open    │   -   │Embargo│
Slime         │  Open    │  Open    │Embargo│   -   │
```

### 5.3 Trade Route System

Trade routes connect districts and affect goods flow:

```csharp
[CreateAssetMenu(menuName = "Ink/Economy/Trade Route")]
public class TradeRoute : ScriptableObject
{
    public string id;
    public string sourceDistrictId;
    public string destinationDistrictId;

    public TradeRouteStatus status;      // Active, Blocked, Dangerous
    public float efficiency;             // 0.0 - 1.0, affects supply delivery
    public List<string> primaryGoods;    // Items commonly traded on this route
    public int travelTimeInDays;

    public string controllingFactionId;  // Who collects tolls
    public float tollRate;               // Additional cost to use route
}

public enum TradeRouteStatus
{
    Active,      // Normal operation
    Blocked,     // No goods flowing (war, disaster)
    Dangerous,   // Goods may be lost (bandits, monsters)
    Contested    // Multiple factions fighting for control
}
```

### 5.4 Palimpsest Trade Inscriptions

Players can inscribe trade-affecting layers:

| Token | Effect | Duration |
|-------|--------|----------|
| `TRADE_OPEN:faction_a:faction_b` | Forces open trade between factions | 10 turns |
| `EMBARGO:faction_id` | Blocks faction from trading in radius | 8 turns |
| `SMUGGLER_HAVEN` | Ignores trade restrictions in radius | 6 turns |
| `TOLL_FREE` | Removes tolls on routes through radius | 12 turns |
| `TRADE_ALLIANCE:faction_a:faction_b` | Creates temporary exclusive deal | 15 turns |

---

## 6. Market System

### 6.1 Market Types

| Market Type | Size | Features | Price Volatility |
|-------------|------|----------|------------------|
| **Street Vendor** | 1 tile | Single merchant, limited stock | High |
| **Shop** | Building | Specialized goods, restocking | Medium |
| **Bazaar** | District | Multiple merchants, variety | Medium |
| **Central Market** | Multi-district | All goods, best prices, highest traffic | Low |
| **Black Market** | Hidden | Illegal goods, no taxes, high risk | Very High |
| **Auction House** | Building | Player-to-player, bidding | Extreme |

### 6.2 MarketDefinition ScriptableObject

```csharp
[CreateAssetMenu(menuName = "Ink/Economy/Market Definition")]
public class MarketDefinition : ScriptableObject
{
    [Header("Identity")]
    public string id;
    public string displayName;
    public MarketType type;

    [Header("Location")]
    public string districtId;
    public Vector2Int centerTile;
    public int radius;                    // Market zone size

    [Header("Economics")]
    public float basePriceModifier;       // Markets often have better prices
    public float volatilityFactor;        // How much prices swing
    public int restockIntervalDays;

    [Header("Specialization")]
    public List<ItemType> specializedTypes;    // Bonus stock/prices for these
    public float specializationDiscount;       // Discount for specialized items

    [Header("Control")]
    public string controllingFactionId;
    public string marketOverseerId;            // Official who manages it

    [Header("Regulations")]
    public bool allowsIllegalGoods;
    public float taxEnforcementRate;           // 0-1, chance of tax being applied
    public List<string> bannedItems;
}

public enum MarketType
{
    StreetVendor, Shop, Bazaar, CentralMarket, BlackMarket, AuctionHouse
}
```

### 6.3 Market Price Discovery

Markets aggregate supply/demand across all merchants:

```csharp
public class MarketPriceIndex
{
    public string marketId;
    public Dictionary<string, MarketItemData> itemPrices;

    public void UpdatePrices()
    {
        foreach (var merchant in GetMerchantsInMarket())
        {
            foreach (var stock in merchant.Stock)
            {
                var data = itemPrices.GetOrCreate(stock.itemId);
                data.totalSupply += stock.quantity;
                data.merchantCount++;
                data.priceSum += merchant.GetBuyPrice(stock.itemId);
            }
        }

        // Calculate average prices and supply levels
        foreach (var kvp in itemPrices)
        {
            var data = kvp.Value;
            data.averagePrice = data.priceSum / data.merchantCount;
            data.supplyLevel = CalculateSupplyLevel(data.totalSupply);
            data.priceModifier = CalculateDemandModifier(data.supplyLevel);
        }
    }
}

public class MarketItemData
{
    public int totalSupply;
    public int merchantCount;
    public int priceSum;
    public int averagePrice;
    public SupplyLevel supplyLevel;       // Scarce, Low, Normal, High, Surplus
    public float priceModifier;           // 0.5 (surplus) to 2.0 (scarce)
}
```

### 6.4 Black Market Mechanics

Black markets have special rules:

```csharp
public class BlackMarket : Market
{
    public float discoveryRisk;           // Chance guards find the market
    public int reputationRequired;        // Criminal rep needed to access

    public override bool CanPlayerAccess()
    {
        // Requires criminal contacts or palimpsest UNDERWORLD token
        var rules = OverlayResolver.GetRulesAt(transform.position);
        if (rules.underworldAccess) return true;

        return CriminalReputationSystem.GetRep() >= reputationRequired;
    }

    public override float GetTaxRate() => 0f;  // No taxes

    public override List<string> GetAvailableItems()
    {
        // Includes illegal items
        var items = base.GetAvailableItems();
        items.AddRange(IllegalItemDatabase.GetAll());
        return items;
    }
}
```

---

## 7. Economic Palimpsest Tokens

### 7.1 Token Registry Extensions

New tokens for `PalimpsestTokenRegistry`:

#### Tax Tokens

| Token | Parameters | Effect |
|-------|------------|--------|
| `TAX:rate` | rate (0.0-1.0) | Sets flat tax rate in radius |
| `TAX_BREAK:rate` | rate (0.0-1.0) | Reduces taxes by rate |
| `TAX_EXEMPT:faction_id` | faction | Faction pays no taxes in radius |
| `TAX_DOUBLE:faction_id` | faction | Faction pays double taxes |

#### Price Tokens

| Token | Parameters | Effect |
|-------|------------|--------|
| `PRICE_FLOOR:item_id:price` | item, min price | Item cannot sell below price |
| `PRICE_CEILING:item_id:price` | item, max price | Item cannot sell above price |
| `SUBSIDY:item_id:rate` | item, discount | Item costs less (subsidized) |
| `TARIFF:item_id:rate` | item, markup | Item costs more (import duty) |
| `INFLATE:rate` | multiplier | All prices multiplied |
| `DEFLATE:rate` | multiplier | All prices divided |

#### Trade Tokens

| Token | Parameters | Effect |
|-------|------------|--------|
| `TRADE_BAN:faction_id` | faction | Faction cannot trade in radius |
| `TRADE_ONLY:faction_id` | faction | Only this faction can trade |
| `SMUGGLER_ZONE` | none | Ignores trade restrictions |
| `FREE_TRADE` | none | No tariffs or duties |
| `BLOCKADE` | none | No goods can enter/leave |

#### Market Tokens

| Token | Parameters | Effect |
|-------|------------|--------|
| `MARKET_BOOST` | none | +50% merchant stock |
| `MARKET_CRASH` | none | -50% all prices (fire sale) |
| `SCARCITY:item_id` | item | Item supply halved |
| `ABUNDANCE:item_id` | item | Item supply doubled |
| `BLACK_MARKET_ACCESS` | none | Reveals black market in radius |

#### Official Tokens

| Token | Parameters | Effect |
|-------|------------|--------|
| `CORRUPT_OFFICIAL:official_id` | official | Official accepts bribes |
| `REPLACE_OFFICIAL:official_id:faction_id` | official, faction | Changes official allegiance |
| `OFFICIAL_BLIND` | none | Officials ignore violations in radius |
| `AUDIT_ZONE` | none | Double penalties for violations |

### 7.2 Token Parsing Extension

```csharp
// Extended TokenRule for economic effects
[System.Serializable]
public class EconomicTokenRule : TokenRule
{
    [Header("Economic Effects")]
    public float taxModifier;              // Added to tax rate
    public float priceMultiplier = 1f;     // Applied to all prices

    public string targetItemId;            // If set, only affects this item
    public string targetFactionId;         // If set, only affects this faction

    public float supplyModifier = 1f;      // Multiplier on supply
    public float demandModifier = 1f;      // Multiplier on demand

    public bool enableBlackMarket;
    public bool disableTaxEnforcement;
    public bool blockTrade;
}
```

### 7.3 Economic Layer Resolution

```csharp
public class EconomicRules
{
    public float taxModifier;
    public float priceMultiplier;
    public float supplyModifier;
    public float demandModifier;

    public HashSet<string> tradeBannedFactions;
    public HashSet<string> taxExemptFactions;
    public Dictionary<string, float> itemPriceOverrides;

    public bool blackMarketAccess;
    public bool taxEnforcementDisabled;
    public bool tradeBlocked;
}

public static EconomicRules GetEconomicRulesAt(int x, int y)
{
    var rules = new EconomicRules();
    var layers = GetLayersAt(x, y).OrderBy(l => l.priority);

    foreach (var layer in layers)
    {
        foreach (var token in layer.tokens)
        {
            if (TryParseEconomicToken(token, out var economic))
            {
                rules.taxModifier += economic.taxModifier;
                rules.priceMultiplier *= economic.priceMultiplier;
                rules.supplyModifier *= economic.supplyModifier;
                rules.demandModifier *= economic.demandModifier;

                if (economic.blockTrade)
                    rules.tradeBlocked = true;
                if (economic.enableBlackMarket)
                    rules.blackMarketAccess = true;
                if (economic.disableTaxEnforcement)
                    rules.taxEnforcementDisabled = true;

                // ... apply other effects
            }
        }
    }

    return rules;
}
```

---

## 8. Economic Ledger Integration

### 8.1 Economic Ledger Tab

Extend `LedgerPanel` with a new "Economy" tab:

```
┌──────────────────────────────────────────────────────────────┐
│  LEDGER                              [Factions] [Economy]    │
├──────────────────────────────────────────────────────────────┤
│                                                              │
│  DISTRICT: Market Quarter            CONTROLLER: Inkguard    │
│  ─────────────────────────────────────────────────────────   │
│                                                              │
│  TAX RATES                                                   │
│  ├─ Sales Tax:     [████████░░] 15%    [Edit]               │
│  ├─ Import Duty:   [██████░░░░] 10%    [Edit]               │
│  ├─ Luxury Tax:    [████████████] 20%  [Edit]               │
│  └─ Guild Fees:    [███░░░░░░░] 5%     [Edit]               │
│                                                              │
│  TRADE STATUS                                                │
│  ├─ Inkbound:      [RESTRICTED]        [Modify]             │
│  ├─ Ghost:         [EMBARGO]           [Modify]             │
│  └─ Slime:         [OPEN]              [Modify]             │
│                                                              │
│  OFFICIALS                                                   │
│  ├─ Tax Collector: Marcus (Fair)       [Influence]          │
│  └─ Overseer:      Helena (Strict)     [Influence]          │
│                                                              │
│  ACTIVE EDICTS (Palimpsest)                                 │
│  ├─ TAX_BREAK:0.05  (8 turns)          [Erase]              │
│  └─ SUBSIDY:potion:0.1 (3 turns)       [Erase]              │
│                                                              │
│  ─────────────────────────────────────────────────────────   │
│  INK: 47                                                     │
│  [Inscribe Tax Edict]  [Inscribe Trade Edict]               │
│                                                              │
└──────────────────────────────────────────────────────────────┘
```

### 8.2 Inscription Flow

When player clicks "Edit" on a tax rate:

```
┌──────────────────────────────────────────────────────────────┐
│  INSCRIBE TAX EDICT                                          │
├──────────────────────────────────────────────────────────────┤
│                                                              │
│  Current Sales Tax: 15%                                      │
│                                                              │
│  New Rate: [████████░░░░░░░░░░░░] 10%                       │
│                                                              │
│  Duration: [███████████░░░░░░░░░] 12 turns                  │
│                                                              │
│  Radius:   [████░░░░░░░░░░░░░░░░] 3 tiles                   │
│                                                              │
│  ─────────────────────────────────────────────────────────   │
│  INK COST BREAKDOWN:                                         │
│  ├─ Base cost:           5 ink                               │
│  ├─ Rate change (-5%):   +3 ink                              │
│  ├─ Duration (12 turns): +6 ink                              │
│  ├─ Radius (3 tiles):    +4 ink                              │
│  └─ TOTAL:               18 ink                              │
│                                                              │
│  [Cancel]                              [Inscribe] (18 ink)   │
│                                                              │
└──────────────────────────────────────────────────────────────┘
```

### 8.3 LedgerEconomyPanel Component

```csharp
public class LedgerEconomyPanel : MonoBehaviour
{
    [Header("State")]
    private DistrictDefinition _currentDistrict;
    private List<TaxPolicy> _districtTaxes;
    private List<FactionTradeRelation> _tradeRelations;
    private List<Official> _districtOfficials;
    private List<PalimpsestLayer> _activeEdicts;

    [Header("UI Elements")]
    private TMP_Dropdown _districtDropdown;
    private List<TaxRateRow> _taxRows;
    private List<TradeStatusRow> _tradeRows;
    private List<OfficialRow> _officialRows;
    private List<EdictRow> _edictRows;
    private TMP_Text _inkDisplay;

    public void RefreshForDistrict(string districtId)
    {
        _currentDistrict = DistrictDatabase.Get(districtId);
        _districtTaxes = TaxRegistry.GetPoliciesFor(districtId);
        _tradeRelations = TradeRelationRegistry.GetForDistrict(districtId);
        _districtOfficials = OfficialRegistry.GetInDistrict(districtId);
        _activeEdicts = OverlayResolver.GetEconomicLayersIn(districtId);

        RebuildUI();
    }

    public void OnEditTaxClicked(TaxPolicy policy)
    {
        // Open inscription dialog for tax modification
        InscriptionDialog.Show(new TaxEdictConfig
        {
            targetPolicy = policy,
            districtId = _currentDistrict.id,
            onConfirm = CreateTaxInscription
        });
    }

    private void CreateTaxInscription(TaxEdictConfig config)
    {
        // Calculate ink cost
        int cost = CalculateInkCost(config);
        if (!PlayerInkInventory.TrySpend(cost))
        {
            ShowError("Not enough ink!");
            return;
        }

        // Create palimpsest layer
        var layer = new PalimpsestLayer
        {
            id = GenerateLayerId(),
            center = PlayerController.Instance.GridPosition,
            radius = config.radius,
            priority = 10,  // Economic edicts are high priority
            turnsRemaining = config.duration,
            tokens = new List<string> { BuildTaxToken(config) }
        };

        OverlayResolver.RegisterLayer(layer);
        AuditLog.Record(AuditAction.EconomicInscription, layer);
        RefreshForDistrict(_currentDistrict.id);
    }

    private string BuildTaxToken(TaxEdictConfig config)
    {
        float delta = config.newRate - config.targetPolicy.rate;
        if (delta < 0)
            return $"TAX_BREAK:{Mathf.Abs(delta):F2}";
        else
            return $"TAX:{config.newRate:F2}";
    }
}
```

### 8.4 Ink Cost Formula

```csharp
public static int CalculateEconomicInkCost(EconomicEdictConfig config)
{
    int cost = 5;  // Base cost

    // Magnitude of change
    float magnitude = Mathf.Abs(config.effectMagnitude);
    cost += Mathf.RoundToInt(magnitude * 20);  // 1% = 0.2 ink

    // Duration
    cost += config.duration / 2;  // 2 turns per ink

    // Radius
    cost += config.radius * 2;  // 2 ink per tile radius

    // Complexity multipliers
    if (config.targetsFaction)
        cost = Mathf.RoundToInt(cost * 1.5f);
    if (config.targetsItem)
        cost = Mathf.RoundToInt(cost * 1.2f);
    if (config.contradictsExisting)
        cost = Mathf.RoundToInt(cost * 2f);  // Expensive to override

    return cost;
}
```

---

## 9. District Economics

### 9.1 DistrictEconomicState

Extended state tracking per district:

```csharp
public class DistrictEconomicState
{
    public string districtId;

    [Header("Wealth")]
    public float prosperity;          // 0-1, affects prices and stock
    public float treasury;            // Accumulated tax revenue
    public float corruption;          // Reduces effective tax collection

    [Header("Market Conditions")]
    public Dictionary<string, float> itemSupply;     // Per-item supply levels
    public Dictionary<string, float> itemDemand;     // Per-item demand levels
    public float economicActivity;    // Trade volume indicator

    [Header("Trade")]
    public List<string> activeTradeRoutes;
    public float importVolume;
    public float exportVolume;

    [Header("Employment")]
    public int merchantCount;
    public int officialCount;
    public float unemploymentRate;    // Affects crime, unrest
}
```

### 9.2 Daily Economic Tick

```csharp
public static void AdvanceEconomicDay()
{
    foreach (var district in DistrictRegistry.GetAll())
    {
        var state = GetOrCreateState(district.id);

        // 1. Collect taxes
        float taxRevenue = CalculateDailyTaxRevenue(district);
        taxRevenue *= (1f - state.corruption);  // Corruption reduces collection
        state.treasury += taxRevenue;

        // 2. Update supply based on trade routes
        foreach (var routeId in state.activeTradeRoutes)
        {
            var route = TradeRouteRegistry.Get(routeId);
            if (route.status == TradeRouteStatus.Active)
            {
                DeliverGoods(district, route);
            }
        }

        // 3. Consume goods (simulate NPC purchasing)
        ConsumeGoods(district, state);

        // 4. Adjust prosperity based on economic health
        float healthScore = CalculateEconomicHealth(state);
        state.prosperity = Mathf.Lerp(state.prosperity, healthScore, 0.1f);

        // 5. Apply palimpsest economic effects
        ApplyPalimpsestEconomicEffects(district, state);

        // 6. Decay corruption (or increase if no oversight)
        UpdateCorruption(district, state);

        // 7. Fire events
        OnDistrictEconomyUpdated?.Invoke(district.id, state);
    }
}
```

### 9.3 Prosperity Effects

| Prosperity Level | Price Modifier | Stock Modifier | Description |
|------------------|----------------|----------------|-------------|
| 0.0 - 0.2 | 1.5x | 0.25x | Desperate: scarce goods, high prices |
| 0.2 - 0.4 | 1.25x | 0.5x | Poor: limited selection |
| 0.4 - 0.6 | 1.0x | 1.0x | Normal: baseline economy |
| 0.6 - 0.8 | 0.9x | 1.5x | Prosperous: good deals, full stocks |
| 0.8 - 1.0 | 0.8x | 2.0x | Booming: best prices, overflowing |

---

## 10. Faction Economic Behaviors

### 10.1 FactionEconomicPolicy

Each faction has economic tendencies:

```csharp
[System.Serializable]
public class FactionEconomicPolicy
{
    [Header("Trade Philosophy")]
    public TradePhilosophy philosophy;    // Mercantile, Isolationist, Aggressive

    [Header("Default Rates")]
    public float preferredTaxRate;        // Target tax level
    public float importTariffRate;
    public float exportTariffRate;

    [Header("Specialization")]
    public List<string> producedItems;    // Items this faction makes cheaply
    public List<string> desiredItems;     // Items this faction wants
    public List<string> bannedItems;      // Items this faction won't trade

    [Header("Behavior")]
    public float priceAggressiveness;     // How much they undercut competitors
    public float hoarding;                // Tendency to stockpile
    public float smugglingTolerance;      // 0 = strict, 1 = anything goes
}

public enum TradePhilosophy
{
    Mercantile,      // Maximize trade, low tariffs, many routes
    Isolationist,    // High tariffs, few routes, self-sufficient
    Aggressive,      // Undercut competitors, embargo rivals
    Cooperative,     // Trade alliances, shared markets
    Exploitative     // Extract resources, high export, low import
}
```

### 10.2 Faction Market Interactions

When a faction controls a district, their policy affects the market:

```csharp
public static void ApplyFactionControlEconomics(DistrictState district)
{
    var faction = FactionRegistry.GetById(district.controllingFactionId);
    var policy = faction.economicPolicy;

    // Apply faction tax preferences
    foreach (var tax in TaxRegistry.GetPoliciesFor(district.id))
    {
        if (policy.philosophy == TradePhilosophy.Mercantile)
            tax.rate = Mathf.Min(tax.rate, 0.1f);  // Cap at 10%
        else if (policy.philosophy == TradePhilosophy.Isolationist)
            tax.rate = Mathf.Max(tax.rate, 0.25f); // Minimum 25%
    }

    // Adjust produced item prices (cheaper from source)
    foreach (var itemId in policy.producedItems)
    {
        SetItemPriceModifier(district.id, itemId, 0.7f);  // 30% cheaper
    }

    // Adjust desired item prices (more valuable here)
    foreach (var itemId in policy.desiredItems)
    {
        SetItemPriceModifier(district.id, itemId, 1.3f);  // 30% more
    }
}
```

### 10.3 Reputation-Based Pricing

Player's faction reputation affects merchant prices:

```csharp
public static float GetFactionPriceModifier(string factionId)
{
    int rep = ReputationSystem.GetRep(factionId);

    // Friendly factions give discounts
    if (rep >= 75)  return 0.7f;   // 30% discount (Exalted)
    if (rep >= 50)  return 0.8f;   // 20% discount (Honored)
    if (rep >= 25)  return 0.9f;   // 10% discount (Friendly)
    if (rep >= -25) return 1.0f;   // No modifier (Neutral)
    if (rep >= -50) return 1.15f;  // 15% markup (Unfriendly)
    if (rep >= -75) return 1.3f;   // 30% markup (Hostile)
    return 1.5f;                   // 50% markup (Hated) - if they trade at all
}
```

---

## 11. Supply and Demand

### 11.1 Supply Tracking

```csharp
public class RegionalSupply
{
    public Dictionary<string, ItemSupplyData> items;

    public void UpdateSupply(string itemId, int delta)
    {
        var data = items.GetOrCreate(itemId);
        data.currentSupply += delta;
        data.supplyHistory.Enqueue(delta);

        // Keep 30-day history
        while (data.supplyHistory.Count > 30)
            data.supplyHistory.Dequeue();

        RecalculatePriceModifier(data);
    }

    private void RecalculatePriceModifier(ItemSupplyData data)
    {
        float supplyRatio = data.currentSupply / (float)data.baselineSupply;

        // Supply curve: low supply = high prices
        if (supplyRatio < 0.25f)
            data.priceModifier = 2.0f;      // Severe shortage
        else if (supplyRatio < 0.5f)
            data.priceModifier = 1.5f;      // Shortage
        else if (supplyRatio < 0.75f)
            data.priceModifier = 1.2f;      // Low
        else if (supplyRatio < 1.25f)
            data.priceModifier = 1.0f;      // Normal
        else if (supplyRatio < 1.5f)
            data.priceModifier = 0.9f;      // High
        else if (supplyRatio < 2.0f)
            data.priceModifier = 0.75f;     // Surplus
        else
            data.priceModifier = 0.5f;      // Glut
    }
}

public class ItemSupplyData
{
    public int currentSupply;
    public int baselineSupply;
    public float priceModifier;
    public Queue<int> supplyHistory;
    public float demandRate;        // How fast it's consumed
}
```

### 11.2 Demand Events

Special events that spike demand:

```csharp
public class DemandEvent
{
    public string id;
    public string itemId;
    public float demandMultiplier;
    public int durationDays;
    public string districtId;       // null = global
    public string description;
}

// Example events
public static class DemandEvents
{
    public static DemandEvent PlagueOutbreak = new DemandEvent
    {
        id = "plague_outbreak",
        itemId = "potion",
        demandMultiplier = 3f,
        durationDays = 10,
        description = "A plague has struck! Potions are in high demand."
    };

    public static DemandEvent WarMobilization = new DemandEvent
    {
        id = "war_mobilization",
        itemId = "sword",
        demandMultiplier = 2f,
        durationDays = 20,
        description = "War preparations have begun. Weapons are scarce."
    };
}
```

### 11.3 Palimpsest Supply Manipulation

Players can inscribe supply-affecting layers:

| Token | Effect | Market Impact |
|-------|--------|---------------|
| `ABUNDANCE:potion` | Doubles potion supply | Prices drop 25% |
| `SCARCITY:sword` | Halves sword supply | Prices rise 50% |
| `DEMAND_SPIKE:armor` | Triples armor demand | Prices rise 100% |
| `DEMAND_CRASH:gem` | Zeroes gem demand | Prices drop 75% |
| `PRODUCTION_BOOST` | All supplies +50% | General deflation |
| `FAMINE` | Food items at 10% supply | Survival items spike |

---

## 12. Implementation Architecture

### 12.1 System Hierarchy

```
┌─────────────────────────────────────────────────────────────────┐
│                    ECONOMIC SYSTEM ARCHITECTURE                  │
├─────────────────────────────────────────────────────────────────┤
│                                                                  │
│  ┌──────────────────────────────────────────────────────────┐   │
│  │                  EconomicPriceResolver                    │   │
│  │  (Central hub - queries all systems for final price)     │   │
│  └────────────────────────┬─────────────────────────────────┘   │
│                           │                                      │
│           ┌───────────────┼───────────────┐                     │
│           ▼               ▼               ▼                     │
│  ┌────────────┐  ┌────────────────┐  ┌──────────────┐          │
│  │TaxRegistry │  │SupplyDemand   │  │OverlayResolver│          │
│  │            │  │Service        │  │(Palimpsest)   │          │
│  └─────┬──────┘  └───────┬───────┘  └──────┬────────┘          │
│        │                 │                  │                    │
│        ▼                 ▼                  ▼                    │
│  ┌──────────┐    ┌─────────────┐    ┌─────────────────┐        │
│  │Official  │    │Market       │    │EconomicToken    │        │
│  │Registry  │    │PriceIndex   │    │Parser           │        │
│  └──────────┘    └─────────────┘    └─────────────────┘        │
│                                                                  │
│  ┌──────────────────────────────────────────────────────────┐   │
│  │                    UI Layer                               │   │
│  │  ┌────────────┐  ┌────────────┐  ┌───────────────────┐   │   │
│  │  │LedgerPanel │  │MerchantUI  │  │TerritoryDebugPanel│   │   │
│  │  │(Economy)   │  │(Prices)    │  │(District Econ)    │   │   │
│  │  └────────────┘  └────────────┘  └───────────────────┘   │   │
│  └──────────────────────────────────────────────────────────┘   │
│                                                                  │
│  ┌──────────────────────────────────────────────────────────┐   │
│  │                    Data Layer                             │   │
│  │  ┌──────────────┐  ┌──────────────┐  ┌────────────────┐  │   │
│  │  │District      │  │Faction       │  │TradeRoute      │  │   │
│  │  │EconomicState │  │EconomicPolicy│  │Definition      │  │   │
│  │  └──────────────┘  └──────────────┘  └────────────────┘  │   │
│  └──────────────────────────────────────────────────────────┘   │
│                                                                  │
└─────────────────────────────────────────────────────────────────┘
```

### 12.2 File Structure

```
Assets/Ink/Gameplay/
├── Economy/
│   ├── Core/
│   │   ├── EconomicPriceResolver.cs
│   │   ├── TaxRegistry.cs
│   │   ├── TaxPolicy.cs
│   │   ├── SupplyDemandService.cs
│   │   └── RegionalSupply.cs
│   │
│   ├── Officials/
│   │   ├── OfficialDefinition.cs
│   │   ├── Official.cs
│   │   ├── OfficialRegistry.cs
│   │   ├── OfficialInfluenceService.cs
│   │   └── OfficialStateMachine.cs
│   │
│   ├── Markets/
│   │   ├── MarketDefinition.cs
│   │   ├── Market.cs
│   │   ├── MarketPriceIndex.cs
│   │   ├── BlackMarket.cs
│   │   └── AuctionHouse.cs
│   │
│   ├── Trade/
│   │   ├── TradeRoute.cs
│   │   ├── TradeRelation.cs
│   │   ├── TradeRelationRegistry.cs
│   │   └── SmuggleService.cs
│   │
│   └── Events/
│       ├── DemandEvent.cs
│       ├── EconomicEventService.cs
│       └── EconomicEventDatabase.cs
│
├── Palimpsest/
│   ├── Tokens/
│   │   └── EconomicTokenParser.cs    # New
│   └── PalimpsestTokenRegistry.cs    # Extended
│
├── UI/
│   ├── LedgerEconomyPanel.cs         # New
│   ├── InscriptionDialog.cs          # New
│   ├── MarketOverviewPanel.cs        # New
│   └── OfficialInfluenceDialog.cs    # New
│
└── Factions/
    └── FactionEconomicPolicy.cs      # New
```

### 12.3 Integration Points

| Existing System | Integration Method | Purpose |
|-----------------|-------------------|---------|
| `Merchant.GetBuyPrice()` | Call `EconomicPriceResolver` | Dynamic pricing |
| `MerchantService.TryBuy()` | Apply tax after price calc | Tax collection |
| `DistrictControlService` | Add economic state tracking | Prosperity simulation |
| `OverlayResolver` | Add economic token parsing | Palimpsest edicts |
| `LedgerPanel` | Add Economy tab | Policy manipulation |
| `ReputationSystem` | Feed into price modifiers | Faction discounts |

---

## 13. Data Structures

### 13.1 Core Classes Summary

```csharp
// === TAXES ===
public class TaxPolicy { id, type, rate, jurisdictionId, exemptions, turnsRemaining }
public class TaxRegistry { GetPoliciesFor(), AddPolicy(), RemovePolicy() }

// === OFFICIALS ===
public class OfficialDefinition : ScriptableObject { id, title, jurisdiction, policies, corruptibility }
public class Official : MonoBehaviour { definition, currentState, bribeLevel, influenceDecay }
public class OfficialRegistry { GetInDistrict(), GetByFaction(), GetById() }

// === MARKETS ===
public class MarketDefinition : ScriptableObject { id, type, districtId, specializations }
public class Market : MonoBehaviour { definition, merchants, priceIndex }
public class MarketPriceIndex { itemPrices, UpdatePrices(), GetModifier() }

// === TRADE ===
public class TradeRoute : ScriptableObject { sourceDistrict, destDistrict, status, toll }
public class FactionTradeRelation { sourceFaction, targetFaction, status, tariff, bans }
public class TradeRelationRegistry { GetRelation(), SetRelation(), GetForDistrict() }

// === SUPPLY/DEMAND ===
public class RegionalSupply { items, UpdateSupply(), GetPriceModifier() }
public class ItemSupplyData { currentSupply, baselineSupply, priceModifier, demandRate }
public class DemandEvent { itemId, multiplier, duration, description }

// === DISTRICT ECONOMICS ===
public class DistrictEconomicState { prosperity, treasury, corruption, supplyLevels }

// === PALIMPSEST ECONOMIC ===
public class EconomicTokenRule : TokenRule { taxModifier, priceMultiplier, supplyModifier }
public class EconomicRules { aggregated effects from all layers at a position }

// === FACTION ECONOMICS ===
public class FactionEconomicPolicy { philosophy, preferredTaxRate, producedItems, desiredItems }
```

---

## 14. Integration Points

### 14.1 Modifying Merchant.GetBuyPrice()

```csharp
// In Merchant.cs - replace simple price lookup
public int GetBuyPrice(string itemId)
{
    // Use new resolver instead of profile directly
    return EconomicPriceResolver.ResolveBuyPrice(
        itemId,
        this,
        GridEntity.GetGridPosition(transform.position)
    );
}

public int GetSellPrice(string itemId)
{
    return EconomicPriceResolver.ResolveSellPrice(
        itemId,
        this,
        GridEntity.GetGridPosition(transform.position)
    );
}
```

### 14.2 Extending MerchantUI

```csharp
// In MerchantUI.cs - show price breakdown on hover
private void ShowPriceTooltip(string itemId, Merchant merchant)
{
    var breakdown = EconomicPriceResolver.GetPriceBreakdown(itemId, merchant);

    tooltipText.text = $@"
{ItemDatabase.Get(itemId).name}

Base Value:      {breakdown.basePrice}
Merchant Markup: {breakdown.merchantModifier:P0}
District Factor: {breakdown.districtModifier:P0}
Tax Rate:        {breakdown.taxRate:P0}
Faction Bonus:   {breakdown.factionModifier:P0}
Palimpsest:      {breakdown.palimpsestModifier:P0}
Supply/Demand:   {breakdown.supplyDemandModifier:P0}
─────────────────
FINAL PRICE:     {breakdown.finalPrice}
";
}
```

### 14.3 Extending LedgerController

```csharp
// In LedgerController.cs
public enum LedgerTab { Factions, Economy }
private LedgerTab _currentTab = LedgerTab.Factions;

private void Update()
{
    if (Input.GetKeyDown(KeyCode.B))
    {
        ToggleLedger();
    }

    // Tab switching
    if (_isOpen)
    {
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            _currentTab = _currentTab == LedgerTab.Factions
                ? LedgerTab.Economy
                : LedgerTab.Factions;
            RefreshCurrentTab();
        }
    }
}

private void RefreshCurrentTab()
{
    _factionPanel.SetActive(_currentTab == LedgerTab.Factions);
    _economyPanel.SetActive(_currentTab == LedgerTab.Economy);
}
```

### 14.4 Token Registry Asset Updates

Add to `PalimpsestTokenRegistry.asset`:

```yaml
rules:
  # Existing
  - token: TRUCE
    truce: true
  - token: ALLY:faction_inkbound
    allyFactionId: faction_inkbound

  # New Economic Tokens
  - token: TAX_BREAK:0.10
    taxModifier: -0.10
  - token: TAX_BREAK:0.20
    taxModifier: -0.20
  - token: TAX:0.25
    taxModifier: 0.25
  - token: SUBSIDY:potion:0.15
    targetItemId: potion
    priceMultiplier: 0.85
  - token: TARIFF:sword:0.30
    targetItemId: sword
    priceMultiplier: 1.30
  - token: FREE_TRADE
    taxModifier: -1.0
    disableTaxEnforcement: true
  - token: TRADE_BAN:faction_ghost
    tradeBannedFactions: [faction_ghost]
  - token: BLACK_MARKET_ACCESS
    enableBlackMarket: true
  - token: ABUNDANCE
    supplyModifier: 2.0
  - token: SCARCITY
    supplyModifier: 0.5
```

---

## 15. Example Scenarios

### 15.1 Scenario: Lowering Taxes in the Market Quarter

**Goal:** Player wants to reduce sales tax to attract merchants.

**Steps:**
1. Player opens Ledger (B key)
2. Switches to Economy tab (Tab key)
3. Selects "Market Quarter" district
4. Sees current Sales Tax: 15%
5. Clicks [Edit] on Sales Tax row
6. Inscription dialog opens:
   - Sets new rate: 8%
   - Sets duration: 20 turns
   - Sets radius: 5 tiles (covers main market)
7. Ink cost calculated: 22 ink
8. Player confirms inscription
9. `TAX_BREAK:0.07` layer created at player position
10. All merchants in radius now charge 7% less in taxes
11. Layer appears in "Active Edicts" list with countdown

### 15.2 Scenario: Creating a Smuggler Haven

**Goal:** Player wants to trade illegal goods without faction interference.

**Steps:**
1. Player finds hidden location away from patrols
2. Opens Ledger > Economy
3. Clicks [Inscribe Trade Edict]
4. Selects "Smuggler Zone" from presets
5. Configuration:
   - Tokens: `SMUGGLER_ZONE`, `OFFICIAL_BLIND`, `BLACK_MARKET_ACCESS`
   - Duration: 15 turns
   - Radius: 3 tiles
6. High ink cost: 45 ink (illegal edicts cost more)
7. Layer creates a zone where:
   - Trade restrictions ignored
   - Officials don't enforce laws
   - Black market merchants appear
8. Risk: If faction patrol enters zone, contradiction heat spikes

### 15.3 Scenario: Economic Warfare

**Goal:** Player wants to destabilize enemy faction's economy.

**Steps:**
1. Player travels to Inkguard-controlled district
2. Opens Ledger > Economy
3. Series of inscriptions:
   - `SCARCITY:sword` (Inkguard's primary trade good)
   - `TAX:0.40` (spike taxes to hurt commerce)
   - `TRADE_BAN:faction_inkbound` (cut off their ally)
4. Combined effects:
   - Sword prices double (scarcity)
   - All purchases cost 40% more (taxes)
   - Inkbound merchants can't trade here
5. Over time:
   - District prosperity drops
   - Inkguard treasury depletes
   - NPC unrest increases
   - District becomes contested

### 15.4 Scenario: Bribing a Tax Collector

**Goal:** Player wants permanent tax reduction without ink cost.

**Steps:**
1. Player locates Tax Collector NPC (Marcus)
2. Interacts with official
3. Official info displayed:
   - Title: Tax Collector
   - Disposition: Fair (moderate corruptibility)
   - Controls: Market Quarter sales tax
4. Player selects [Influence] > [Bribe]
5. Bribe cost: 500 coins
6. Success chance: 60% (based on corruptibility + player rep)
7. If successful:
   - Tax Collector enters BRIBED state
   - Sales tax reduced by 5%
   - Decay: 2% effectiveness lost per day
8. If failed:
   - Official refuses
   - Player reputation with controlling faction -10
   - May alert guards (disposition dependent)

---

## Appendix A: Economic Token Quick Reference

### Tax Tokens
| Token | Effect |
|-------|--------|
| `TAX:0.XX` | Set tax rate to XX% |
| `TAX_BREAK:0.XX` | Reduce taxes by XX% |
| `TAX_EXEMPT:faction_id` | Faction pays no taxes |
| `TAX_DOUBLE:faction_id` | Faction pays 2x taxes |

### Price Tokens
| Token | Effect |
|-------|--------|
| `PRICE_FLOOR:item:N` | Item minimum price N |
| `PRICE_CEILING:item:N` | Item maximum price N |
| `SUBSIDY:item:0.XX` | Item costs XX% less |
| `TARIFF:item:0.XX` | Item costs XX% more |
| `INFLATE:X.X` | All prices × X.X |
| `DEFLATE:X.X` | All prices ÷ X.X |

### Trade Tokens
| Token | Effect |
|-------|--------|
| `TRADE_BAN:faction` | Faction cannot trade |
| `TRADE_ONLY:faction` | Only faction can trade |
| `FREE_TRADE` | No tariffs/duties |
| `BLOCKADE` | No trade at all |
| `SMUGGLER_ZONE` | Ignore restrictions |

### Supply Tokens
| Token | Effect |
|-------|--------|
| `ABUNDANCE:item` | Double item supply |
| `SCARCITY:item` | Halve item supply |
| `DEMAND_SPIKE:item` | Triple demand |
| `PRODUCTION_BOOST` | All supply +50% |

### Official Tokens
| Token | Effect |
|-------|--------|
| `CORRUPT_OFFICIAL:id` | Official takes bribes |
| `OFFICIAL_BLIND` | No enforcement |
| `AUDIT_ZONE` | Double penalties |

---

## Appendix B: Ink Cost Reference

| Factor | Cost |
|--------|------|
| Base inscription | 5 ink |
| Per 1% tax change | 0.5 ink |
| Per turn duration | 0.5 ink |
| Per tile radius | 2 ink |
| Targets specific faction | ×1.5 |
| Targets specific item | ×1.2 |
| Contradicts existing layer | ×2.0 |
| Illegal effect | ×1.5 |

**Example:** TAX_BREAK:0.10, 15 turns, 4 radius, no faction
- Base: 5
- Tax (10%): 5
- Duration: 7.5
- Radius: 8
- **Total: 26 ink** (rounded)
