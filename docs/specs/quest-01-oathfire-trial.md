# Q1 · Quest 01 — The Oathfire Trial

> **Status:** Active
> **Phase:** 1 — the vertical slice
> **Depends on:** [room-01-oathfire-chamber](room-01-oathfire-chamber.md)

---

## 1. Intent

The classic 2004 quest loop, complete and legible, in one sitting: a yellow `!` over
an NPC's head, a briefing worth reading, a kill-and-collect objective, a boss, a
world interaction that visibly changes the room, and a turn-in that hands over gold,
XP, and an item you keep.

A player who has never seen the game should be able to finish it without being told
anything, and a player who played WoW in 2004 should recognise every beat.

## 2. Current state

`QuestManager` (`Assets/Scripts/Quest/QuestManager.cs`), `Interactable`, `QuestData`
(`Resources/RPG/OathfireTrial.asset`), `QuestMarker`, and the HUD quest tracker and
journal. Working today:

Stage machine, owned by `QuestManager`:

| Stage | Meaning |
|---|---|
| 0 | Not started — Mira shows `!` |
| 1 | Kill 3 guardians, collect 3 Oathfire Embers |
| 2 | Defeat the Oathbound Warden |
| 3 | Carry the embers to the Oathfire — brazier shows `?` |
| 4 | Return to Mira — each participant claims once |
| 5 | Complete; the trial can be repeated |

- Single-player. The claim is recorded before rewards are granted, so a repeated
  turn-in cannot pay twice — asserted by the PlayMode test.
- Rewards: 80 copper, 100 XP, 2 potions, Seal of the Hearth.
- Dialogue actions are whitelisted per dialogue, so an action ID that was never
  offered does nothing — asserted by the PlayMode test.
- The briefing is solo-facing ("Return with the embers and earn the hearth's
  blessing"); the old party wording was migrated in `ClassicContentBuilder`.

Known gaps: quest text and stage logic are hardcoded rather than data (`D-05`); no
audio or visual celebration on completion; no quest-accepted/completed sound; the
journal is functional but sparse.

## 3. Requirements

### Functional (`F`)

| ID | Requirement | Acceptance |
|---|---|---|
| `Q1-F-001` | The full loop 0 → 5 completes solo with no soft-lock at any stage | Covered by `TrialIntegrationTests` — keep green |
| ~~`Q1-F-002`~~ | ~~The full loop completes with two players~~ | Withdrawn — single-player. See `DECISIONS-0006` |
| `Q1-F-003` | The quest can be repeated from stage 5 and fully resets kills, collected count, claim, and ember inventory | PlayMode: `Q1_F_003_RepeatResetsAllProgress` |
| `Q1-F-004` | Killing guardians before accepting the quest grants no progress | PlayMode: `Q1_F_004_KillsBeforeAcceptDoNotCount` |
| `Q1-F-005` | The objective text in the tracker is correct and specific at every stage, including the count of each objective | EditMode: `Q1_F_005_ObjectiveTextCoversEveryStage` iterates 0–5 and asserts non-empty, distinct strings |
| `Q1-F-006` | The journal (`L`) shows briefing, current objectives with counts, and the full reward list | Manual Q1-M-2 |
| `Q1-F-007` | Abandoning is possible and returns the quest to stage 0 without leaving stray embers or spawned guardians | PlayMode: `Q1_F_007_AbandonCleansUp` — **new behaviour, does not exist yet** |
| `Q1-F-008` | Quest items are removed from inventory on turn-in and on abandon | Partly covered (`ClearQuestItem` on brazier); extend for abandon |
| ~~`Q1-F-009`~~ | ~~All participants disconnecting resets the quest~~ | Withdrawn — single-player. See `DECISIONS-0006` |

### Presentation (`A`)

| ID | Requirement | Acceptance |
|---|---|---|
| `Q1-A-001` | Accepting the quest plays an accept sound and a brief toast; completing plays a distinct, more triumphant one | Manual Q1-M-3. Ambience exists (`ChamberAmbience`); per-action audio is Phase 2 |
| `Q1-A-002` | Objective progress updates visibly in the tracker the instant it changes — count animates or flashes, never silently increments | Manual Q1-M-1 |
| `Q1-A-003` | The dialogue window uses the parchment style, types the briefing in rather than snapping it on, and can be skipped with a click | Manual Q1-M-2 |
| `Q1-A-004` | Lighting the Oathfire is a visible event in the room — the brazier ignites, the room warms, everyone present sees it at once | Manual Q1-M-1, and `R1-F-005` |
| `Q1-A-005` | Reward hand-over shows the item, the coin split (gold/silver/copper), and the XP gained | Manual Q1-M-2 |

### State integrity (`N`)

| ID | Requirement | Acceptance |
|---|---|---|
| `Q1-N-001` | No dialogue action ID can grant a reward, item, gold, or heal outside the state that offers it | Existing assertion in `TrialIntegrationTests`; extend to every action ID 0–9 at every stage |
| ~~`Q1-N-002`~~ | ~~A client cannot force a stage change~~ | Withdrawn — single-player. See `DECISIONS-0006` |
| `Q1-N-003` | Rewards are granted exactly once, even under repeated rapid turn-in attempts | Existing double-claim assertion; add a 20× loop |
| `Q1-N-004` | The rest-at-hearth action cannot be used to full-heal during combat | `Interactable.Choose` checks `LastCombatTime`; add `Q1_N_004_CannotRestInCombat` |

### UX (`X`)

| ID | Requirement | Acceptance |
|---|---|---|
| `Q1-X-001` | At every stage, the tracker tells the player exactly where to go next | Manual Q1-M-1 |
| `Q1-X-002` | A player who walks away and returns 10 minutes later can tell what they were doing from the tracker alone | Manual Q1-M-1 |

## 4. Out of scope

- Generalising `QuestData` to arbitrary quests → Phase 4. **Resist this.** The
  vertical slice is more valuable than a quest framework built on guesses.
- A second quest, a quest chain, or a quest log with multiple entries → Phase 4
- Enemy behaviour and difficulty → [mech-02-combat-gcd](mech-02-combat-gcd.md)

## 5. Open decisions

| # | Question | Options | Blocking? |
|---|---|---|---|
| ~~Q1-D1~~ | ~~Shared party progress or per-player?~~ | Resolved by the single-player rework — there is one player |
| Q1-D2 | Is quest abandon reachable from the journal, from Mira's dialogue, or both? | journal / dialogue / both | No — default both |
| Q1-D3 | Does the warden need a mechanic beyond a bigger health pool (a knockback, an enrage, adds)? | none / one telegraphed ability | No — default one telegraphed ability, specified in `mech-02` once combat is proven |

## 6. Verification plan

Automated: extend `Assets/Tests/PlayMode/TrialIntegrationTests.cs` — it is already the
regression net for this quest. New tests use the `Q1_*` naming from §3.

**Manual checks — for the user to run and tick.**

> **Q1-M-1 — the loop reads without help.**
> Start fresh, play to completion without touching the code or these docs. At every
> moment you should be able to answer "what now?" from the tracker alone. Note every
> point where you were unsure.

> **Q1-M-2 — the text and the windows.**
> Read the briefing in the dialogue window and again in the journal (`L`). Both must
> be legible, parchment-styled, and complete. On turn-in, the reward panel must show
> the item, the coin split, and the XP.

> **Q1-M-3 — celebration.** *(after Phase 2 per-action audio)*
> Accept and complete the quest. Both moments must sound and feel distinct, and
> lighting the Oathfire must be the most dramatic beat in the room.
