# 00 · Workflow

How work happens in this repository. Read once, follow every session.

---

## 1. The unit of work: a Task

A Task is a slice of one spec, small enough to finish and verify in a single
session. Tasks are named for the requirements they close:

> **Task:** `R1-F-004..007` — replace the primitive brazier with art and give it
> a lit/unlit visual state.

A Task is never "improve combat." It is always a list of requirement IDs.

---

## 2. Requirement IDs

Every requirement in every spec has a stable ID. Format:

```
<SCOPE>-<CATEGORY>-<NUMBER>
```

| Part | Values |
|---|---|
| SCOPE | `R1`, `R2`… (rooms) · `Q1`, `Q2`… (quests) · `M1`…`M6` (mechanics) |
| CATEGORY | `F` functional · `A` art/presentation · `N` netcode/authority · `P` performance · `X` accessibility/UX |
| NUMBER | Zero-padded, three digits, never reused |

IDs are permanent. If a requirement is dropped, mark it `~~R1-F-012~~ (dropped, see
DECISIONS-0007)` — never delete the line and never renumber.

Reference IDs in:
- commit messages — `feat(room1): brazier art + lit state (R1-F-004..007)`
- test names — `[Test] public void R1_F_004_BrazierUsesArtAssetNotPrimitive()`
- `docs/05-STATUS.md` checkboxes

---

## 3. The loop

### Step 1 — Read
Read, in order: `AGENTS.md` → `docs/05-STATUS.md` (current milestone + active spec)
→ the active spec → `docs/01-ARCHITECTURE.md` for anything you're about to touch.

Do not skip to the code. Most bad changes in this project come from an agent
implementing a mechanic that already exists somewhere else.

### Step 2 — Plan
Post a short plan before writing code:

- the requirement IDs you will close
- the files you will touch
- how each acceptance criterion will be verified
- anything in the spec that is wrong, ambiguous, or impossible

If the spec is wrong, **stop and fix the spec first**. A spec amendment is a normal,
cheap event. Silently building something different is not.

### Step 3 — Implement
- Smallest change that satisfies the requirement.
- Obey every hard rule in `AGENTS.md §3`.
- If a requirement forces you to break a hard rule, the rule wins — go back to Step 2.

### Step 4 — Verify
Run the protocol in `docs/04-VERIFICATION.md`. Every acceptance criterion is either
covered by a test you ran, or explicitly flagged as manual with the steps written out.

Never write "should work." Either it was verified or it wasn't.

### Step 5 — Record
Update `docs/05-STATUS.md`:
- tick closed requirements
- add anything you discovered but did not fix to **Deferred**
- update the "Current milestone" line if the milestone moved

Amend the spec if the built thing differs from the written thing. Add a
`docs/DECISIONS.md` entry for anything future-you would ask "why?" about.

### Step 6 — Commit
One Task per commit. Message format:

```
<type>(<scope>): <summary> (<requirement IDs>)

<why, if not obvious>
```

Types: `feat` `fix` `art` `spec` `test` `refactor` `chore`.
Scopes: `room1` `quest1` `combat` `movement` `anim` `class` `ui` `docs`.

---

## 4. Two agents, one repo

Claude Code and Codex both work here. They must not both be mid-Task on the same
files.

**The lock is `docs/05-STATUS.md`.** Before starting, an agent writes its name and
the Task into the *In progress* table. When finished, it clears the row. If a row is
already claimed for files you need, pick a different Task.

Suggested split, because it plays to each tool's strengths:

| Work | Who |
|---|---|
| Spec authoring, architecture changes, anything cross-cutting | Claude Code |
| Well-specified single-file implementation, test writing, mechanical refactors | Either |
| Long grinding passes across many similar call sites | Codex |

Neither agent may change `AGENTS.md`, `docs/00-WORKFLOW.md`, or
`docs/01-ARCHITECTURE.md` without the user's explicit go-ahead. Specs under
`docs/specs/` may be amended freely as long as the amendment is called out.

**Handoff note.** When you stop mid-milestone, leave the next agent a short block at
the bottom of `docs/05-STATUS.md`:

```
### Handoff — <date>, <agent>
Done: R1-F-004, R1-F-005
Next: R1-F-006 (brazier lit state) — the art asset is loaded but the material swap
      is unwritten; see SceneBuilder.cs:~330
Watch out: re-running SceneBuilder resets the brazier transform; the offsets in
      the spec are correct, the ones in the scene are not.
```

---

## 5. Phase order (do not reorder)

The user's sequence, which the roadmap follows:

1. **Finish one room and one questline** to a shippable standard. This is the
   vertical slice that defines "good" for everything after it.
2. **Perfect the mechanics** against that room — combat, movement, camera, animation,
   AI, feel.
3. **Class design** on top of proven mechanics.
4. **Then** the second room.

Within a phase, work one spec at a time. Within a spec, work top to bottom.

---

## 6. When you are stuck or disagree

Say so in one or two sentences, propose the alternative, and — unless proceeding
would be unsafe or waste the work — carry on with the stated assumption. Do not stall
a whole Task on a question you can answer with a reasonable default and a note.

Things that *do* warrant stopping and asking:
- a change that would break saved player data, or reintroduce a networking dependency
- a decision that locks in a class fantasy or an art direction
- anything that would make a hard rule in `AGENTS.md` unenforceable
