# M1 · Movement & Camera

> **Status:** Draft
> **Phase:** 2, order 2
> **Depends on:** [mech-03-animation](mech-03-animation.md)

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

- Server-authoritative: the client sends `(input, heading, jump)` at 20 Hz via
  `SubmitRpc`; only the server moves the `CharacterController`.
- Right-click steer: heading follows `CameraFollow.Yaw` while RMB is held.
- Left+right mouse together forces forward movement.
- A/D turn the character (120°/s) when not steering; Q strafes left.
- Jump momentum: `airMomentum` is captured at take-off and the player cannot change
  direction mid-air — this is the 2004 behaviour and it is deliberate.
- Gravity −20, jump speed 7, move speed 5.
- Camera: orbit on LMB/RMB, pitch clamped −10…70°, scroll zoom 2.5–11 m,
  sphere-cast occlusion against world geometry only (characters are on layer 2).
- Cursor locks while a mouse button is held and unlocks on release.
- Stale input (>0.25 s) zeroes movement; input is ignored while dead, stunned, or
  while the HUD has focus.

Gaps: **strafe is asymmetric.** While right-click steering, A/D strafe correctly
(the turn-conversion is skipped and `input.x` passes through). Without steering,
`Q` strafes left but there is no right-hand counterpart — `E` is bound to interact
(`PlayerCombat.Update`), so `Input.GetKey(KeyCode.Q)` has no partner in
`PlayerController.Update`. Also missing: auto-run, walk toggle, fall damage, slope
limit tuning, and camera occlusion smoothing (the camera snaps when occlusion
clears).

## 3. Requirements

### Functional (`F`)

| ID | Requirement | Acceptance |
|---|---|---|
| `M1-F-001` | W/S move forward/back relative to the character's facing | PlayMode: `M1_F_001_ForwardIsRelativeToFacing` |
| `M1-F-002` | A/D turn the character in place at a constant rate when not steering with the mouse | PlayMode: `M1_F_002_TurnRateIsConstant` |
| `M1-F-003` | Strafe left and right both work with and without mouse steering, on keys that do not collide with interact | PlayMode: `M1_F_003_StrafeBothDirections` — **partly broken: standalone strafe-right is unbound** |
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

### Netcode (`N`)

| ID | Requirement | Acceptance |
|---|---|---|
| `M1-N-001` | A modified client cannot move faster than the authored speed, teleport, or fly | PlayMode: `M1_N_001_ForgedInputCannotExceedSpeed` sends out-of-range and non-finite input and asserts server clamping |
| `M1-N-002` | Remote players move smoothly on other clients with no visible rubber-banding at 20 Hz input | Manual M1-M-4 |
| `M1-N-003` | A disconnecting or hitching client's avatar stops rather than sliding | Already implemented (0.25 s stale-input cutoff); assert it |

### Performance / feel (`P`)

| ID | Requirement | Acceptance |
|---|---|---|
| `M1-P-001` | Input-to-visible-movement latency on a local host is under 100 ms | Manual M1-M-4 |
| `M1-P-002` | Movement produces no per-frame allocation | Profiler, 0 B/frame |

## 4. Out of scope

- Animation of the movement → [mech-03-animation](mech-03-animation.md)
- Mounts, swimming, flight — not in this project
- Client-side prediction and reconciliation. **Deliberately excluded**: at LAN
  latency the current model feels fine, and prediction is the single biggest source
  of authority bugs. Revisit only if a real latency problem is measured.

## 5. Open decisions

| # | Question | Options | Blocking? |
|---|---|---|---|
| M1-D1 | `E` is currently interact. Where does strafe-right go? | (a) move interact to `F`, free `Q`/`E` for strafe (2004-accurate); (b) keep interact on `E`, strafe right on `C` | No — default (a); it matches the reference and interact-on-F is a common rebind anyway |
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

> **M1-M-4 — two players.**
> Multiplayer Play Mode, two players. Watch the other avatar run in circles and jump.
> No rubber-banding, no sliding after they stop, no teleporting.
