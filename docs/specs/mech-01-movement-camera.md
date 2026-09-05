# M1 · Movement & Camera

> **Status:** Draft
> **Phase:** 2, order 1
> **Depends on:** nothing

---

## 1. Intent

The thing the player touches every single second. 2004 WoW movement is specific and
unusual by modern standards, and getting it *nearly* right feels worse than getting
it obviously wrong: right-click steers the character with the camera, left-click
orbits the camera without turning the character, both buttons together run forward,
A/D turn rather than strafe unless Q/E is used, and jumping locks your horizontal
momentum for the whole arc.

## 2. Current state

`PlayerController` (`Assets/Scripts/Core/PlayerController.cs`) and `CameraFollow`
(`Assets/Scripts/Core/CameraFollow.cs`).

Implemented and working:

- `Update` reads input and calls `SetMovement(input, heading, jump)`; `FixedUpdate`
  moves the `CharacterController`. `ReadInput = false` lets tests drive movement
  directly without synthesising real input.
- Right-click steer: heading follows `CameraFollow.Yaw` while RMB is held.
- Left+right mouse together forces forward movement.
- A/D turn the character (135°/s) when not steering, and strafe while steering;
  `Q` and `R` strafe left and right standalone.
- Jump momentum: `airMomentum` is captured at take-off and the player cannot change
  direction mid-air — this is the 2004 behaviour and it is deliberate.
- Gravity −22, jump speed 7, move speed 4.6, turn speed 135°/s; backpedalling is
  slower than running forward.
- Camera: orbit on LMB/RMB, pitch clamped −10…70°, scroll zoom 2.5–11 m,
  sphere-cast occlusion against world geometry only (characters are on layer 2).
- Cursor locks while a mouse button is held and unlocks on release.
- Input is ignored while dead, stunned, or while the HUD has focus.

Gaps: no auto-run, no walk toggle, no fall damage, no slope limit tuning, and no
camera occlusion smoothing — the camera snaps when occlusion clears.

## 3. Requirements

### Functional (`F`)

| ID | Requirement | Acceptance |
|---|---|---|
| `M1-F-001` | W/S move forward/back relative to the character's facing | PlayMode: `M1_F_001_ForwardIsRelativeToFacing` |
| `M1-F-002` | A/D turn the character in place at a constant rate when not steering with the mouse | PlayMode: `M1_F_002_TurnRateIsConstant` |
| `M1-F-003` | Strafe left and right both work with and without mouse steering, on keys that do not collide with interact | **Done** — `Q` / `R` strafe standalone, A/D while steering. Add `M1_F_003_StrafeBothDirections` to lock it in |
| `M1-F-004` | Right-click held steers the character to the camera yaw; releasing it leaves the character facing where it was | PlayMode: `M1_F_004_RightClickSteers` |
| `M1-F-005` | Left-click orbits the camera without turning the character | Manual M1-M-1 |
| `M1-F-006` | Both mouse buttons held runs forward | PlayMode: `M1_F_006_BothButtonsRunForward` |
| `M1-F-007` | Jump preserves horizontal momentum for the whole arc and cannot be steered mid-air | PlayMode: `M1_F_007_AirMomentumIsLocked` |
| `M1-F-008` | Jumping is impossible while airborne (no double jump, no bunny-hop speed gain) | PlayMode: `M1_F_008_NoDoubleJump` |
| `M1-F-009` | Auto-run toggles on a key and cancels on any backward input | PlayMode: `M1_F_009_AutoRunTogglesAndCancels` — **new** |
| `M1-F-010` | Movement is blocked while dead, stunned, or while the HUD holds focus | Already implemented; add `M1_F_010_InputBlockedStates` |
| `M1-F-011` | The character cannot climb a wall by holding forward into it, and slope limit produces sensible behaviour on stairs and prop edges | Manual M1-M-2 |

### Camera (`A`)

| ID | Requirement | Acceptance |
|---|---|---|
| `M1-A-001` | The camera never clips inside world geometry | PlayMode: `M1_A_001_CameraNeverInsideGeometry` samples 64 yaw/pitch pairs against the room |
| `M1-A-002` | Occlusion pull-in and release are smoothed, not snapped | Manual M1-M-1 |
| `M1-A-003` | Zoom is smooth and clamped; scrolling past minimum does not enter first person unexpectedly | Manual M1-M-1 |
| `M1-A-004` | The camera never passes through a character (characters do not occlude) | Already correct via layer mask; add `M1_A_004_CharactersDoNotOccludeCamera` |
| `M1-A-005` | Pitch clamp prevents looking through the floor or straight up into nothing | Covered by the −10…70° clamp; assert it |
| `M1-A-006` | The cursor unlocks cleanly on every path out of mouse-look, including opening a dialogue mid-drag | Manual M1-M-3 |

### Input integrity (`N`)

| ID | Requirement | Acceptance |
|---|---|---|
| `M1-N-001` | `SetMovement` clamps its input to unit magnitude and rejects non-finite values, so a bad caller cannot fling the character | PlayMode: `M1_N_001_SetMovementClampsInput` |
| ~~`M1-N-002`~~ | ~~Remote players move smoothly with no rubber-banding~~ | Withdrawn — single-player. See `DECISIONS-0006` |
| ~~`M1-N-003`~~ | ~~A disconnecting client's avatar stops rather than sliding~~ | Withdrawn — single-player. See `DECISIONS-0006` |

### Performance / feel (`P`)

| ID | Requirement | Acceptance |
|---|---|---|
| `M1-P-001` | Input-to-visible-movement latency is imperceptible (well under 100 ms) | Manual M1-M-1 |
| `M1-P-002` | Movement produces no per-frame allocation | Profiler, 0 B/frame |

## 4. Out of scope

- Animation of the movement → [mech-03-animation](mech-03-animation.md)
- Mounts, swimming, flight — not in this project
- Anything network-related — the game is single-player

## 5. Open decisions

| # | Question | Options | Blocking? |
|---|---|---|---|
| ~~M1-D1~~ | ~~Where does strafe-right go?~~ | Resolved: `Q`/`R` strafe, `E` stays interact. `R` is not 2004-accurate but avoids the collision; revisit if rebinding lands |
| M1-D2 | Auto-run key | `Num Lock` (2004-accurate) / `R` / user-configurable | No — default configurable, shipping with `R` |
| M1-D3 | Should input send rate stay at 20 Hz? | 20 / 30 / 60 | No — default 20 until `M1-P-001` measures otherwise |

## 6. Verification plan

> **M1-M-1 — the mouse feels right.**
> Hold left-click and drag: the camera orbits, the character keeps facing forward.
> Hold right-click and drag: the character turns with the camera. Hold both: you run.
> Walk behind a pillar so the camera is pushed in — it should ease in and ease back
> out, never snap.

> **M1-M-2 — geometry.**
> Run into every wall and prop holding forward. You should not climb, stick, or
> vibrate. Jump onto the crate stack and off it.

> **M1-M-3 — cursor.**
> While holding right-click and turning, press `E` on Mira to open the dialogue.
> The cursor must appear and be usable immediately, and the character must stop.

> **M1-M-4 — backpedalling and turn feel.**
> Walk backwards: it must be visibly slower than forward. Turn with A/D at a steady
> rate, then hold right-click and turn with the mouse — the two must not fight.
