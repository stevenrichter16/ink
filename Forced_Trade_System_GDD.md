# Forced Trade System Design Document

## Overview

The **Forced Trade** system allows players to inscribe palimpsest layers that compel hostile factions to engage in commerce despite their enmity. This creates volatile, emergent gameplay where NPCs who hate each other are forced into proximity, building tension that eventually erupts into violence.

This document expands on the concept: *"Forcing trade open between hostile factions leads to trading, and subsequently breaking out into fights."*

---

## Table of Contents

1. [Core Concept](#1-core-concept)
2. [Trade Compulsion Mechanics](#2-trade-compulsion-mechanics)
3. [Tension System](#3-tension-system)
4. [Violence Escalation](#4-violence-escalation)
5. [NPC Behaviors During Forced Trade](#5-npc-behaviors-during-forced-trade)
6. [Market Brawl Events](#6-market-brawl-events)
7. [Faction Responses](#7-faction-responses)
8. [Player Manipulation Opportunities](#8-player-manipulation-opportunities)
9. [Economic Consequences](#9-economic-consequences)
10. [Palimpsest Integration](#10-palimpsest-integration)
11. [Implementation Architecture](#11-implementation-architecture)
12. [Example Scenarios](#12-example-scenarios)

---

## 1. Core Concept

### The Paradox of Forced Commerce

When you inscribe a `TRADE_OPEN` layer between hostile factions:

```
┌─────────────────────────────────────────────────────────────────┐
│                    FORCED TRADE LIFECYCLE                       │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│   INSCRIPTION          COMMERCE           TENSION              │
│   ──────────────────────────────────────────────────────────   │
│                                                                 │
│   Player inscribes     Hostile NPCs       Grudges don't        │
│   TRADE_OPEN layer     forced to trade    disappear...         │
│         │                   │                  │                │
│         ▼                   ▼                  ▼                │
│   ┌───────────┐       ┌───────────┐      ┌───────────┐         │
│   │ Palimpsest│  ───► │ Merchants │ ───► │ Tension   │         │
│   │ Active    │       │ Interact  │      │ Builds    │         │
│   └───────────┘       └───────────┘      └───────────┘         │
│                                                │                │
│                                                ▼                │
│   VIOLENCE            CONSEQUENCES       AFTERMATH             │
│   ──────────────────────────────────────────────────────────   │
│                                                                 │
│   Incident triggers    Brawl erupts,     Economic damage,      │
│   market brawl         rallies allies    faction retaliation   │
│         │                   │                  │                │
│         ▼                   ▼                  ▼                │
│   ┌───────────┐       ┌───────────┐      ┌───────────┐         │
│   │ Threshold │  ───► │ Combat    │ ───► │ Trade     │         │
│   │ Exceeded  │       │ Outbreak  │      │ Collapse  │         │
│   └───────────┘       └───────────┘      └───────────┘         │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```

### Design Philosophy

The system creates a **pressure cooker** dynamic:
- Trade IS happening (economic benefits flow)
- But hostility IS NOT resolved (just suppressed)
- Tension accumulates with each interaction
- Violence is inevitable without intervention
- Player can exploit the chaos or try to maintain peace

---

## 2. Trade Compulsion Mechanics

### 2.1 The TRADE_OPEN Token

```csharp
// New palimpsest token forcing trade between hostile factions
public class TradeOpenToken
{
    public string factionA;           // First faction ID
    public string factionB;           // Second faction ID
    public bool bidirectional;        // Both directions or one-way
    public float tensionBuildRate;    // How fast tension accumulates
    public float violenceThreshold;   // When brawls trigger (0.0 - 1.0)
}

// Token format examples:
// TRADE_OPEN:inkguard:ghost           - Bidirectional forced trade
// TRADE_OPEN:inkguard>ghost           - One-way (Inkguard must sell to Ghost)
// TRADE_OPEN:inkguard:ghost:volatile  - High tension build rate
```

### 2.2 Trade Compulsion Rules

When a `TRADE_OPEN` layer is active:

| Normal Behavior | Forced Trade Behavior |
|----------------|----------------------|
| Hostile merchants refuse service | Must accept transactions |
| NPCs attack on sight | Combat suppressed during trade |
| Factions avoid each other's territory | Must path to trade zones |
| Prices include hostility markup | Prices forced to neutral rates |

### 2.3 ForcedTradeZone Component

```csharp
public class ForcedTradeZone : MonoBehaviour
{
    [Header("Configuration")]
    public string factionA;
    public string factionB;
    public int radius;
    public float tensionBuildRate = 0.1f;     // Per interaction
    public float passiveTensionRate = 0.02f;  // Per turn while NPCs present
    public float violenceThreshold = 0.8f;

    [Header("State")]
    public float currentTension;               // 0.0 to 1.0
    public int interactionCount;
    public int incidentCount;                  // Near-misses
    public List<FactionMember> presentNPCs;

    [Header("Events")]
    public UnityEvent<float> OnTensionChanged;
    public UnityEvent OnBrawlTriggered;
    public UnityEvent OnTradeCompleted;

    public bool IsVolatile => currentTension >= 0.6f;
    public bool IsExplosive => currentTension >= violenceThreshold;
}
```

### 2.4 Merchant Behavior Override

```csharp
// In Merchant.cs - extend CanTradeWith()
public bool CanTradeWith(GridEntity customer)
{
    var myFaction = GetComponent<FactionMember>()?.faction;
    var theirFaction = customer.GetComponent<FactionMember>()?.faction;

    // Normal hostility check
    if (myFaction != null && theirFaction != null)
    {
        int interRep = ReputationSystem.GetInterRep(myFaction.id, theirFaction.id);

        if (interRep <= HostilityService.HostileThreshold)
        {
            // Check for forced trade layer
            var rules = OverlayResolver.GetRulesAt(gridX, gridY);
            if (rules.HasForcedTrade(myFaction.id, theirFaction.id))
            {
                // Forced to trade despite hostility
                OnForcedTradeInteraction(customer);
                return true;
            }

            // Normal refusal
            return false;
        }
    }

    return true;
}

private void OnForcedTradeInteraction(GridEntity customer)
{
    var zone = ForcedTradeZone.GetZoneAt(transform.position);
    if (zone != null)
    {
        zone.RecordInteraction(this, customer);
        zone.BuildTension(zone.tensionBuildRate);
    }
}
```

---

## 3. Tension System

### 3.1 Tension Accumulation

Tension builds through multiple sources:

| Source | Tension Gain | Notes |
|--------|--------------|-------|
| **Trade completed** | +0.05 | Successful but grudging commerce |
| **Price dispute** | +0.10 | Haggling between enemies |
| **Insult exchanged** | +0.08 | Dialogue triggers |
| **Near-miss bump** | +0.12 | NPCs accidentally touch |
| **Weapon displayed** | +0.15 | NPC drew weapon but didn't attack |
| **Ally killed elsewhere** | +0.25 | News of faction casualties |
| **Passive proximity** | +0.02/turn | Just being near each other |
| **Previous brawl memory** | +0.03/turn | Past violence not forgotten |

### 3.2 Tension Decay

Tension can decrease:

| Source | Tension Reduction | Notes |
|--------|-------------------|-------|
| **No NPCs present** | -0.05/turn | Zone cools down when empty |
| **Player mediation** | -0.10 | Dialogue option to calm things |
| **Bribe/gift exchange** | -0.15 | Economic peace offering |
| **Time since last incident** | -0.01/turn | Natural cooling |
| **CALM_ZONE inscription** | -0.20 | Stacks with TRADE_OPEN |
| **Festival/event** | -0.30 | Special peaceful occasions |

### 3.3 TensionManager System

```csharp
public static class ForcedTradeTensionManager
{
    private static Dictionary<string, ForcedTradeZone> _zones = new();

    public static void TickAllZones()
    {
        foreach (var zone in _zones.Values)
        {
            // Count NPCs from each faction present
            int factionACount = CountFactionMembers(zone, zone.factionA);
            int factionBCount = CountFactionMembers(zone, zone.factionB);

            if (factionACount > 0 && factionBCount > 0)
            {
                // Both factions present - tension builds
                float proximityMultiplier = Mathf.Min(factionACount, factionBCount);
                zone.BuildTension(zone.passiveTensionRate * proximityMultiplier);

                // Check for random incidents
                if (TryTriggerIncident(zone))
                {
                    zone.incidentCount++;
                    zone.BuildTension(0.12f);  // Near-miss bump
                }

                // Check for brawl
                if (zone.IsExplosive && Random.value < GetBrawlChance(zone))
                {
                    TriggerBrawl(zone);
                }
            }
            else
            {
                // One or no factions - tension decays
                zone.ReduceTension(0.05f);
            }

            zone.OnTensionChanged?.Invoke(zone.currentTension);
        }
    }

    private static float GetBrawlChance(ForcedTradeZone zone)
    {
        // Base 10% per turn when explosive, scaling with excess tension
        float excess = zone.currentTension - zone.violenceThreshold;
        return 0.1f + (excess * 2f);  // +20% per 0.1 over threshold
    }
}
```

### 3.4 Tension Indicators

Visual/audio feedback as tension rises:

| Tension Level | Visual Indicator | Audio | NPC Behavior |
|---------------|------------------|-------|--------------|
| 0.0 - 0.3 | Normal | Normal market sounds | Polite but curt |
| 0.3 - 0.5 | NPCs face each other | Muttering, whispers | Suspicious glances |
| 0.5 - 0.7 | Hands near weapons | Raised voices | Insults, shoving |
| 0.7 - 0.9 | Weapons drawn | Shouting, threats | Aggressive posturing |
| 0.9 - 1.0 | Combat stances | Combat music starts | About to snap |

```csharp
public class TensionIndicatorUI : MonoBehaviour
{
    public void UpdateIndicators(ForcedTradeZone zone)
    {
        float t = zone.currentTension;

        // Color the zone boundary
        zoneBoundary.color = Color.Lerp(Color.green, Color.red, t);

        // Particle effects
        tensionParticles.emissionRate = t * 50f;

        // NPC animation states
        foreach (var npc in zone.presentNPCs)
        {
            var animator = npc.GetComponent<Animator>();
            animator.SetFloat("Tension", t);

            if (t > 0.7f)
                animator.SetBool("WeaponDrawn", true);
        }

        // Audio
        if (t > 0.5f && !tensionAudioPlaying)
        {
            AudioManager.PlayLoop("market_tension");
            tensionAudioPlaying = true;
        }
    }
}
```

---

## 4. Violence Escalation

### 4.1 Incident Types (Pre-Violence)

Before a full brawl, incidents occur as warnings:

```csharp
public enum TensionIncident
{
    VerbalInsult,        // "Your faction are all thieves!"
    ShoveMatch,          // Physical contact, no damage
    DrawnWeapon,         // Weapon out but not swung
    TheftAccusation,     // "You cheated me!"
    SpitOnGround,        // Disrespect gesture
    BlockedPath,         // Refusing to let enemy pass
    PriceRefusal,        // "I won't pay that to YOUR kind!"
    AllyMention,         // "Remember what you did to [fallen ally]?"
    TerritoryTaunt,      // "This district should be OURS!"
}

public static class IncidentGenerator
{
    public static TensionIncident GenerateIncident(ForcedTradeZone zone)
    {
        float t = zone.currentTension;

        // Higher tension = more severe incidents
        if (t > 0.7f)
            return RandomFrom(DrawnWeapon, AllyMention, TerritoryTaunt);
        else if (t > 0.5f)
            return RandomFrom(ShoveMatch, TheftAccusation, PriceRefusal);
        else
            return RandomFrom(VerbalInsult, SpitOnGround, BlockedPath);
    }
}
```

### 4.2 Brawl Triggers

A brawl is triggered when:

| Trigger | Tension Required | Description |
|---------|------------------|-------------|
| **Tension overflow** | 100% | Accumulated tension hits max |
| **Random escalation** | 80%+ | Per-turn chance when explosive |
| **External attack** | 50%+ | Someone attacks anyone in zone |
| **Faction news** | 60%+ | News of ally death arrives |
| **Theft detected** | 40%+ | One NPC caught stealing |
| **Palimpsest contradiction** | 70%+ | Conflicting layer inscribed |
| **Player provocation** | 30%+ | Player insults either faction |
| **Alcohol/drug use** | 50%+ | Lowered inhibitions |

### 4.3 Brawl Mechanics

When a brawl triggers:

```csharp
public static class MarketBrawlService
{
    public static void TriggerBrawl(ForcedTradeZone zone)
    {
        Debug.Log($"[Brawl] FIGHT BREAKS OUT in {zone.name}!");
        zone.OnBrawlTriggered?.Invoke();

        // 1. Identify combatants
        var factionAMembers = GetFactionMembersInZone(zone, zone.factionA);
        var factionBMembers = GetFactionMembersInZone(zone, zone.factionB);

        // 2. All NPCs enter HOSTILE state toward opposite faction
        foreach (var member in factionAMembers)
        {
            var target = GetNearestEnemy(member, factionBMembers);
            member.EnterHostile(target?.GetComponent<GridEntity>());
            member.NotePlayerAttack();  // Prevents immediate cooldown
        }

        foreach (var member in factionBMembers)
        {
            var target = GetNearestEnemy(member, factionAMembers);
            member.EnterHostile(target?.GetComponent<GridEntity>());
            member.NotePlayerAttack();
        }

        // 3. Rally nearby allies (expand the brawl)
        int rallyRadius = 8;  // Larger than normal combat rally
        foreach (var member in factionAMembers.Concat(factionBMembers))
        {
            FactionMember.ForAlliesInRadius(member, rallyRadius, ally =>
            {
                if (ally.state != FactionMember.AlertState.Hostile)
                {
                    ally.EnterHostile(member.GetComponent<GridEntity>());
                    Debug.Log($"[Brawl] {ally.name} joins the fight!");
                }
            });
        }

        // 4. Disable trade in zone temporarily
        zone.SetTradeEnabled(false);
        zone.brawlCooldownTurns = 10;

        // 5. Alert player
        NotificationSystem.Show("A brawl has erupted in the market!", NotificationType.Combat);

        // 6. Start brawl event tracking
        BrawlEventTracker.StartTracking(zone);
    }
}
```

### 4.4 Brawl Escalation Stages

Brawls can escalate beyond the initial zone:

```
┌──────────────────────────────────────────────────────────────────┐
│                    BRAWL ESCALATION STAGES                       │
├──────────────────────────────────────────────────────────────────┤
│                                                                  │
│  STAGE 1: MARKET BRAWL                                          │
│  ────────────────────                                           │
│  • 2-6 combatants per side                                      │
│  • Contained to trade zone                                      │
│  • Guards may intervene                                         │
│  • Duration: 3-8 turns                                          │
│                                                                  │
│           │ (3+ casualties OR guard killed)                     │
│           ▼                                                      │
│                                                                  │
│  STAGE 2: DISTRICT RIOT                                         │
│  ──────────────────────                                         │
│  • All faction members in district join                         │
│  • Spreads to adjacent tiles                                    │
│  • Shops close, civilians flee                                  │
│  • Duration: 10-20 turns                                        │
│                                                                  │
│           │ (faction leader killed OR 10+ casualties)           │
│           ▼                                                      │
│                                                                  │
│  STAGE 3: FACTION WAR                                           │
│  ────────────────────                                           │
│  • All-out hostility between factions                           │
│  • Inter-faction rep drops to minimum                           │
│  • Trade completely halted                                      │
│  • Duration: Until peace negotiated                             │
│                                                                  │
└──────────────────────────────────────────────────────────────────┘
```

---

## 5. NPC Behaviors During Forced Trade

### 5.1 Reluctant Trader AI

NPCs forced to trade show their displeasure:

```csharp
public class ReluctantTraderBehavior : MonoBehaviour
{
    private FactionMember _faction;
    private Merchant _merchant;
    private List<string> _hostileFactions;

    public void OnCustomerApproach(GridEntity customer)
    {
        var theirFaction = customer.GetComponent<FactionMember>()?.faction;
        if (theirFaction != null && _hostileFactions.Contains(theirFaction.id))
        {
            // Show reluctance
            PlayDialogue(GetReluctantGreeting());
            animator.SetTrigger("Grimace");

            // Increase zone tension
            var zone = ForcedTradeZone.GetZoneAt(transform.position);
            zone?.BuildTension(0.02f);
        }
    }

    public void OnTransactionComplete(GridEntity customer, bool wasHostile)
    {
        if (wasHostile)
        {
            // Post-trade resentment
            var responses = new[]
            {
                "Take it and go.",
                "*mutters under breath*",
                "Don't come back.",
                "This changes nothing between our people.",
                "*glares silently*",
                "Blood money..."
            };

            PlayDialogue(responses.Random());

            // Small chance of incident
            if (Random.value < 0.15f)
            {
                var zone = ForcedTradeZone.GetZoneAt(transform.position);
                zone?.RecordIncident(TensionIncident.VerbalInsult);
            }
        }
    }

    private string GetReluctantGreeting()
    {
        var greetings = new[]
        {
            "You. What do you want?",
            "*sighs heavily* ...What?",
            "Make it quick.",
            "The inscription forces my hand. Speak.",
            "I serve you only because the palimpsest compels me.",
            "Don't mistake commerce for friendship.",
            "Your coin spends, even if your kind doesn't.",
        };
        return greetings.Random();
    }
}
```

### 5.2 Hostile Customer AI

Hostile faction members shopping exhibit behaviors:

```csharp
public class HostileCustomerBehavior : MonoBehaviour
{
    public void OnEnterForcedTradeZone(ForcedTradeZone zone)
    {
        // Check if any hostile merchants present
        var hostileMerchants = zone.GetMerchantsHostileTo(faction.id);

        if (hostileMerchants.Count > 0)
        {
            // Behavioral modifiers
            movementSpeed *= 0.8f;  // Move cautiously
            animator.SetBool("OnGuard", true);
            handNearWeapon = true;

            // Internal monologue (thought bubble or subtle indicator)
            ThoughtBubble.Show(GetHostileThought());
        }
    }

    public void OnNearHostileNPC(FactionMember hostile, float distance)
    {
        if (distance < 2f)
        {
            // Too close - incident chance
            if (Random.value < 0.1f)
            {
                var incident = distance < 1f
                    ? TensionIncident.ShoveMatch
                    : TensionIncident.BlockedPath;

                ForcedTradeZone.GetZoneAt(transform.position)
                    ?.RecordIncident(incident);

                // Dialogue exchange
                DialogueService.PlayExchange(this, hostile, GetConfrontationDialogue());
            }
        }
    }

    private string[] GetHostileThought()
    {
        return new[]
        {
            "(Keep calm... just get what you need...)",
            "(Don't let them provoke you...)",
            "(Remember why you're here...)",
            "(Their blood would stain this market...)",
            "(The inscription protects them. For now.)",
        };
    }
}
```

### 5.3 NPC Pathfinding Under Forced Trade

NPCs modify their pathfinding in forced trade zones:

```csharp
public class ForcedTradePathfinding : MonoBehaviour
{
    public void ModifyPath(List<Vector2Int> path, ForcedTradeZone zone)
    {
        var hostileNPCs = zone.GetHostileNPCsTo(myFaction.id);

        foreach (var hostile in hostileNPCs)
        {
            // Add pathfinding penalty near hostile NPCs
            var hostilePos = hostile.GetComponent<GridEntity>().GridPosition;

            // Prefer paths that don't get too close
            int avoidanceRadius = 3;
            pathfinder.AddTemporaryPenalty(hostilePos, avoidanceRadius, 50);
        }

        // But still must reach destination (can't leave zone without trading)
        if (forcedTradeObjective != null)
        {
            pathfinder.SetMandatoryWaypoint(forcedTradeObjective.position);
        }
    }
}
```

---

## 6. Market Brawl Events

### 6.1 Brawl Event Structure

```csharp
public class MarketBrawlEvent
{
    public string id;
    public ForcedTradeZone zone;
    public string factionA;
    public string factionB;

    public BrawlStage stage;
    public int turnsSinceStart;
    public int totalCasualties;

    public List<FactionMember> factionACombatants;
    public List<FactionMember> factionBCombatants;
    public List<FactionMember> casualties;

    public bool guardIntervention;
    public bool playerInvolved;

    public Dictionary<string, int> factionCasualties;  // Per faction death count
}

public enum BrawlStage
{
    Skirmish,     // Initial outbreak
    Brawl,        // Full market fight
    Riot,         // Spreading beyond zone
    Battle,       // District-wide conflict
    War           // Faction-wide war state
}
```

### 6.2 Brawl Resolution

How brawls end:

| Resolution | Condition | Aftermath |
|------------|-----------|-----------|
| **One side flees** | 75% casualties on one side | Winners claim zone |
| **Guard intervention** | Guards kill 2+ combatants | Both factions penalized |
| **Mutual exhaustion** | 50%+ casualties both sides | Zone marked as dangerous |
| **Player intervention** | Player defeats aggressors | Reputation with defenders |
| **Natural timeout** | 15+ turns | Combatants disengage |
| **Faction retreat** | Leader calls withdrawal | Tactical loss, saves lives |

```csharp
public static class BrawlResolutionService
{
    public static void CheckResolution(MarketBrawlEvent brawl)
    {
        float aRemaining = GetRemainingRatio(brawl, brawl.factionA);
        float bRemaining = GetRemainingRatio(brawl, brawl.factionB);

        // One side routed
        if (aRemaining < 0.25f)
        {
            ResolveBrawl(brawl, BrawlOutcome.FactionBVictory);
        }
        else if (bRemaining < 0.25f)
        {
            ResolveBrawl(brawl, BrawlOutcome.FactionAVictory);
        }
        // Mutual destruction
        else if (aRemaining < 0.5f && bRemaining < 0.5f)
        {
            ResolveBrawl(brawl, BrawlOutcome.MutualDestruction);
        }
        // Timeout
        else if (brawl.turnsSinceStart >= 15)
        {
            ResolveBrawl(brawl, BrawlOutcome.Exhaustion);
        }
    }

    private static void ResolveBrawl(MarketBrawlEvent brawl, BrawlOutcome outcome)
    {
        // Apply inter-faction reputation damage
        int repDamage = -10 * brawl.totalCasualties;
        ReputationSystem.AddInterRep(brawl.factionA, brawl.factionB, repDamage);
        ReputationSystem.AddInterRep(brawl.factionB, brawl.factionA, repDamage);

        // Zone aftermath
        brawl.zone.currentTension = 0.3f;  // Reset but not zero
        brawl.zone.brawlHistory.Add(brawl);
        brawl.zone.SetTradeEnabled(true);

        // Mark zone as volatile (increases future tension rate)
        brawl.zone.tensionBuildRate *= 1.5f;

        // Notify systems
        OnBrawlResolved?.Invoke(brawl, outcome);
    }
}
```

### 6.3 Brawl Aftermath Effects

After a brawl, the zone is changed:

```csharp
public class BrawlAftermathEffects
{
    public void ApplyAftermath(ForcedTradeZone zone, MarketBrawlEvent brawl)
    {
        // 1. Economic damage
        zone.economicPenalty = 0.5f;  // 50% reduced trade volume
        zone.economicRecoveryTurns = brawl.totalCasualties * 3;

        // 2. Visual changes
        SpawnBloodstains(zone, brawl.casualties);
        SpawnBrokenStalls(zone, Random.Range(1, 4));
        DamageNearbyProps(zone);

        // 3. NPC behavior changes
        foreach (var merchant in zone.GetMerchants())
        {
            merchant.fearLevel += 0.3f;
            merchant.priceModifier += 0.1f;  // Risk premium
        }

        // 4. Faction memory
        FactionMemoryService.RecordEvent(brawl.factionA, new FactionMemory
        {
            type = MemoryType.MarketBrawl,
            targetFaction = brawl.factionB,
            location = zone.id,
            casualties = brawl.factionCasualties[brawl.factionA],
            turnsAgo = 0
        });

        // 5. Guard patrol increase
        if (zone.controllingFaction != null)
        {
            DistrictControlService.IncreasePatrol(zone.districtId, 0.2f);
        }
    }
}
```

---

## 7. Faction Responses

### 7.1 Faction Memory of Brawls

Factions remember market brawls and adjust behavior:

```csharp
public class FactionBrawlMemory
{
    public string enemyFactionId;
    public string location;
    public int casualtiesSuffered;
    public int casualtiesInflicted;
    public int daysSinceBrawl;

    public float GetResentment()
    {
        // High casualties = high resentment
        // Decays slowly over time
        float baseFactor = casualtiesSuffered * 10f;
        float decayFactor = Mathf.Pow(0.95f, daysSinceBrawl);
        return baseFactor * decayFactor;
    }

    public float GetConfidence()
    {
        // Won the brawl = more likely to start another
        if (casualtiesInflicted > casualtiesSuffered)
            return 1.2f;
        return 0.8f;
    }
}
```

### 7.2 Faction Strategic Responses

After brawls, factions may:

| Response | Trigger | Effect |
|----------|---------|--------|
| **Reinforce** | Lost brawl | Send more NPCs to zone |
| **Withdraw** | Heavy losses | Pull merchants from zone |
| **Revenge raid** | 5+ casualties | Attack enemy territory |
| **Diplomatic protest** | Any brawl | Demand inscription removal |
| **Assassinate inscriber** | Repeated brawls | Hunt the player |
| **Counter-inscription** | Multiple losses | Inscribe opposing layer |
| **Hire mercenaries** | Losing pattern | Bring in neutral fighters |

```csharp
public static class FactionStrategicResponse
{
    public static void EvaluateResponse(string factionId, MarketBrawlEvent brawl)
    {
        var faction = FactionRegistry.GetById(factionId);
        var memory = FactionMemoryService.GetBrawlMemory(factionId, brawl.zone.id);

        // Calculate response weight
        float urgency = memory.Sum(m => m.GetResentment());
        float strength = GetFactionStrength(factionId, brawl.zone);

        if (urgency > 50 && strength > 0.5f)
        {
            // Strong and angry - counterattack
            QueueStrategicAction(new FactionAction
            {
                type = FactionActionType.RevengeRaid,
                targetZone = GetEnemyTerritory(brawl.enemyFaction),
                priority = urgency / 100f
            });
        }
        else if (urgency > 30 && strength < 0.3f)
        {
            // Weak but angry - withdraw and regroup
            QueueStrategicAction(new FactionAction
            {
                type = FactionActionType.StrategicWithdrawal,
                targetZone = brawl.zone.id,
                priority = 0.8f
            });
        }
        else if (urgency > 20)
        {
            // Moderate - reinforce position
            QueueStrategicAction(new FactionAction
            {
                type = FactionActionType.Reinforce,
                targetZone = brawl.zone.id,
                priority = 0.5f
            });
        }
    }
}
```

### 7.3 Faction Dialogue About Brawls

NPCs reference past brawls in dialogue:

```csharp
public class BrawlAwareDialogue : DialogueProvider
{
    public override DialogueSequence GetDialogue(FactionMember speaker, GridEntity target)
    {
        var zone = ForcedTradeZone.GetZoneAt(speaker.transform.position);
        if (zone == null) return base.GetDialogue(speaker, target);

        var recentBrawl = zone.brawlHistory
            .Where(b => b.turnsSinceResolution < 50)
            .OrderByDescending(b => b.totalCasualties)
            .FirstOrDefault();

        if (recentBrawl != null)
        {
            bool wasVictim = recentBrawl.factionCasualties
                .GetValueOrDefault(speaker.faction.id) > 0;

            if (wasVictim)
            {
                return new DialogueSequence(
                    "We lost good people here. The inscription may force trade...",
                    "But it cannot force forgiveness.",
                    "Every transaction reminds us of what they took."
                );
            }
            else
            {
                return new DialogueSequence(
                    "They started it. We finished it.",
                    "Let them come back. We're ready.",
                    "The market runs red when they push too far."
                );
            }
        }

        return base.GetDialogue(speaker, target);
    }
}
```

---

## 8. Player Manipulation Opportunities

### 8.1 Chaos Agent Strategy

Players can exploit forced trade for profit/power:

```
┌─────────────────────────────────────────────────────────────────┐
│                  PLAYER MANIPULATION STRATEGIES                  │
├─────────────────────────────────────────────────────────────────┤
│                                                                  │
│  STRATEGY: PROFITEER                                            │
│  ───────────────────                                            │
│  1. Inscribe TRADE_OPEN between hostile factions                │
│  2. Buy goods cheap from one faction                            │
│  3. Sell to the other at markup (they're desperate)             │
│  4. When brawl happens, loot the aftermath                      │
│  5. Repeat in new location                                      │
│                                                                  │
│  STRATEGY: KINGMAKER                                            │
│  ───────────────────                                            │
│  1. Inscribe TRADE_OPEN in enemy faction's territory            │
│  2. Wait for their rivals to show up                            │
│  3. Secretly boost tension (insults, theft accusations)         │
│  4. When brawl erupts, help your preferred faction              │
│  5. Claim reward from grateful winners                          │
│                                                                  │
│  STRATEGY: PEACEKEEPER                                          │
│  ───────────────────                                            │
│  1. Inscribe TRADE_OPEN to normalize relations                  │
│  2. Also inscribe CALM_ZONE to reduce tension                   │
│  3. Mediate disputes when they arise                            │
│  4. Build reputation with both factions                         │
│  5. Eventually achieve true peace (trade without inscription)   │
│                                                                  │
│  STRATEGY: ARSONIST                                             │
│  ───────────────────                                            │
│  1. Inscribe TRADE_OPEN between two strong factions             │
│  2. Inscribe contradicting layers to spike heat                 │
│  3. Personally provoke NPCs to accelerate tension               │
│  4. Evacuate before the explosion                               │
│  5. Return to weakened factions and take over                   │
│                                                                  │
└─────────────────────────────────────────────────────────────────┘
```

### 8.2 Player Actions That Affect Tension

| Action | Tension Effect | Risk |
|--------|----------------|------|
| **Insult faction NPC** | +0.15 | Hostility toward player |
| **Spread rumors** | +0.10 | May be caught lying |
| **Frame for theft** | +0.25 | High if discovered |
| **Buy all stock** | +0.08 | Creates scarcity |
| **Gift to one faction** | +0.05 (other jealous) | None |
| **Mediate dispute** | -0.15 | None |
| **Bribe both sides** | -0.20 | Expensive |
| **Kill NPC secretly** | +0.30 | Body may be found |
| **Reveal past crimes** | +0.20 | Need evidence |

### 8.3 Tension Manipulation Items

Consumable items that affect forced trade zones:

```csharp
public class TensionManipulationItems
{
    // Increases tension
    public static ItemData IncitingPamphlet = new ItemData
    {
        id = "inciting_pamphlet",
        name = "Inciting Pamphlet",
        description = "Propaganda that inflames faction hatred.",
        useEffect = (user, target) =>
        {
            var zone = ForcedTradeZone.GetZoneAt(target);
            zone?.BuildTension(0.15f);
            zone?.RecordIncident(TensionIncident.AllyMention);
        }
    };

    // Decreases tension
    public static ItemData CalmingIncense = new ItemData
    {
        id = "calming_incense",
        name = "Calming Incense",
        description = "Fragrant smoke that soothes tempers.",
        useEffect = (user, target) =>
        {
            var zone = ForcedTradeZone.GetZoneAt(target);
            zone?.ReduceTension(0.20f);
        }
    };

    // Triggers immediate brawl
    public static ItemData BloodVial = new ItemData
    {
        id = "blood_vial",
        name = "Vial of Faction Blood",
        description = "The blood of a fallen faction member. Throw to enrage their allies.",
        useEffect = (user, target) =>
        {
            var zone = ForcedTradeZone.GetZoneAt(target);
            if (zone != null && zone.currentTension > 0.4f)
            {
                MarketBrawlService.TriggerBrawl(zone);
            }
            else
            {
                zone?.BuildTension(0.35f);
            }
        }
    };
}
```

---

## 9. Economic Consequences

### 9.1 Trade Volume Effects

Forced trade affects economic metrics:

```csharp
public class ForcedTradeEconomics
{
    public static float GetTradeVolumeModifier(ForcedTradeZone zone)
    {
        float modifier = 1.0f;

        // Base penalty for forced trade (reluctant participants)
        modifier *= 0.8f;

        // Tension reduces trade
        modifier *= 1f - (zone.currentTension * 0.5f);

        // Recent brawls devastate trade
        var recentBrawl = zone.brawlHistory
            .FirstOrDefault(b => b.turnsSinceResolution < 20);
        if (recentBrawl != null)
        {
            float brawlPenalty = 0.5f - (recentBrawl.turnsSinceResolution * 0.025f);
            modifier *= Mathf.Max(0.1f, 1f - brawlPenalty);
        }

        return modifier;
    }

    public static float GetPriceVolatility(ForcedTradeZone zone)
    {
        // High tension = wild price swings
        return 1f + (zone.currentTension * 2f);
    }
}
```

### 9.2 Supply Chain Disruption

Brawls disrupt goods flow:

```csharp
public static void ApplyBrawlSupplyDisruption(MarketBrawlEvent brawl)
{
    var zone = brawl.zone;
    var district = DistrictControlService.GetDistrict(zone.districtId);

    // Goods from both factions become scarce
    foreach (var itemId in GetFactionTradeGoods(brawl.factionA))
    {
        district.ReduceSupply(itemId, 0.5f);  // 50% reduction
    }
    foreach (var itemId in GetFactionTradeGoods(brawl.factionB))
    {
        district.ReduceSupply(itemId, 0.5f);
    }

    // Trade routes through zone become "Dangerous"
    foreach (var route in TradeRouteRegistry.GetRoutesThrough(zone.id))
    {
        route.status = TradeRouteStatus.Dangerous;
        route.efficiency *= 0.5f;
    }

    // Recovery over time
    ScheduleRecovery(zone, brawl.totalCasualties * 5);  // Days to recover
}
```

### 9.3 Price Effects During Tension

As tension rises, prices become erratic:

| Tension | Buy Price | Sell Price | Volatility |
|---------|-----------|------------|------------|
| 0.0-0.3 | Normal | Normal | ±5% |
| 0.3-0.5 | +10% | -5% | ±15% |
| 0.5-0.7 | +20% | -10% | ±25% |
| 0.7-0.9 | +40% | -20% | ±40% |
| 0.9-1.0 | +60% (if trading at all) | -30% | ±60% |

```csharp
public static int GetTensionAdjustedPrice(int basePrice, ForcedTradeZone zone, bool isBuying)
{
    float t = zone.currentTension;
    float modifier;

    if (isBuying)
    {
        // Merchants charge more as tension rises (risk premium)
        modifier = 1f + (t * 0.6f);
    }
    else
    {
        // Merchants pay less (they're nervous about inventory)
        modifier = 1f - (t * 0.3f);
    }

    // Add volatility
    float volatility = t * 0.6f;
    modifier *= 1f + Random.Range(-volatility, volatility);

    return Mathf.Max(1, Mathf.RoundToInt(basePrice * modifier));
}
```

---

## 10. Palimpsest Integration

### 10.1 New Tokens for Forced Trade

```yaml
# In PalimpsestTokenRegistry.asset

# Basic forced trade
- token: TRADE_OPEN:inkguard:ghost
  forcedTrade: true
  factionA: inkguard
  factionB: ghost
  tensionRate: 0.1
  violenceThreshold: 0.8

# High-tension variant
- token: TRADE_VOLATILE:inkguard:ghost
  forcedTrade: true
  factionA: inkguard
  factionB: ghost
  tensionRate: 0.2
  violenceThreshold: 0.6

# One-way trade
- token: TRADE_TRIBUTE:ghost>inkguard
  forcedTrade: true
  factionA: ghost
  factionB: inkguard
  oneWay: true
  tensionRate: 0.15

# Trade with tension suppression
- token: TRADE_PEACE:inkguard:ghost
  forcedTrade: true
  factionA: inkguard
  factionB: ghost
  tensionRate: 0.05
  violenceThreshold: 0.95
  calmZone: true
```

### 10.2 Layer Contradiction Heat

When TRADE_OPEN conflicts with other layers:

| Conflict | Heat Generated | Result |
|----------|----------------|--------|
| TRADE_OPEN + EMBARGO (same factions) | Very High | Rapid layer decay |
| TRADE_OPEN + HUNT (one of the factions) | High | Violence threshold lowered |
| TRADE_OPEN + TRUCE | Low | Tension rate reduced |
| TRADE_OPEN + TRADE_OPEN (overlapping) | Medium | Both zones compete |
| TRADE_OPEN + ALLY (one faction) | Medium | One-sided peace |

```csharp
public static float CalculateForcedTradeHeat(ForcedTradeZone zone)
{
    float heat = 0f;
    var layers = OverlayResolver.GetLayersAt(zone.center.x, zone.center.y);

    foreach (var layer in layers)
    {
        foreach (var token in layer.tokens)
        {
            // Check for contradictions
            if (IsEmbargo(token, zone.factionA, zone.factionB))
                heat += 0.4f;  // Direct contradiction

            if (IsHunt(token, zone.factionA) || IsHunt(token, zone.factionB))
                heat += 0.25f; // One side being hunted

            if (IsTruce(token))
                heat -= 0.15f; // Truce reduces heat

            if (IsAlly(token, zone.factionA) || IsAlly(token, zone.factionB))
                heat -= 0.1f;  // Alliance helps
        }
    }

    return Mathf.Clamp(heat, -0.3f, 0.5f);
}
```

### 10.3 Decay Acceleration

Forced trade layers decay faster when:

```csharp
public static float GetForcedTradeDecayRate(PalimpsestLayer layer, ForcedTradeZone zone)
{
    float rate = 1.0f;  // Normal decay

    // High tension accelerates decay
    rate += zone.currentTension * 0.5f;

    // Each brawl doubles decay rate
    rate *= Mathf.Pow(2f, zone.brawlHistory.Count(b => b.turnsSinceResolution < 30));

    // Contradiction heat accelerates decay
    rate += CalculateForcedTradeHeat(zone);

    // Faction resistance (they're trying to break the inscription)
    rate += GetFactionResistanceLevel(zone.factionA, zone.factionB);

    return rate;
}
```

---

## 11. Implementation Architecture

### 11.1 System Overview

```
┌─────────────────────────────────────────────────────────────────┐
│                  FORCED TRADE SYSTEM ARCHITECTURE               │
├─────────────────────────────────────────────────────────────────┤
│                                                                  │
│  ┌──────────────────────────────────────────────────────────┐   │
│  │                  ForcedTradeManager                       │   │
│  │  (Singleton - manages all forced trade zones)             │   │
│  └────────────────────────┬─────────────────────────────────┘   │
│                           │                                      │
│           ┌───────────────┼───────────────┐                     │
│           ▼               ▼               ▼                     │
│  ┌────────────────┐ ┌────────────┐ ┌────────────────┐          │
│  │ForcedTradeZone │ │ TensionMgr │ │ BrawlService   │          │
│  │(per-zone state)│ │(tick logic)│ │(combat events) │          │
│  └────────┬───────┘ └─────┬──────┘ └────────┬───────┘          │
│           │               │                  │                   │
│           ▼               ▼                  ▼                   │
│  ┌────────────────────────────────────────────────────────┐     │
│  │              Integration Points                         │     │
│  ├────────────────────────────────────────────────────────┤     │
│  │ • Merchant.CanTradeWith() - forced trade check         │     │
│  │ • HostilityService.IsHostile() - combat suppression    │     │
│  │ • FactionMember state - reluctant animations           │     │
│  │ • EconomicPriceResolver - tension price modifiers      │     │
│  │ • OverlayResolver - TRADE_OPEN token parsing           │     │
│  │ • DistrictControlService - patrol response             │     │
│  └────────────────────────────────────────────────────────┘     │
│                                                                  │
│  ┌──────────────────────────────────────────────────────────┐   │
│  │                    UI Components                          │   │
│  │  ┌─────────────┐  ┌──────────────┐  ┌────────────────┐   │   │
│  │  │TensionMeter │  │BrawlAlert    │  │ZoneBoundaryVFX │   │   │
│  │  └─────────────┘  └──────────────┘  └────────────────┘   │   │
│  └──────────────────────────────────────────────────────────┘   │
│                                                                  │
└─────────────────────────────────────────────────────────────────┘
```

### 11.2 File Structure

```
Assets/Ink/Gameplay/
├── Economy/
│   └── ForcedTrade/
│       ├── ForcedTradeManager.cs
│       ├── ForcedTradeZone.cs
│       ├── ForcedTradeTensionManager.cs
│       ├── MarketBrawlService.cs
│       ├── MarketBrawlEvent.cs
│       ├── BrawlResolutionService.cs
│       ├── BrawlAftermathEffects.cs
│       ├── ReluctantTraderBehavior.cs
│       ├── HostileCustomerBehavior.cs
│       ├── ForcedTradeEconomics.cs
│       ├── FactionStrategicResponse.cs
│       └── TensionManipulationItems.cs
│
├── Palimpsest/
│   └── Tokens/
│       └── ForcedTradeTokenParser.cs
│
├── UI/
│   ├── TensionMeterUI.cs
│   ├── BrawlAlertUI.cs
│   └── ForcedTradeZoneVFX.cs
│
└── Factions/
    ├── FactionBrawlMemory.cs
    └── BrawlAwareDialogue.cs
```

### 11.3 Core Integration: HostilityService

Modify `HostilityService.cs` to handle forced trade:

```csharp
// In HostilityService.IsHostile()
public static bool IsHostile(GridEntity attacker, GridEntity target)
{
    // ... existing checks ...

    // Check for forced trade zone suppression
    var zone = ForcedTradeZone.GetZoneAt(target.gridX, target.gridY);
    if (zone != null && zone.IsActive)
    {
        var attackerFaction = GetFaction(attacker)?.id;
        var targetFaction = GetFaction(target)?.id;

        // If both are in the forced trade pair, suppress combat
        // UNLESS a brawl is active
        if (zone.IsForcedTradePair(attackerFaction, targetFaction))
        {
            if (!zone.IsBrawlActive)
            {
                // Suppress hostility but track tension
                zone.RecordHostilitySuppressionEvent(attacker, target);
                return false;
            }
            // During brawl, hostility is NOT suppressed
        }
    }

    // ... rest of existing logic ...
}
```

---

## 12. Example Scenarios

### 12.1 Scenario: The Reluctant Marketplace

**Setup:**
- Inkguard and Ghost factions are hostile (inter-rep: -60)
- Player needs goods from both
- No merchants will trade with both factions

**Player Actions:**
1. Inscribes `TRADE_OPEN:inkguard:ghost` at the central market (15 ink)
2. Layer activates with 5 tile radius, 30 turn duration

**Immediate Effects:**
- Ghost merchant who was hostile now says: *"The inscription compels me. What do you want?"*
- Inkguard guard lowers weapon but glares at Ghost customers
- Tension meter appears: 0%

**Over Time (Turns 1-10):**
- Trade happens, both factions profit
- Tension rises to 35% from proximity
- Incidents: 2 verbal insults, 1 shove match
- NPCs start muttering about "the other side"

**Escalation (Turns 11-20):**
- Tension hits 65%
- Inkguard merchant accuses Ghost customer of theft
- Weapons drawn but not used
- Player can intervene to reduce tension (-15%)

**Climax (Turn 23):**
- Tension hits 82%
- Ghost NPC mentions Inkguard killing their brother last month
- Brawl triggers
- 4 vs 3 combat in the market
- 2 casualties (1 per side)

**Aftermath:**
- Layer still active but trade suspended for 10 turns
- Zone marked as volatile (+50% tension rate)
- Both factions remember the fight
- Player can profit from aftermath loot

### 12.2 Scenario: The Kingmaker's Gambit

**Setup:**
- Player is friendly with Slime faction (+40 rep)
- Player is hostile with Inkbound faction (-50 rep)
- Slime and Inkbound are neutral (inter-rep: 0)

**Strategy:**
1. Player inscribes `TRADE_OPEN:slime:inkbound` in Inkbound territory
2. This forces Inkbound to trade with Slime (no immediate conflict)
3. Player inscribes `TRADE_VOLATILE` variant (higher tension rate)
4. Player waits for tension to build

**Manipulation:**
- Player uses `Inciting Pamphlet` near Inkbound NPCs (+15% tension)
- Player spreads rumor that Slime merchant is a spy
- Tension spikes to 60%

**Provocation:**
- Player "accidentally" knocks over Inkbound goods near Slime merchant
- Inkbound blames Slime
- Brawl triggers at 72% tension

**Outcome:**
- Player helps Slime during brawl (no rep cost - already hostile to Inkbound)
- Slime wins, gains confidence
- Inkbound weakened in this district
- Player's Slime rep increases +15
- Inkbound now also hostile to Slime (inter-rep drops to -40)

### 12.3 Scenario: The Peacekeeper's Path

**Setup:**
- Inkguard and Ghost have been hostile for generations
- Player wants true peace between them
- Inter-rep: -80

**Long-term Strategy:**

**Phase 1: Forced Commerce (Days 1-30)**
1. Inscribe `TRADE_PEACE:inkguard:ghost` (low tension variant)
2. Also inscribe `CALM_ZONE` in same area
3. Personally mediate any disputes that arise
4. Use `Calming Incense` when tension spikes

**Phase 2: Economic Interdependence (Days 31-60)**
1. Ensure each faction needs goods the other produces
2. Use `SUBSIDY` tokens to make cross-faction trade profitable
3. Let them see the economic benefits
4. Inter-faction rep slowly improves through successful trade

**Phase 3: Relationship Building (Days 61-90)**
1. Introduce faction leaders through dialogue
2. Complete quests that benefit both factions
3. Remove the `TRADE_OPEN` inscription
4. Trade continues voluntarily (inter-rep now -20)

**Phase 4: True Peace (Days 91+)**
1. Facilitate official peace treaty
2. Inter-rep rises above neutral
3. Former enemies now trade freely
4. Player gains "Peacemaker" reputation with both factions

---

## Appendix A: Tension Quick Reference

### Tension Sources
| Source | Change |
|--------|--------|
| Trade completed | +0.05 |
| Price dispute | +0.10 |
| Insult | +0.08 |
| Physical bump | +0.12 |
| Weapon drawn | +0.15 |
| Ally killed elsewhere | +0.25 |
| Passive proximity | +0.02/turn |
| Brawl memory | +0.03/turn |
| Player mediation | -0.15 |
| Bribe exchange | -0.15 |
| No NPCs present | -0.05/turn |
| CALM_ZONE active | -0.20 |

### Tension Thresholds
| Level | Range | Visual | Behavior |
|-------|-------|--------|----------|
| Calm | 0-30% | Green | Normal trade |
| Uneasy | 30-50% | Yellow | Suspicious glances |
| Tense | 50-70% | Orange | Insults, shoving |
| Volatile | 70-90% | Red | Weapons drawn |
| Explosive | 90-100% | Flashing | Brawl imminent |

---

## Appendix B: Brawl Resolution Reference

### Outcomes
| Outcome | Condition | Result |
|---------|-----------|--------|
| Victory A | B < 25% | A claims zone |
| Victory B | A < 25% | B claims zone |
| Mutual Destruction | Both < 50% | Zone dangerous |
| Guard Victory | Guards kill 2+ | Both penalized |
| Exhaustion | 15+ turns | Draw |
| Player Intervention | Player decides | Flexible |

### Aftermath Duration
| Severity | Recovery Time |
|----------|---------------|
| Minor (1-2 casualties) | 5 days |
| Moderate (3-5 casualties) | 15 days |
| Severe (6+ casualties) | 30 days |
| Escalated to riot | 60 days |
