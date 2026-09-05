# AGENTS.md — Project Azeroth04

**Read this file first. Every time. Before any other action.**

This is the root contract for every agent working on this repository — Claude Code,
Codex, or a human. It is short on purpose. It tells you what the project is, the
rules you may never break, and where the real specifications live.

---

## 0. What this project is

A 3D action RPG in Unity 6 (URP) that replicates the mechanics, UI paradigms, and
artistic feel of 2004 *World of Warcraft* (Vanilla / patch 1.12). It is currently
**single-player**: one scene, one room, no networking. The former Netcode layer is
preserved for reference in `Archive/Networking/`, outside `Assets/`, and is not part
of the game.

We build it **room by room and mechanic by mechanic**. One space is finished to a
shippable standard before the next is started. One mechanic is perfected before the
next is touched. Breadth-first work is forbidden.

---

## 1. The spec-driven loop (non-negotiable)

```
   read spec  →  plan  →  implement  →  verify  →  update status  →  commit
        ↑                                                              │
        └────────────── spec amended if reality disagrees ─────────────┘
```

**No code is written that is not traceable to a numbered requirement in a spec.**

If you want to build something that has no requirement, you write the requirement
first (in the relevant spec file, with an ID), get it agreed, and *then* implement.
If reality contradicts a spec, you change the spec in the same commit as the code
and note it in `docs/DECISIONS.md`.

The full process — including how Claude and Codex hand work to each other — is in
**[docs/00-WORKFLOW.md](docs/00-WORKFLOW.md)**. Read it before your first change.

---

## 2. Where to look

| Question | File |
|---|---|
| How do I work here? What counts as done? | [docs/00-WORKFLOW.md](docs/00-WORKFLOW.md) |
| How is the code laid out? What are the invariants? | [docs/01-ARCHITECTURE.md](docs/01-ARCHITECTURE.md) |
| What are we building next, in what order? | [docs/02-ROADMAP.md](docs/02-ROADMAP.md) |
| Is this art/UI allowed? | [docs/03-ART-BIBLE.md](docs/03-ART-BIBLE.md) |
| How do I prove my change works? | [docs/04-VERIFICATION.md](docs/04-VERIFICATION.md) |
| What is the state of play right now? | [docs/05-STATUS.md](docs/05-STATUS.md) |
| Why was X decided that way? | [docs/DECISIONS.md](docs/DECISIONS.md) |
| The actual requirements | [docs/specs/](docs/specs/) |

**Active spec** (the only one you may implement against without being asked):
see the "Current milestone" line at the top of [docs/05-STATUS.md](docs/05-STATUS.md).

---

## 3. Hard rules

These are violations, not preferences. A change that breaks one of these is rejected
regardless of how well it works.

### 3.1 Scenes and prefabs are generated, never hand-edited
`Assets/Scenes/World.unity` and `Assets/Prefabs/*.prefab` are **build outputs** of
`Assets/Editor/SceneBuilder.cs` (menu: *Azeroth04 ▸ Build Oathfire Room*). `Assets/Resources/RPG/*.asset` are build outputs of
`Assets/Editor/ClassicContentBuilder.cs`.

- To change the world, edit `SceneBuilder.cs` and re-run the builder.
- To change spell/enemy/quest/item numbers, edit `ClassicContentBuilder.cs`.
- **Never** edit scene or prefab YAML by hand, and never ask the user to wire an
  Inspector reference that a builder could set.
- Re-running the builders must be idempotent and must not lose authored data.

### 3.2 Gameplay owns state; presentation only reads it
There is no server and no client. Gameplay systems own their state and expose it as
`ObservableValue<T>`, which notifies only on change.

- Damage, healing, resource spend, loot, quest state, and item grants are decided by
  the gameplay system that owns them — never by a UI script or an animation callback.
- Validation still belongs at the entry point of every action: range, line of sight,
  cooldown, GCD, resource availability, and whether the action was actually offered.
  `PlayerCombat.ChooseAction` consuming a whitelist of offered dialogue IDs is the
  pattern to follow — it is what stops a stray call granting a reward.
- **Do not reintroduce `Unity.Netcode` types into `Assets/`.** Multiplayer is out of
  scope until Phase 4 at the earliest; if it returns, it is a deliberate decision with
  a `DECISIONS.md` entry, not a side effect of someone's change.

### 3.3 Gameplay never references UI
`Core.Runtime`, `Combat.Runtime`, and `Quest.Runtime` must not reference
`UI.Runtime`. Gameplay raises events through `GameEvents`; the HUD subscribes.
If you need the HUD to know something, add an event — not a reference.

### 3.4 Data lives in ScriptableObjects
Spells, abilities, enemies, quests, items, and loot tables are `ScriptableObject`
definitions. Balance numbers, ranges, costs, and cooldowns never appear as literals
in a `MonoBehaviour`. Tuning must be possible without recompiling.

### 3.5 Everything the player can see obeys the Art Bible
2004 aesthetic, strictly. No PBR, no normal maps, no metallic/smoothness, no
faceted untextured low-poly. See [docs/03-ART-BIBLE.md](docs/03-ART-BIBLE.md)
before importing, generating, or shading anything.

### 3.6 Every requirement ships with a test
An acceptance criterion that cannot be checked automatically must say so explicitly
and name the manual step. Everything else gets an EditMode or PlayMode test.
See [docs/04-VERIFICATION.md](docs/04-VERIFICATION.md).

### 3.7 Scope discipline
Do not "improve while you're in there." If you spot something out of scope, add it
to the Deferred list in `docs/05-STATUS.md` and leave the code alone. Drive-by
refactors are the main way a room-by-room plan turns into a broken game.

---

## 4. Engine and language

- **Unity 6000.6.0f1** (URP). The installed editor is at
  `/Applications/Unity/Hub/Editor/6000.6.0f1`.
- **C# 9+**, .NET Standard 2.1 profile.
- **No networking dependency.** `Assets/` must compile with no reference to
  `Unity.Netcode`.
- Assemblies: `Core.Runtime`, `Combat.Runtime`, `Quest.Runtime`, `UI.Runtime`.
  New systems go in an existing assembly or a new one — never in the default
  `Assembly-CSharp`.

### Performance rules
- Cache component references in `Awake()`. Never call `GetComponent<T>()` in
  `Update()` or `FixedUpdate()`.
- Periodic work (regen, AI think, aggro scan, prompt polling) uses coroutines with
  a cached `WaitForSeconds`, not per-frame `Update()`.
- No allocation in per-frame paths: no LINQ, no `new` in `Update`, no string
  concatenation for UI that hasn't changed.

---

## 5. Style

Match the file you are editing. This codebase is deliberately dense — short methods,
expression bodies, minimal ceremony. Do not reformat existing files to your taste.

- Comments explain **why**, never **what**. A comment restating the code gets deleted.
- Public API on gameplay types gets an XML doc comment only when the contract is
  non-obvious (ownership, authority, ordering).
- `PascalCase` for types/methods/properties, `camelCase` for fields and locals,
  no Hungarian prefixes in gameplay code (`RpgHud` uses `_leading` underscores —
  match that file's local convention).

---

## 6. Definition of Done

A task is done when **all** of these are true:

1. Every requirement ID in the task's scope is implemented.
2. *Azeroth04 ▸ Build Oathfire Room* regenerates cleanly and `World.unity` is
   playable straight from Play.
3. EditMode and PlayMode tests pass in batch mode (`docs/04-VERIFICATION.md`).
4. No new console errors or warnings during a full play session of the room.
5. `docs/05-STATUS.md` is updated: requirements ticked, new gaps recorded.
6. The spec file reflects what was actually built.

Report honestly. "Tests pass" means you ran them and read the output. If something
is unfinished or unverified, say which part and why — do not round up.
