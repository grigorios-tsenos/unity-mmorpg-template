# M4 · Class Design

> **Status:** Draft — do not implement before Phase 2 closes
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
- More than the launch roster below

## 5. Open decisions

> **M4-D1 is blocking and is the reason this spec is a draft.** The roster decides
> the animation sets, the ability art, the balance work, and how much of Phase 3
> there is. It should be answered before Phase 2 closes so asset work can start.

| # | Question | Options | Blocking? |
|---|---|---|---|
| M4-D1 | The launch class roster | (a) **three** — Warrior (rage), Mage (mana), Rogue (energy): one per resource system, all three already have a working ability as a seed, smallest path to "classes feel different"; (b) four, adding a Priest/healer (mana); (c) more | **Yes** |
| M4-D2 | Is there a healer at launch? | yes / no | Depends on M4-D1. The game is single-player, so a dedicated healer has no group to heal — this argues for folding healing into a hybrid rather than a class |
| M4-D3 | How many abilities per class at level 1, and at the level cap the trial reaches? | 2 at L1 → 5 by L5 / all at L1 | No — default staged unlock; it gives levelling a purpose |
| M4-D4 | Do classes differ in base health and armour, or only in abilities? | abilities only / full stat differentiation | No — default full differentiation; a mage that is as tough as a warrior has no fantasy |

**Recommendation on M4-D1: (a) three.** The three resource systems are already
implemented and genuinely distinct, one seed ability exists for each, and three
classes is the smallest roster that proves the system. Since the game is
single-player, a dedicated healer has nobody to heal — Hearthlight is better as a
self-heal every class can reach, or as a hybrid's tool.

### Sketch — not committed, for discussion only

| Class | Resource | Rhythm | Seed ability that exists |
|---|---|---|---|
| Warrior | Rage | Starts empty, builds by fighting, must spend or lose it | Heroic Strike |
| Mage | Mana | Big slow casts, stands still, cannot afford to be hit | Ember Bolt |
| Rogue | Energy | Constant small ticks, never idle, never rich | Quick Slash |

Hearthlight (the existing heal) becomes either a Priest ability under option (b), or
a shared self-heal under option (a) — which suits single-player better.

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
