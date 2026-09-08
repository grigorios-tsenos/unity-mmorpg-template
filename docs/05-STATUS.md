# 05 · Status

**The live state of play. Every agent updates this at the end of every Task.**

> **Current milestone:** Phase 1 — the vertical slice
> **Active specs:** [room-01-oathfire-chamber](specs/room-01-oathfire-chamber.md) · [quest-01-oathfire-trial](specs/quest-01-oathfire-trial.md)
> **Last updated:** 2026-09-08

---

## In progress — the lock

Claim a row before you start. Clear it when you stop. If a row is claimed for files
you need, pick a different Task.

| Agent | Task (requirement IDs) | Files | Started |
|---|---|---|---|
| — | — | — | — |

---

## Blockers

None.

`V-001` (PlayMode tests unrunnable in batch mode) is **closed**. It was specific to
the old Netcode host-mode integration test. The single-player rewrite,
`RoomMovementCombatAndQuestWorkWithoutNetworking`, passes in batch mode.

---

## Verified state

Both suites pass on the current tree, run per [04-VERIFICATION.md](04-VERIFICATION.md):

| Suite | Result |
|---|---|
| EditMode | **3/3 pass**, no compile errors |
| PlayMode | **1/1 pass** — the full room, movement, combat and quest walkthrough |

Note: the Unity process exit code is unreliable — it has reported 0 on a failing run.
Always read the counts out of the results XML.

---

## Phase 1 requirement tracker

Tick only what is implemented **and** verified. A manual criterion is ticked by the
user, never by an agent.

### R1 · Oathfire Chamber

- [ ] `R1-F-001` player cannot leave the room
- [ ] `R1-F-002` colliders match prop silhouettes
- [ ] `R1-F-003` nothing traps the player *(manual)*
- [ ] `R1-F-004` brazier is art, not a primitive — *still open: bowl and flame are a cylinder and a sphere (`D-09`)*
- [x] `R1-F-005` brazier cold/lit states — *`OathfireVisual` drives it from `QuestManager.Changed`*
- [x] `R1-F-006` authored spawn point — *the player is placed by `SceneBuilder`; the old hardcoded respawn literal is gone*
- [ ] `R1-F-008` authored guardian spawn circle — *still a literal (`D-02`)*
- [ ] `R1-F-009` merchant purchase is validated
- [ ] `R1-F-010` front of the hall is a sealed door, not a bare barrier — *newly unblocked (`DECISIONS-0009`)*
- [ ] `R1-F-011` the sealed door responds to interact
- [ ] `R1-A-008` the door reads as "not yet", not as a bug *(manual)*
- [x] `R1-A-002` lit brazier has animated warm light and a flame
- [x] `R1-A-003` torches read as pools of warmth — *`TorchFlicker`*
- [ ] `R1-A-001`, `R1-A-004`…`R1-A-007` remaining presentation *(manual)*
- [ ] `R1-P-001`, `R1-P-002` performance *(manual)*

`R1-F-007` (simultaneous spawns) and `R1-N-001`/`R1-N-002` are **withdrawn** — they
described multiplayer behaviour that no longer exists. See `DECISIONS-0006`.

### Q1 · The Oathfire Trial

- [x] `Q1-F-001` solo loop 0 → 5 completes — *verified by the PlayMode test*
- [x] `Q1-F-003` repeat resets progress — *stage 5 → accept path exercised*
- [ ] `Q1-F-004` pre-accept kills grant nothing
- [ ] `Q1-F-005` objective text correct at every stage
- [ ] `Q1-F-006` journal shows briefing, objectives, rewards *(manual)*
- [ ] `Q1-F-007` abandon works and cleans up — **not implemented**
- [ ] `Q1-F-008` quest items cleared on turn-in and abandon — *turn-in verified; abandon missing*
- [x] `Q1-N-001` an un-offered dialogue ID grants nothing — *asserted; extend to all IDs at all stages*
- [x] `Q1-N-003` rewards granted exactly once — *double claim asserted; extend to a 20× loop*
- [ ] `Q1-N-004` cannot rest at the hearth in combat
- [ ] `Q1-A-001`…`Q1-A-005` presentation *(manual)*
- [ ] `Q1-X-001`, `Q1-X-002` legibility *(manual)*

`Q1-F-002` (two players both claim) and `Q1-F-009` (all participants leave) are
**withdrawn** — single-player. See `DECISIONS-0006`.

---

## Deferred

Found but deliberately not fixed. Each needs a requirement ID before anyone touches it.

| From | Item | Where it belongs |
|---|---|---|
| `D-02` | Guardian spawn circle is a hardcoded literal | `R1-F-008`, Phase 1 |
| `D-04` | Enemies steer straight at their target and walk into furniture | *mech-05-ai-navigation*, Phase 2 |
| `D-05` | Quest stages and NPC dialogue are hardcoded rather than data | Phase 4 — **do not generalise early** |
| `D-08` | Corpses despawn instantly; loot auto-grants | Phase 4 |
| `D-09` | Brazier bowl and flame are primitives | `R1-F-004`, Phase 1 |
| `D-10` | Fixed 5-slot loadout, no class identity | [mech-04-classes](specs/mech-04-classes.md), Phase 3 |
| `D-11` | No persistence — progress lasts one session | Phase 4 |
| `D-12` | Legacy `Animation` component, clip lookup by name, no blending | [mech-03-animation](specs/mech-03-animation.md), Phase 2 |
| — | Camera snaps rather than eases when occlusion clears | `M1-A-002`, Phase 2 |
| — | No damage variance, crits, or misses | `M2-F-004`…`M2-F-006`, Phase 2 |
| — | `PlayerCombat.Stun` exists but nothing calls it | `M4-F-011`, Phase 3 class kits |

---

## Decisions awaiting the user

None. The grilling decisions are recorded in `DECISIONS-0010`; no implemented
requirements were ticked by that documentation task.

| # | Question | Answer |
|---|---|---|
| `M4-D1` / `M4-D2` | Class roster | **Three — Warrior (Rage), Mage (Mana), Rogue (Energy). No healer.** `DECISIONS-0008` |
| `R1-D2` | The chamber's open front | **A sealed door to Room 2, authored now, opened in Phase 4.** `DECISIONS-0009` |
| Grilling 1–21 | Fidelity, slice sign-off, room, quest, mechanics, animation, and classes | **Resolved.** See `DECISIONS-0010` and the amended specs. |

---

## Handoff — 2026-09-05, Claude

**Done:** Merged `origin/main` into this branch, resolved the `README.md` conflict
onto main's single-player description, and corrected the spec set for the new
architecture — main removed networking entirely, which invalidated the authority
model in `AGENTS.md`, most of `01-ARCHITECTURE.md`, and every `-N-` requirement.

**Verified:** EditMode 3/3, PlayMode 1/1 on the merged tree. `V-001` closed.

**Next:** three concrete Phase 1 gaps in the room — `R1-F-010`/`R1-F-011`/`R1-A-008`
(the newly unblocked sealed door), `R1-F-004` (brazier art), and `R1-F-008` (authored
spawn circle). `Q1-F-007` (abandon) is the largest quest gap. Phase 3 is fully
specified now but gated behind Phase 2.

**Watch out:** `ClassicContentBuilder.Asset<T>` never overwrites an existing asset, so
changing a balance number there does nothing to the committed
`Assets/Resources/RPG/*.asset`. The quest briefing is patched in place as an explicit
exception — follow that pattern when authored data needs migrating.

### Handoff — 2026-09-08, Codex

**Done:** Recorded the owner's grilling decisions 1–21 in `DECISIONS-0010` and the
affected Phase 1–3 specs. No gameplay work was performed and no requirement was
ticked.

**Next:** Finish the current Phase 1 requirements in order. Before Phase 2 combat,
author and cite the 1.12-inspired combat-table inputs as data (`M2-F-017`). Before
Phase 3 implementation, author the exact level 1–5 XP thresholds and class unlock
map as data; they were deliberately not guessed in the decision record.

**Verified:** Documentation links, requirement IDs, and diff only; Unity tests were
not run because this task changed no runtime code, scenes, prefabs, or content assets.
