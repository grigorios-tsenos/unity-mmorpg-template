# 04 · Verification

"It should work" is not a result. This file defines what counts as proof.

---

## 1. The editor

```
/Applications/Unity/Hub/Editor/6000.6.0f1/Unity.app/Contents/MacOS/Unity
```

Unity holds an exclusive lock on the project. **Close the Unity Editor before running
anything below**, or the batch process will block on the lock. Only one batch run at
a time.

A cold run in a fresh worktree re-imports every asset — budget ~5 minutes for the
first EditMode run, seconds thereafter.

---

## 2. Commands

Set once per shell:

```bash
UNITY=/Applications/Unity/Hub/Editor/6000.6.0f1/Unity.app/Contents/MacOS/Unity
```

### EditMode tests — data and pure logic

```bash
"$UNITY" -batchmode -nographic -projectPath "$PWD" -runTests -testPlatform EditMode -testResults /tmp/EditMode.xml -logFile /tmp/unity-editmode.log
```

### PlayMode tests — the live game, host mode, netcode

```bash
"$UNITY" -batchmode -nographic -projectPath "$PWD" -runTests -testPlatform PlayMode -testResults /tmp/PlayMode.xml -logFile /tmp/unity-playmode.log
```

### Regenerate scenes, prefabs and content assets

```bash
"$UNITY" -batchmode -nographic -quit -projectPath "$PWD" -executeMethod SceneBuilder.Build -logFile /tmp/unity-build.log
```

### Reading the result

Exit code `0` means the run passed; `2` means tests failed; `3`/`4` mean the run
itself broke. Do not trust the exit code alone — read the counts:

```bash
grep -o 'total="[0-9]*" passed="[0-9]*" failed="[0-9]*"' /tmp/EditMode.xml | head -1
grep -n "error CS\|Exception\|Assertion" /tmp/unity-editmode.log | head -20
```

A run with `failed="0"` but compiler errors in the log is a **failure** — the tests
never ran.

---

## 3. What gets which kind of test

| Kind of requirement | Test |
|---|---|
| Balance numbers, data relationships, ScriptableObject invariants | EditMode |
| Pure logic: state machines, formulas, cooldown arithmetic | EditMode |
| Server authority, RPC validation, quest flow, loot, rewards | PlayMode, host mode |
| Anything requiring a real frame: movement, collision, camera | PlayMode |
| Look, feel, animation quality, audio mix | **Manual — write the steps out** |

### Test naming

Name tests for the requirement they close so a failure points at a spec line:

```csharp
[Test]      public void R1_F_004_BrazierUsesArtAssetNotPrimitive() { … }
[UnityTest] public IEnumerator Q1_N_003_ArbitraryDialogueIdCannotGrantRewards() { … }
```

### Existing coverage — extend, don't duplicate

- `Assets/Tests/Editor/ClassicRulesTests.cs` — HFSM ancestor transitions; guardian
  loot can actually satisfy the quest's collect requirement; every ability has a real
  cost, a 1.5 s GCD, and the three resource types are covered.
- `Assets/Tests/PlayMode/TrialIntegrationTests.cs` — a full host-mode run of the
  trial: cast interruption by movement, mana not charged on a cancelled cast, kill
  and collect progress, boss stage, brazier, single-claim rewards, potion cooldown,
  energy ticks. This is the regression net for Phase 1; keep it green.

---

## 4. Manual verification

Some things only a person can judge. When a criterion is manual, the spec must name
the exact steps and the exact thing to look for. Write them so the *user* can run
them without reading code:

> **Manual — M3-A-002 (run cycle reads as weight, not sliding)**
> 1. Host the game, walk from the hearth to the proving circle holding W.
> 2. Watch the feet: they must plant, not skate. Contact should land on the beat of
>    the footstep sound.
> 3. Strafe left and right with Q/E while holding W — the body leans into the turn.

An agent may not tick a manual criterion. Only the user does, after looking.

---

## 5. Definition of "verified" for a Task

1. EditMode and PlayMode runs completed, counts read, log checked for `error CS`
   and exceptions.
2. `SceneBuilder.Build` re-run and the game launched at least once if the Task
   touched scenes, prefabs, or content assets.
3. Every automatable acceptance criterion has a named test that passes.
4. Every manual criterion is listed in the handoff with its steps, marked *awaiting
   user check*.
5. Console is clean during a play session of the affected room — no new errors,
   no new warnings.

Report exactly this, with the numbers. If a step was skipped, say which and why.
