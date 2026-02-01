# Territorial Control, Infiltration, and Collapse (Palimpsest-Driven)

This document captures the provided design for district-based faction control, rival infiltration, and collapse, integrating directly with the Palimpsest layer model (claims/overlays, provenance, jurisdiction).

---

## 1) District Partition & Tick
- City = graph of ~60 districts, updated once per in-game day.
- Daily tick order:
  1. Recompute derived values (contest/closeness, instability).
  2. Update per-faction district variables (patrol, institutions, registry, corruption, legitimacy, control).
  3. Update palimpsest-driven variables (contradiction density X, heat H).
  4. Generate cases (Poisson) and enqueue incidents.
  5. Evaluate district loss and faction fracture triggers.

---

## 2) Variables (summary)
- **Static per district:** population, visibility, economicValue, terrainFriction, strategicNode, Q (baseline disorder).
- **Dynamic per district:** H (heat), X (contradiction density), casesQueued.
- **Per faction per district:** C (control), L (legitimacy), I (institutions), P (patrol), R (registry sync), K (corruption), InvestPatrol, InvestInstitutions, AuditPresence, SabotagePressure.
- **Per faction global:** AdminCap, AdminLoad, Overcap, PeakAreaEff, AreaEff.

---

## 3) Key Derived Quantities
- **Opponent pressure:** Opp[f,d] = max control of any rival in d.
- **Closeness:** closeness[d] = 1 - |top1C - top2C|.
- **Instability:** U[d] = 0.40*Q + 0.30*H + 0.30*X.
- **Overcap multiplier:** O[f] = 1 / (1 + OMult*Overcap[f]).
- **Enforcement capacity:** E[f,d] = 0.35*P + 0.35*I + 0.20*R + 0.10*(1-K).

---

## 4) Daily Update Equations (per faction/per district)
- **Patrol:** P += +pInvest*InvestPatrol - pFriction*terrainFriction - pOvercap*Overcap - pInstability*U.
- **Institutions:** I += +iInvest*InvestInstitutions - iSabotage*SabotagePressure.
- **Registry:** R += +rInst*I*O - rOvercap*Overcap - rContradiction*X.
- **Corruption:** K += +kUp*pressure*(1-K) - kDown*cleaning*K, where pressure uses Overcap, Opp, U; cleaning uses audits, institutions, O.
- **Legitimacy:** L += (growth - decay)*(0.75 + 0.25*O); growth from service (I,P,R), decay from harm (U,K,X, no patrol).
- **Control:** C += growthC + decayC; growthC uses E*L*O*(1-C), decayC uses (U+Opp)*(1-E)*C.
- Clamp all to [0,1].

---

## 5) Palimpsest Integration
- **Contradiction density X:** computed from conflicting predicate assertions (Jurisdiction, LawZone, Owner, etc.) using secondary confidence (c2+c3).
- **Heat H:** H += hEdits*newEdits + hX*X - hClean*cleaning - hBaseDecay.
- Contradictions/heat feed instability U and case rates, and slow recovery.

---

## 6) Case Generation (per district)
- Incident rate λ(d) = baseCaseRate + 0.6*economicValue + 0.8*U + 0.6*closeness + 0.5*H + 0.4*(1 - patrolMax).
- casesToday ~ Poisson(λ); case type weights depend on economicValue, patrolMax, legitimacyMax, corruptionMax, X, H, Q.
- Cases can add offenses, palimpsest edits (graffiti), and sabotage pressure.

---

## 7) Rival Infiltration AI
- Budget + risk tolerance drive daily action choice.
- Target score S = opportunity (weakness/value) – risk (scaled by risk tolerance).
- Actions: Rumor (hit L, raise X), Doc seeding (raise X, hit R, possible predicate win), Institutional capture (raise I, bonus to C), Patrol sabotage (lower P, raise Q), Heat spike (raise H/X).
- Campaign phases: Probe → Undermine → Capture → Flip; escalate on control/legitimacy/overcap/contradiction thresholds; withdraw if patrol/audits spike.

---

## 8) Loss & Collapse
- **District loss:** if C low for T days and (Opp high or L low) → lose district; claims weaken, patrol reallocates.
- **Fracture:** if AreaEff < FractureRatio * PeakAreaEff for FractureDays → faction fractures or suffers legitimacy shock.

---

## 9) Pseudocode (daily tick)
- Compute closeness, X, H, U.
- Compute Overcap & O per faction.
- Update P, I, R, K, L, C per faction/district (with capture bonus optional).
- Generate cases (Poisson).
- Update AreaEff, PeakAreaEff; check loss streaks & fractures.

---

## 10) JSON Schema
- `DistrictState` schema defines static, dynamic, and per-faction state, including neighbors for the graph.

---

## 11) Recommended Defaults (Vertical Slice)
- 60 districts, 5 factions. Suggested distributions for static descriptors and starting control patterns for dominant, rivals, underworld, civic institution.
- Constants provided for all rates (aC, bC, pInvest, iInvest, heat, cases, loss/fracture thresholds, infiltration magnitudes).

---

## 12) Implementation Notes
- Stable dynamics (growth shrinks near 1, decay shrinks near 0).
- Main levers: reduce P/R on frontier, raise X via competing claims, raise H, push Overcap via forced annexation.
- Jurisdiction-aware: predicate confidence should inform X and effective control recognition.

---

## 13) Acceptance Criteria (60-district slice)
1. Forced annexation of frontier districts lowers dominant Overcap within ~3–7 days.
2. Rival infiltration can flip districts in ~7–20 days without combat.
3. Contradiction/heat raise case rate and slow recovery.
4. Player can topple a faction via overextension + infiltration (manipulating P, R, X, H, Overcap) in a 5–7 mission chain.
