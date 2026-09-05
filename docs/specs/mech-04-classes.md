# M4 · Class Design

> **Status:** Roster committed (`DECISIONS-0008`) — do not implement before Phase 2 closes
> **Phase:** 3
> **Depends on:** [mech-02-combat-gcd](mech-02-combat-gcd.md), [mech-03-animation](mech-03-animation.md)

---

## 1. Intent

A class in 2004 is not a stat block — it is a *fantasy with a rhythm*. A warrior
starts every fight with nothing and builds rage as steel meets steel. A mage stands
still, spends a bar that only refills when the fighting stops, and dies if something
reaches them. A rogue never runs out of resource but never has enough of it at once.

The mechanics already model those three rhythms. What is missing is the identity on
top of them.

## 2. Current state

There are no classes. Every player gets the same five ability slots, loaded by name
in `PlayerCombat.Awake`:

```
0 Strike        auto-attack toggle,  no GCD
1 Ember Bolt    2.2 s cast, 18 mana
2 Hearthlight   2 s cast heal, 25 mana
3 Heroic Strike instant, 15 rage
4 Quick Slash   instant, 40 energy
```

Resource type is a runtime toggle with no persistent identity — the README calls it
a training control — which means the player currently holds abilities from three
different classes and can only use one third of the bar at a time. This is scaffold
behaviour from proving the three resource systems — it is not a design.

The pieces that *do* exist and should be reused: `SpellData` as the ability
definition, `ResourceType` with three genuinely different regeneration models, the
GCD, and the HFSM-driven cast/stun states.

## 3. Requirements

### Functional (`F`)

| ID | Requirement | Acceptance |
|---|---|---|
| `M4-F-001` | A `ClassData` ScriptableObject defines a class: display name, description, resource type, starting ability loadout, base health, health/level, and starting equipment | EditMode: `M4_F_001_EveryClassAssetIsComplete` validates all class assets |
| `M4-F-002` | Class is chosen at character creation and persists for the character | PlayMode: `M4_F_002_ClassPersistsAcrossRespawn` |
| `M4-F-003` | `PlayerCombat` loads its ability loadout from `ClassData`, not from a hardcoded name array | EditMode: `M4_F_003_LoadoutComesFromClassData` |
| `M4-F-004` | Resource type is owned by the class and cannot be switched at runtime; the training toggle is removed or restricted to debug builds | PlayMode: `M4_F_004_ResourceTypeIsImmutable` |
| `M4-F-005` | Each class has at least four usable abilities plus auto-attack, all reachable from the action bar | EditMode: `M4_F_005_ClassHasFullBar` |
| `M4-F-009` | Exactly three classes exist — Warrior, Mage, Rogue — each owning a different `ResourceType` | EditMode: `M4_F_009_ThreeClassesOneResourceEach` |
| `M4-F-010` | Every class can reach a self-heal; its cost works for that class's resource | EditMode: `M4_F_010_SelfHealIsReachableByAllClasses` |
| `M4-F-006` | Abilities unlock by level, so a level 1 character does not start with everything | PlayMode: `M4_F_006_AbilitiesUnlockByLevel` |
| `M4-F-007` | Each class can complete the Oathfire Trial solo | PlayMode: `M4_F_007_EveryClassCanSoloTheTrial` — one run per class |
| `M4-F-008` | The HUD resource bar colours and labels itself from the class's resource type | Manual M4-M-2 |

### Design (`A`)

| ID | Requirement | Acceptance |
|---|---|---|
| `M4-A-001` | Each class has a distinct silhouette from a distance — different armour weight and weapon | Manual M4-M-1 |
| `M4-A-002` | Each class has its own ability icons and cast/swing animation set | Manual M4-M-1 |
| `M4-A-003` | Two classes fighting the same guardian play visibly differently — different button rhythm, different positioning | Manual M4-M-3 |

### State integrity (`N`)

| ID | Requirement | Acceptance |
|---|---|---|
| ~~`M4-N-001`~~ | ~~Class is server-validated at spawn~~ | Withdrawn — single-player. See `DECISIONS-0006` |
| `M4-N-002` | A slot index outside the class's loadout does nothing, rather than falling through to another class's ability | PlayMode: `M4_N_002_CrossClassAbilityRejected` |

## 4. Out of scope

- Talents, specialisations, and talent trees → Phase 4 at the earliest
- Gear, stat scaling, and itemisation → Phase 4
- Class quests and trainers → Phase 4
- A fourth class or a dedicated healer → revisit only per `DECISIONS-0008`

## 5. Decisions

**Resolved — `DECISIONS-0008`: three classes, no healer.**

| # | Question | Outcome |
|---|---|---|
| `M4-D1` | The launch class roster | **Three** — Warrior (Rage), Mage (Mana), Rogue (Energy). One per resource system |
| `M4-D2` | A healer at launch? | **No.** Single-player: a dedicated healer has nobody to heal. Hearthlight becomes a self-heal every class can reach |
| `M4-D3` | Abilities per class at level 1 vs. the cap the trial reaches | Open — default staged unlock, 2 at L1 rising to 5. Not blocking |
| `M4-D4` | Do classes differ in base health and armour, or only in abilities? | Open — default full differentiation. A mage as tough as a warrior has no fantasy. Not blocking |

The roster was chosen "for now": it is the smallest set that proves the system, and
adding a fourth class later is additive because the loadout is data. Revisit if group
content or multiplayer ever returns.

### The committed roster

Each class owns one resource system and inherits its rhythm from mechanics that
already work. The seed ability for each already exists and is already balanced
against the guardians.

| Class | Resource | Rhythm | Seed ability (exists) |
|---|---|---|---|
| **Warrior** | Rage | Starts every fight empty, builds by dealing and taking damage, bleeds away out of combat. Punishes hesitation — you must keep swinging or lose the bar | Heroic Strike (instant, 15 rage, 35 power) |
| **Mage** | Mana | Big slow casts that break if you move. The bar refills only once the fighting stops, so every pull is a budget. Cannot afford to be hit | Ember Bolt (2.2 s cast, 18 mana, 40 power) |
| **Rogue** | Energy | Small constant ticks regardless of combat. Never empty, never rich — the constraint is *right now*, not the whole fight | Quick Slash (instant, 40 energy, 30 power) |

Hearthlight (2 s cast, 25 mana, 50 healing) is currently mana-costed, which does not
work for a Warrior or a Rogue. Making it a shared self-heal needs either a per-class
cost or a resource-free cooldown — resolve that when `M4-F-003` is implemented, and
note the outcome here.

## 6. Verification plan

> **M4-M-1 — identity at a glance.**
> Spawn one of each class side by side. From across the room you should be able to
> name each one from silhouette alone.

> **M4-M-2 — the bar.**
> For each class: the resource bar is the right colour and label, regenerates the
> right way, and the action bar shows only that class's abilities.

> **M4-M-3 — they play differently.**
> Fight the same guardian as each class. Write down the button sequence for each.
> If the sequences are interchangeable, the classes are not done.
