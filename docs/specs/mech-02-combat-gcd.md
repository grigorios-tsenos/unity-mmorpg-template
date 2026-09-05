# M2 · Combat, GCD & Resources

> **Status:** Draft
> **Phase:** 2, order 3
> **Depends on:** [mech-03-animation](mech-03-animation.md), [mech-01-movement-camera](mech-01-movement-camera.md)

---

## 1. Intent

Tab-target combat with a 1.5-second global cooldown, three resource systems that
behave differently on purpose, casts that break when you move, and mobs that leash
when you run. The rhythm is deliberate and slightly slow: you press a button, the
GCD sweeps, the swing lands, a number floats up.

## 2. Current state

`PlayerCombat` (`Assets/Scripts/Combat/PlayerCombat.cs`), `Enemy`, `PlayerStats`,
`SpellData`, and the HUD's action bar, cast bar, and target frame.

Implemented and working:

- **Targeting:** `Tab` cycles living enemies within 25 m by `NetworkObjectId`;
  `Escape` clears the target, stops auto-attack, and cancels the cast. The server
  re-validates the target on every `SetTargetRpc`.
- **GCD:** `GlobalReady` is a replicated server timestamp; every non-auto-attack
  ability costs 1.5 s. Auto-attack toggle has `globalCooldown = 0`.
- **Cast bar:** `CastingSlot` + `CastEnd` replicate, so remote clients can render
  another player's cast.
- **Cast interruption:** moving during a cast cancels it and **does not charge the
  resource** — the spend happens in `FinishCast`, not on cast start. Verified by test.
- **Validation on every cast:** slot range, death, pending cast, stun, GCD, per-slot
  cooldown, target alive and in range, line of sight (`Physics.Linecast`, world
  geometry only), not moving for timed casts, correct resource type and amount.
- **Resources:** Mana regenerates 12 per 2 s while resting and not casting; Energy
  ticks 20 per 2 s always; Rage decays 5 per 2 s while resting, gains 10 per swing
  and 8 per hit taken. Switching resource type is blocked in combat, while casting,
  and while dead.
- **Health regen:** 4 per 2 s after 5 s out of combat.
- **Death:** 5 s corpse timer, then teleport to the hearth at full health.
- **Potions:** shared 60 s cooldown, server-enforced, no effect at full health.
- **Enemy AI:** aggro scan with line-of-sight, 0.5 s aggro delay before the first
  attack, chase, in-range attack on an interval, leash on distance from home or
  target, full heal on returning home.
- **Abilities:** Strike (auto), Ember Bolt (2.2 s cast, 18 mana, 40), Hearthlight
  (2 s cast heal, 25 mana, 50), Heroic Strike (instant, 15 rage, 35), Quick Slash
  (instant, 40 energy, 30).

Gaps: no threat/aggro table (the last attacker simply becomes the target); no crits;
no misses, dodges, parries, or resists; no damage variance at all; no spell school or
resistance; no interrupts, stuns, or crowd control usable *by* the player (`Stun`
exists on `PlayerCombat` but nothing calls it); no auto-attack swing timer visible in
the UI; no facing requirement; no combat log.

## 3. Requirements

### Functional (`F`)

| ID | Requirement | Acceptance |
|---|---|---|
| `M2-F-001` | The GCD is exactly 1.5 s for every ability that has one, visibly sweeps on all action-bar slots at once, and cannot be bypassed by spamming | PlayMode: `M2_F_001_GcdBlocksAllSlots` |
| `M2-F-002` | Per-ability cooldowns are independent of the GCD and are shown as their own sweep | PlayMode: `M2_F_002_AbilityCooldownIndependentOfGcd` |
| `M2-F-003` | Auto-attack swings on a fixed timer, continues while moving, and stops when the target dies or leashes | PlayMode: `M2_F_003_AutoAttackTimerAndStopConditions` |
| `M2-F-004` | Damage has variance (a low–high range per ability) rather than a single fixed number | EditMode: `M2_F_004_AbilitiesHaveDamageRange` — **needs `SpellData` fields `powerMin`/`powerMax`** |
| `M2-F-005` | Attacks can critically strike for double damage, shown distinctly in floating combat text | PlayMode + Manual M2-M-2 |
| `M2-F-006` | Attacks can miss, and a miss is communicated ("Miss", not a silent nothing) | PlayMode: `M2_F_006_MissIsReportedNotSilent` |
| `M2-F-007` | An enemy maintains a threat table; the highest-threat player is the target, not merely the most recent attacker | PlayMode: `M2_F_007_ThreatTableSelectsTarget` |
| `M2-F-008` | The player has at least one interrupt or crowd-control ability, and `PlayerCombat.Stun` is actually reachable from gameplay | PlayMode: `M2_F_008_PlayerCanInterrupt` |
| `M2-F-009` | Damage requires facing the target within a tolerance cone | PlayMode: `M2_F_009_CannotAttackBehindYou` |
| `M2-F-010` | Line of sight is required and is broken by pillars and walls, not by other characters | Already implemented; add `M2_F_010_LosBlockedByPillarNotByPlayer` |
| `M2-F-011` | Leashing enemies are untargetable, deal no damage, and fully heal at home | Already implemented; assert it |
| `M2-F-012` | A combat log records every swing, cast, hit, miss, and death with amounts | Manual M2-M-3 — **new** |

### Resources (`F`, continued)

| ID | Requirement | Acceptance |
|---|---|---|
| `M2-F-013` | Mana regenerates only out of combat and never while casting | Already implemented; assert `M2_F_013_ManaDoesNotRegenWhileCasting` |
| `M2-F-014` | Energy ticks on a fixed 2 s cadence regardless of combat state | Covered by an existing PlayMode assertion |
| `M2-F-015` | Rage is generated by dealing and taking damage and decays out of combat | PlayMode: `M2_F_015_RageGenerationAndDecay` |
| `M2-F-016` | An ability cannot be used with insufficient resource, and the failure is communicated | Already implemented (`FeedbackRpc`); assert it |

### Presentation (`A`)

| ID | Requirement | Acceptance |
|---|---|---|
| `M2-A-001` | Floating combat text distinguishes damage dealt, damage taken, crits, heals, and misses by colour and size | Manual M2-M-2 |
| `M2-A-002` | The cast bar fills accurately over `castTime` and visibly breaks (not just disappears) on interruption | Manual M2-M-1 |
| `M2-A-003` | The target frame shows name, level, health, and hostile/neutral colouring | Manual M2-M-1 |
| `M2-A-004` | The action bar shows keybind, icon, cooldown sweep, and an out-of-range / out-of-resource state | Manual M2-M-1 |
| `M2-A-005` | The swing timer is visible so auto-attack rhythm is readable | Manual M2-M-1 |

### Netcode (`N`)

| ID | Requirement | Acceptance |
|---|---|---|
| `M2-N-001` | Every combat outcome is computed server-side; a client cannot deal damage, grant itself resource, or bypass a cooldown | PlayMode: `M2_N_001_ForgedCastRpcRejected` fires every slot with every invalid precondition |
| `M2-N-002` | Cast completion re-validates target, range, LOS and resource — a target that walked away mid-cast takes no damage and the resource is not spent | Already implemented in `FinishCast`; assert it |
| `M2-N-003` | Damage numbers seen by clients always match server-applied damage | PlayMode: `M2_N_003_ClientDamageMatchesServer` |
| `M2-N-004` | Two players attacking one enemy both get credit and neither can double-loot | PlayMode: `M2_N_004_SharedKillCredit` |

## 4. Out of scope

- Class-specific kits and resource ownership → [mech-04-classes](mech-04-classes.md)
- Enemy pathing and pull mechanics → *mech-05-ai-navigation*
- Combat audio → *mech-06-audio-feedback*
- Gear, stats, and itemisation → Phase 4

## 5. Open decisions

| # | Question | Options | Blocking? |
|---|---|---|---|
| M2-D1 | How faithful is the hit table? | (a) simple: hit/crit/miss; (b) full 1.12: miss, dodge, parry, block, glancing, crush | No — default (a) now, (b) is a Phase 4 refinement |
| M2-D2 | Does the player character have a level-vs-enemy-level modifier on hit chance? | yes / no | No — default no until levelling has content behind it |
| M2-D3 | Threat model | (a) simple damage-summed table; (b) 1.12 threat with modifiers and a 110%/130% pull threshold | No — default (a); (b) only matters with real tanking roles |
| M2-D4 | Where does the combat log live? | HUD chat tab / separate window / both | No — default a chat tab, matching 2004 |

## 6. Verification plan

> **M2-M-1 — the rhythm.**
> Pull a guardian. Toggle auto-attack and just watch for 20 s: swings should land on
> a readable beat with the swing timer. Then weave abilities in. The GCD sweep must
> start the instant you press, cover every slot, and finish before you can press
> again. Start a cast and walk — the bar must visibly break, and your mana must not
> be spent.

> **M2-M-2 — the numbers.**
> Fight until you see a crit, a miss, a heal, and damage taken. All four must be
> instantly distinguishable without reading them.

> **M2-M-3 — the log.**
> Fight one guardian to the death, then read the combat log. You should be able to
> reconstruct the fight from it, including what killed what.
