# How Factions Work

## Core Concepts
- **Faction**: Defines shared rules for NPCs/enemies (id, displayName, defaultReputation, disposition, ranks, level profile).
- **Reputation (rep)**: Integer per faction stored in `ReputationSystem`. Thresholds in `HostilityService`:  
  - Friendly ≥ `FriendlyThreshold` (25)  
  - Hostile ≤ `HostileThreshold` (-25)  
  - Between is Neutral.
- **Disposition** (`FactionDefinition.FactionDisposition`):  
  - **Calm**: Neutral members will not retaliate to a single player hit (they only enter Alert if rep is friendly; otherwise Hostile only when rep already hostile).  
  - **Aggressive**: Neutral members retaliate immediately when attacked.

## Member State Machine (per `FactionMember`)
- **CALM**: Default. No target.  
- **ALERT**: Cautious; no attacks. Has `alertTurnsRemaining` (default from faction). Shows in logs as “entering ALERT”.  
- **HOSTILE**: Actively attacks assigned target (usually player).

### Transitions
| Event | From | To | Notes |
| --- | --- | --- | --- |
| Player hit (rep ≤ Hostile) | any | HOSTILE | Immediate retaliation. |
| Player hit (rep Neutral, aggressive disposition) | CALM | HOSTILE | Victim + allies within `rallyRadius` retaliate. |
| Player hit (rep Neutral, calm disposition) | CALM | ALERT | Victim only; no retaliation unless hit again while alert. |
| Player hit (rep Friendly/Positive) first time | CALM | ALERT | Victim + nearby allies enter ALERT. |
| Player hit again while ALERT (rep Friendly/Positive) | ALERT | HOSTILE | Victim + rallied allies flip to HOSTILE. |
| Player kill (rep > Hostile) | any non-Hostile | HOSTILE | Allies in `rallyRadius` also HOSTILE; applies `repOnKill`. |
| Alert timer expires | ALERT | CALM | `alertTurnsRemaining` ticks in `TickAlert()`. |
| Forgiveness cooldown | HOSTILE | ALERT | After 5 turns without further player attacks (and rep not hostile). |
| Forgiveness cooldown | ALERT | CALM | After 7 turns without player attacks (and rep not hostile). |

## Timers & Cooldowns
- `alertDurationTurns` (default 2) set on faction; decremented each member turn via `TickAlert()`.
- `turnsSincePlayerAttack` increments per turn via `TickAggroCooldown()`; drives forgiveness path (Hostile→Alert after 5 turns, Alert→Calm after 7) when rep above hostile.

## Rally Behavior
- `rallyRadius` (default 5) on faction.  
- When certain events fire (neutral aggressive retaliation, friendly second hit, kills), allies within radius mirror the state change (Alert or Hostile).

## Hostile-on-Sight Radius
- If rep is hostile to player (≤ -25), members will auto-aggro when the player is within `HostileAggroRadius` (=5 tiles), even without being attacked.

## Reputation Deltas
- `repOnHit` (default -5): applied on every player hit if rep > hostile threshold.
- `repOnKill` (default -100): applied on player kills if rep > hostile threshold.
- Rep changes are clamped by `ReputationSystem` (see implementation) and immediately influence future checks (e.g., hostile-on-sight).

## Disposition Summary
- **Calm**: More forgiving; neutral hits start with Alert (no immediate retaliation) unless rep already hostile. Will still attack on sight when rep hostile.  
- **Aggressive**: Neutral hits cause immediate retaliation and rally; otherwise rules are the same.

## Debugging Aids
- `FactionMember` logs on entering ALERT/HOSTILE/CALM, alert ticks, forgiveness cooldown ticks/pauses, and when noting player attacks.
- Enemy/NPC auto-aggro logs when hostile rep pulls the player within 5 tiles.

## Key Classes & Responsibilities
- `FactionDefinition`: data (disposition, rallyRadius, alertDurationTurns, repOnHit/repOnKill).
- `FactionMember`: state machine, cooldowns, ally queries, logs.
- `FactionCombatService`: central hit/kill rules and rally propagation.
- `HostilityService`: rep thresholds, disposition-based retaliation, hostile-on-sight radius.
- `NpcAI` / `EnemyAI`: per-turn ticking of alert/cooldown and auto-aggro when rep hostile.
