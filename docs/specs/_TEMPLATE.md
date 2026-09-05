# <SCOPE> · <Title>

> **Status:** Draft | Active | Closed
> **Phase:** <n> — see [../02-ROADMAP.md](../02-ROADMAP.md)
> **Owner:** <agent or user>
> **Depends on:** <spec ids, or "nothing">

---

## 1. Intent

Two or three sentences. What the player experiences when this is right, and why it
matters. Not an implementation description.

## 2. Current state

What exists today, with file and line references. Be specific and honest — this
section is how the next agent avoids rebuilding something that already works.

## 3. Requirements

Grouped by category. Every line gets a permanent ID (see
[../00-WORKFLOW.md](../00-WORKFLOW.md) §2). Write them so a failure is unambiguous.

### Functional (`F`)

| ID | Requirement | Acceptance |
|---|---|---|
| `X-F-001` | The thing that must be true | How we prove it — test name, or "Manual: …" |

### Presentation (`A`)
### State integrity (`N`)
### Performance (`P`)
### UX / accessibility (`X`)

## 4. Out of scope

What this spec deliberately does not cover, and which spec owns it instead. Prevents
scope creep and duplicated work.

## 5. Open decisions

| # | Question | Options | Blocking? |
|---|---|---|---|

Anything the user must choose. Mark blocking items clearly; non-blocking ones get a
stated default so work can continue.

## 6. Verification plan

The concrete steps from [../04-VERIFICATION.md](../04-VERIFICATION.md) that close this
spec, including every manual check written out for the user to run.
