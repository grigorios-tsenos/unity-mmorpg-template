# Decisions

Short architecture decision records. One entry per decision that future-you would
otherwise ask "why on earth?" about. Newest last. Never edit a closed entry — add a
superseding one.

Format: **number · date · title**, then Context / Decision / Consequences.

---

## 0001 · 2026-09-05 · Scenes, prefabs and content assets are generated, not hand-authored

**Context.** Unity scenes and prefabs are Editor-managed YAML with GUID
cross-references. Hand-editing them is error-prone, unreviewable in a diff, and
impossible for an agent to do reliably. Earlier attempts to wire uGUI from an Editor
script produced subtly broken windows (components caching null references in
`OnEnable`, edit-time `AddListener` calls that never serialise).

**Decision.** `SceneBuilder.cs` generates both scenes and both prefabs;
`ClassicContentBuilder.cs` generates the ScriptableObject content;
`RpgHud` builds the entire runtime UI in code. Generated files are committed so the
project opens and plays without running a build step first.

**Consequences.** Agents edit generators, never outputs. Re-running a builder must be
idempotent. The committed `.unity` and `.prefab` files will show large diffs on
regeneration — that is expected and is not a reason to hand-edit them.

---

## 0002 · 2026-09-05 · The server is the only authority; no client prediction

**Context.** This is LAN-scale co-op, not a latency-sensitive competitive game. Client
prediction and reconciliation is the single largest source of authority bugs in a
codebase this size.

**Decision.** Clients send intent at 20 Hz; the server simulates and `NetworkTransform`
replicates the result. Every server RPC validates permission, argument finiteness and
range, distance, line of sight, cooldown, and resource. No client-side prediction.

**Consequences.** Movement has one round-trip of latency, which is imperceptible on a
LAN and acceptable elsewhere for this project. Revisit only when a real latency
problem is measured — not preemptively. See `M1` "Out of scope".

---

## 0003 · 2026-09-05 · Gameplay talks to the UI only through `GameEvents`

**Context.** The gameplay assemblies must be testable and buildable without the UI,
and a headless server must never touch presentation code.

**Decision.** `Core.Runtime`, `Combat.Runtime` and `Quest.Runtime` may not reference
`UI.Runtime`. All gameplay-to-presentation communication goes through the static
`GameEvents` bus, which resets its handlers on `SubsystemRegistration` so domain
reloads and repeated PlayMode tests start clean.

**Consequences.** Adding a HUD feature means adding an event, not a reference. Any new
static gameplay state must also reset on `SubsystemRegistration` or PlayMode tests
will leak state between runs.

---

## 0004 · 2026-09-05 · Room by room, mechanic by mechanic — vertical slice before systems

**Context.** The project owner's explicit sequence: finish one room and one questline,
then perfect the mechanics, then design classes. The quest system is currently
hardcoded to a single trial, which is tempting to generalise immediately.

**Decision.** Phase 1 finishes Room 01 and Quest 01 to a shippable standard against
the existing hardcoded implementation. Generalising `QuestData`/`QuestManager` is
explicitly deferred to Phase 4, after the mechanics and classes are proven.

**Consequences.** Some Phase 1 work will be rewritten when the quest system is
generalised. That is the accepted cost: a framework designed against one finished,
playtested quest will be far better than one designed against a guess. Agents that
"helpfully" generalise early are working against the plan.

---

## 0005 · 2026-09-05 · Requirement IDs are permanent and traceable

**Context.** Two agents and a human work in this repo, across sessions with no shared
memory. Without stable identifiers, "done" is unfalsifiable.

**Decision.** Every requirement gets a permanent ID (`SCOPE-CATEGORY-NUMBER`) that
appears in the spec, the test name, the commit message, and the status tracker. IDs
are never reused or renumbered; dropped requirements are struck through with a
pointer to the decision that dropped them.

**Consequences.** A failing test names the spec line it violates. The status board can
be trusted. Slightly more ceremony per change — worth it.
