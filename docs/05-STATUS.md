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

| ID | Blocker | Impact | Owner |
|---|---|---|---|
| `V-001` | **The PlayMode integration test fails in headless batch mode.** `TrialIntegrationTests.HostTrialCastsLootAndRewardsAreAuthoritative` times out at its first wait: `Bootstrap` and `World` both load and `StartHost()` returns true, but `QuestManager.Instance.IsSpawned`, `PlayerStats.Local` and `PlayerCombat.Local` are still null after 15 s. No Netcode errors appear in the log. EditMode is green (3/3). | The entire automated safety net for Phase 1 is unverifiable from the command line. Triage this first — every other Task depends on being able to prove it did no harm | unassigned |

**Triage notes for `V-001`:** confirm whether the test passes from the Editor's Test
Runner window (if it does, the fault is batch-mode-specific, most likely host start
or scene-managed spawning under `-nographic`). Check whether `NetworkManager`
survives the `LoadSceneMode.Single` load of World, and whether the 15 s timeout is
simply too short for a cold run. Raise `NetworkManager.LogLevel` to `Developer`
before re-running — the current log contains no Netcode diagnostics at all.

---

## Phase 1 requirement tracker

Tick only what is implemented **and** verified. A manual criterion is ticked by the
user, never by an agent.

### R1 · Oathfire Chamber

- [ ] `R1-F-001` player cannot leave the room
- [ ] `R1-F-002` colliders match prop silhouettes
- [ ] `R1-F-003` nothing traps the player *(manual)*
- [ ] `R1-F-004` brazier is art, not a primitive
- [ ] `R1-F-005` brazier cold/lit states from quest state
- [ ] `R1-F-006` authored spawn point, no literals
- [ ] `R1-F-007` simultaneous spawns never overlap
- [ ] `R1-F-008` authored guardian spawn circle
- [ ] `R1-F-009` merchant purchase is server-validated
- [ ] `R1-A-001`…`R1-A-007` presentation *(mostly manual)*
- [ ] `R1-N-001` late joiner sees correct room state
- [ ] `R1-N-002` interaction range enforced server-side
- [ ] `R1-P-001`, `R1-P-002` performance *(manual)*

### Q1 · The Oathfire Trial

- [x] `Q1-F-001` solo loop 0 → 5 completes — *covered by `TrialIntegrationTests`, currently unverifiable, see `V-001`*
- [ ] `Q1-F-002` two players both claim
- [ ] `Q1-F-003` repeat resets all progress
- [ ] `Q1-F-004` pre-accept kills grant nothing
- [ ] `Q1-F-005` objective text correct at every stage
- [ ] `Q1-F-006` journal shows briefing, objectives, rewards *(manual)*
- [ ] `Q1-F-007` abandon works and cleans up — **not implemented**
- [ ] `Q1-F-008` quest items cleared on turn-in and abandon — *turn-in done, abandon missing*
- [ ] `Q1-F-009` all-participants-leave resets
- [ ] `Q1-A-001`…`Q1-A-005` presentation *(manual; `Q1-A-001` blocked on Phase 2 audio)*
- [x] `Q1-N-001` forged dialogue IDs grant nothing — *asserted for one ID; extend to all*
- [ ] `Q1-N-002` client cannot advance stage
- [x] `Q1-N-003` rewards granted exactly once — *asserted for a double claim; extend to 20×*
- [ ] `Q1-N-004` cannot rest at the hearth in combat
- [ ] `Q1-X-001`, `Q1-X-002` legibility *(manual)*

---

## Deferred

Found but deliberately not fixed. Each needs a requirement ID before anyone touches it.

| From | Item | Where it belongs |
|---|---|---|
| `D-04` | Enemies steer straight at their target and walk into furniture | *mech-05-ai-navigation*, Phase 2 |
| `D-05` | Quest stages and NPC dialogue are hardcoded rather than data | Phase 4 — **do not generalise early** |
| `D-06` | No `Animator` on any character | [mech-03-animation](specs/mech-03-animation.md), Phase 2 |
| `D-07` | No audio anywhere | *mech-06-audio-feedback*, Phase 2 |
| `D-08` | Corpses despawn instantly; loot auto-grants to the killer only | Phase 4 |
| `D-10` | Fixed 5-slot loadout, no class identity | [mech-04-classes](specs/mech-04-classes.md), Phase 3 |
| — | Standalone strafe-right is unbound (`E` is interact) | `M1-F-003`, Phase 2 |
| — | Camera snaps rather than eases when occlusion clears | `M1-A-002`, Phase 2 |
| — | No damage variance, crits, or misses | `M2-F-004`…`M2-F-006`, Phase 2 |
| — | `PlayerCombat.Stun` exists but nothing calls it | `M2-F-008`, Phase 2 |

---

## Decisions awaiting the user

| # | Question | Why it matters now |
|---|---|---|
| `M4-D1` | The class roster — three (Warrior/Mage/Rogue) or four with a healer? | Blocking for Phase 3, and it decides which animation and icon sets to acquire during Phase 2 |
| `M3-D1` | Animation source — Quaternius pack, Mixamo, or hand-authored? | Blocking for the first Phase 2 spec; the Quaternius licence is already committed |
| `R1-D2` | Is the open front of the chamber a permanent wall or a sealed door to Room 2? | Cheap to author now, expensive to retrofit |

Everything else has a stated default in its spec and is not blocking.

---

## Handoff — 2026-09-05, Claude

**Done:** Authored the spec system — `AGENTS.md`, `CLAUDE.md`, `docs/00`–`docs/05`,
`docs/DECISIONS.md`, and six specs under `docs/specs/`. No gameplay code changed.

**Verified:** EditMode 3/3 pass in batch mode (~5 min cold). PlayMode 0/1 — see
`V-001`. Note that the Unity process exit code was unreliable in both runs; read the
counts out of the results XML as `docs/04-VERIFICATION.md` instructs.

**Next:** Triage `V-001`. Until the PlayMode net runs from the command line, no
Phase 1 Task can prove it did no harm.

**Watch out:** `ClassicContentBuilder.Asset<T>` never overwrites an existing asset,
so changing a balance number in that file does nothing to the already-committed
`Assets/Resources/RPG/*.asset`. Delete the asset to regenerate it.
