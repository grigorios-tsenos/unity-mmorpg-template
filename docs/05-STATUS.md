# 05 · Status

**The live state of play. Every agent updates this at the end of every Task.**

> **Current milestone:** Phase 1 — the vertical slice
> **Active specs:** [room-01-oathfire-chamber](specs/room-01-oathfire-chamber.md) · [quest-01-oathfire-trial](specs/quest-01-oathfire-trial.md)
> **Last updated:** 2026-09-05

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
| — | `PlayerCombat.Stun` exists but nothing calls it | `M2-F-008`, Phase 2 |

---

## Decisions awaiting the user

| # | Question | Why it matters now |
|---|---|---|
| `M4-D1` | The class roster — three (Warrior/Mage/Rogue) or four with a healer? | Blocking for Phase 3; without multiplayer, a dedicated healer is harder to justify, which strengthens the case for three |
| `R1-D2` | Is the open front of the chamber a permanent wall or a sealed door to Room 2? | Cheap to author now, expensive to retrofit |

`M3-D1` (animation source) is **answered by events**: bundled clips driving a legacy
`Animation` rig are already in place. The open question is now narrower — whether to
migrate to an `Animator` controller for real blending (`D-12`).

---

## Handoff — 2026-09-05, Claude

**Done:** Merged `origin/main` into this branch, resolved the `README.md` conflict
onto main's single-player description, and corrected the spec set for the new
architecture — main removed networking entirely, which invalidated the authority
model in `AGENTS.md`, most of `01-ARCHITECTURE.md`, and every `-N-` requirement.

**Verified:** EditMode 3/3, PlayMode 1/1 on the merged tree. `V-001` closed.

**Next:** `R1-F-004` (brazier art) and `R1-F-008` (authored spawn circle) are the two
concrete Phase 1 gaps left in the room. `Q1-F-007` (abandon) is the largest quest gap.

**Watch out:** `ClassicContentBuilder.Asset<T>` never overwrites an existing asset, so
changing a balance number there does nothing to the committed
`Assets/Resources/RPG/*.asset`. The quest briefing is patched in place as an explicit
exception — follow that pattern when authored data needs migrating.
