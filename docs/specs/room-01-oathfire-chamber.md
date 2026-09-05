# R1 · Room 01 — The Oathfire Chamber

> **Status:** Active
> **Phase:** 1 — the vertical slice
> **Depends on:** nothing (this is the foundation)

---

## 1. Intent

A 20 × 16 m stone-and-timber hall, warm and lived-in on the west side, cold and
ceremonial on the east. It is the first thing a player sees and the only space they
occupy for the whole of Phase 1, so it has to carry the entire 2004 fantasy in one
room: torchlight on painted stone, a hearth someone actually sleeps beside, banners,
and a dead brazier that the quest exists to relight.

When this room is finished, a player should want to walk around it before they talk
to anyone.

## 2. Current state

Built by `SceneBuilder.BuildScenery()` (`Assets/Editor/SceneBuilder.cs`). Working
today:

- 5 × 4 grid of 4 m floor tiles — wood west of x = −2, stone tile east.
- Back wall at z = −8 with arched windows at x = ±4; side walls at x = ±10;
  an invisible barrier at z = +8 so the front stays a camera cutaway.
- Box colliders behind every wall segment, the long table, and the crate stack.
- Hearth corner: long table with candles, food and a bottle; chair; stool; bed;
  large shelf; small table; candle shelf.
- Storage: barrels west, crates east.
- Heraldry: two red banners, a sword-and-shield, a shield banner.
- Four mounted torches with warm point lights, a hearth light, a moonlight spot
  through the windows.
- Mira (quest giver) at (−4.1, 0, 1.0); Toma (merchant) at (−6.2, 0, −2.6), both
  with `Interactable` + `QuestMarker`.
- The Oathfire at (0, 0, −6.1) — **a primitive cylinder and sphere**, not art.
- Guardians spawn at (4.8, 0.2, −1.5) in the eastern circle.

Known gaps carried from [../01-ARCHITECTURE.md](../01-ARCHITECTURE.md): `D-01`,
`D-02`, `D-03`, `D-09`.

## 3. Requirements

### Functional (`F`)

| ID | Requirement | Acceptance |
|---|---|---|
| `R1-F-001` | A player cannot leave the room in any direction, including by jumping onto furniture and over a wall | PlayMode: `R1_F_001_PlayerCannotEscapeChamber` walks/jumps into all four boundaries from 8 sample positions and asserts the player stays inside the 20 × 16 bounds |
| `R1-F-002` | Every prop a player can walk into has a collider matching its silhouette; nothing is walk-through-able that looks solid | PlayMode: sweep test asserts a collider exists within 0.5 m of each authored prop position in the solid list |
| `R1-F-003` | A player cannot become stuck on any prop, wall corner, or the brazier | Manual: R1-M-1 (below) |
| `R1-F-004` | The Oathfire brazier is an art asset, not primitive geometry | EditMode: `R1_F_004_BrazierUsesArtAsset` asserts the Oathfire has no `PrimitiveType` mesh and references a `.glb` under `Assets/Art` |
| `R1-F-005` | The brazier has two distinct visual states — cold (quest stage < 4) and lit (stage ≥ 4) — driven by replicated quest state, correct for late-joining clients | PlayMode: `R1_F_005_BrazierLightsOnStageFour` |
| `R1-F-006` | Player spawn and respawn use a named spawn point authored in the scene, not literals (`D-01`, `D-03`) | EditMode: `R1_F_006_SpawnPointIsAuthored` asserts no hardcoded spawn vector remains in `PlayerStats`/`PlayerSpawner` |
| `R1-F-007` | Multiple players spawning simultaneously never overlap each other or furniture | PlayMode: spawn 4 players, assert pairwise distance > 0.9 m and none intersects a collider |
| `R1-F-008` | The guardian spawn circle is authored in the scene, not a literal (`D-02`), and every spawn point is reachable and clear of props | EditMode + Manual R1-M-2 |
| `R1-F-009` | Toma sells potions and the transaction is server-validated; buying with insufficient copper fails silently and safely | PlayMode: `R1_F_009_MerchantRejectsUnderfundedPurchase` |

### Presentation (`A`)

| ID | Requirement | Acceptance |
|---|---|---|
| `R1-A-001` | No untextured primitive geometry is visible anywhere in the room | Manual R1-M-3 |
| `R1-A-002` | The lit brazier casts animated warm light and has a visible flame effect consistent with the Art Bible | Manual R1-M-3 |
| `R1-A-003` | Torch lights read as pools of warmth without blowing out the painted textures; the room is legible in every corner | Manual R1-M-3 |
| `R1-A-004` | Moonlight through the arched windows lands on the floor as a visible shaft | Manual R1-M-3 |
| `R1-A-005` | Mira and Toma face plausible directions and stand on the floor, not in it or above it | EditMode: assert NPC y ≈ floor height ± 0.05 |
| `R1-A-006` | The west (living) and east (proving) halves read as different places at a glance | Manual R1-M-3 |
| `R1-A-007` | Quest markers (`!` / `?`) float above NPC heads, always face the camera, and are visible from anywhere in the room | Manual R1-M-4 |

### Netcode (`N`)

| ID | Requirement | Acceptance |
|---|---|---|
| `R1-N-001` | A client joining mid-session sees the room in exactly the state the host does, including brazier state and NPC markers | PlayMode: two-client `R1_N_001_LateJoinerSeesRoomState` |
| `R1-N-002` | Interaction range (3.2 m) is enforced on the server for every interactable | Already covered by `PlayerCombat.InteractRpc`; add `R1_N_002_InteractionOutOfRangeRejected` |

### Performance (`P`)

| ID | Requirement | Acceptance |
|---|---|---|
| `R1-P-001` | The room renders in under 250 draw calls with 4 players and 4 guardians present | Manual R1-M-5 with the Frame Debugger |
| `R1-P-002` | No per-frame allocation from room scripts during a 60 s idle session | Manual R1-M-5 with the Profiler; GC alloc from `Chamber` scripts is 0 B/frame |

## 4. Out of scope

- Animation of characters in the room → [mech-03-animation](mech-03-animation.md)
- Enemy pathing around furniture → *mech-05-ai-navigation* (Phase 2)
- Audio and ambience → *mech-06-audio-feedback* (Phase 2)
- Doors, exits, or any second room → Phase 4
- Quest logic → [quest-01-oathfire-trial](quest-01-oathfire-trial.md)

## 5. Open decisions

| # | Question | Options | Blocking? |
|---|---|---|---|
| R1-D1 | Does the chamber get a ceiling, or stay open-topped for the cutaway camera? | (a) open — camera never fights geometry, but no overhead light shafts; (b) ceiling with the camera clipping through | No — default (a) |
| R1-D2 | Is the front (z = +8) an invisible wall forever, or does it become the doorway to Room 2? | (a) permanent wall; (b) authored as a sealed door now, opened in Phase 4 | No — default (b), it costs nothing and saves rework |
| R1-D3 | Should the proving circle be visually marked on the floor (inlaid ring, worn stone)? | yes / no | No — default yes; it explains where guardians appear |

## 6. Verification plan

Automated: EditMode + PlayMode per the table above, run per
[../04-VERIFICATION.md](../04-VERIFICATION.md).

**Manual checks — for the user to run and tick.**

> **R1-M-1 — nothing traps the player.**
> Host the game. Walk the full perimeter hugging the wall, then walk into each of:
> the long table, the bed, the shelf, both barrel stacks, the crate stack, each
> pillar, and the brazier. Jump onto anything you can. You must always be able to
> walk away without jumping. Report anything that grabs you.

> **R1-M-2 — the proving circle works as a stage.**
> Accept the quest and watch where guardians appear. Each one must appear on open
> stone, be immediately reachable, and be visible from the hearth side of the room.

> **R1-M-3 — the room looks like 2004.**
> Stand at the hearth, then at the circle, then in each corner. Check: no bare
> primitives; torch pools read warm without washing out; a moonlight shaft is visible
> on the floor; the west half feels lived-in and the east half feels ceremonial.

> **R1-M-4 — quest markers.**
> From four corners of the room, confirm Mira's `!` is legible and facing you. Accept
> the quest and confirm it disappears; reach stage 4 and confirm the `?` appears.

> **R1-M-5 — performance.**
> Window ▸ Analysis ▸ Frame Debugger with 4 guardians up: draw calls < 250.
> Profiler, 60 s idle: 0 B/frame GC allocation attributed to room scripts.
