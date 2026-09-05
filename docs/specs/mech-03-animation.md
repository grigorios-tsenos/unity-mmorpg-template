# M3 · Animation

> **Status:** Draft — first spec of Phase 2
> **Phase:** 2, order 1
> **Depends on:** [room-01-oathfire-chamber](room-01-oathfire-chamber.md)

---

## 1. Intent

Right now every character in the game is a static mesh that slides across the floor
in a T-pose. It is the single largest gap between what this project is and what it is
trying to be — no amount of combat tuning reads correctly while the models don't move.

When this is done: characters idle with weight, run with foot contact, swing weapons
on the beat of the swing timer, flinch when hit, cast with a wind-up the enemy can
read, and fall over when they die.

## 2. Current state

Nothing. `SceneBuilder.BuildPlayerPrefab` and `BuildEnemyPrefab` instantiate the
`.glb` visual and add no `Animator`. There are no animation clips in `Assets/Art` —
only `Assets/Art/licenses/quaternius-animations.txt`, so the licence for a Quaternius
animation set is already cleared but the clips are not imported.

The information the animation layer needs already exists and is replicated:

- `PlayerStats.State` — `Idle | Moving | Casting | Stunned`
- `Enemy.State` — `Wander | Aggro | Chase | Combat | Leash` (+ `Passive`, `Alive`)
- `PlayerCombat.CastingSlot` / `CastEnd` — which spell, and when it lands
- `PlayerCombat.AutoAttacking` and the internal `swingReady` timer
- `GameEvents.Damage` — fires at the moment of every hit

**No new network traffic is needed.** The animation layer is a *consumer* of state
that already replicates. Adding `NetworkAnimator` would be the wrong answer.

## 3. Requirements

### Functional (`F`)

| ID | Requirement | Acceptance |
|---|---|---|
| `M3-F-001` | Player and enemy prefabs carry an `Animator` with an authored controller, added by `SceneBuilder` | EditMode: `M3_F_001_CharacterPrefabsHaveAnimator` |
| `M3-F-002` | Locomotion blends idle → walk → run from actual horizontal velocity, not from the input vector | PlayMode: `M3_F_002_LocomotionFollowsVelocity` |
| `M3-F-003` | The animation state is derived from replicated state, so remote players are animated correctly on every client | PlayMode: two-client `M3_F_003_RemotePlayersAnimate` |
| `M3-F-004` | Jump has distinct take-off, airborne, and land states, driven by `CharacterController.isGrounded` | Manual M3-M-1 |
| `M3-F-005` | Auto-attack swings play once per swing, synchronised to the server swing timer — the damage number appears on the impact frame, not the wind-up | PlayMode: `M3_F_005_SwingImpactAlignsWithDamage` (±100 ms) |
| `M3-F-006` | Casting plays a wind-up that lasts exactly `SpellData.castTime`, and is cut immediately when the cast is interrupted or cancelled | PlayMode: `M3_F_006_CastAnimationMatchesCastTime` |
| `M3-F-007` | Taking damage plays a hit reaction that does not interrupt locomotion (upper-body layer) | Manual M3-M-2 |
| `M3-F-008` | Death plays a fall, the body stays down for the respawn delay, and the character stands on respawn | PlayMode + Manual M3-M-2 |
| `M3-F-009` | NPCs (Mira, Toma) idle rather than stand frozen, and turn to face a player who interacts with them | Manual M3-M-3 |
| `M3-F-010` | Animation never drives gameplay — no root motion, no animation events that apply damage | EditMode: `M3_F_010_NoRootMotion` asserts `Animator.applyRootMotion == false` on all character prefabs |

### Presentation (`A`)

| ID | Requirement | Acceptance |
|---|---|---|
| `M3-A-001` | Transitions between locomotion states are under 0.15 s — snappy, 2004-style, not smoothed | Manual M3-M-1 |
| `M3-A-002` | The run cycle reads as weight, not sliding: foot plant matches ground speed | Manual M3-M-1 |
| `M3-A-003` | Weapon swings have a readable wind-up, fast strike, and recovery that fills the swing timer without looping awkwardly | Manual M3-M-2 |
| `M3-A-004` | Idle includes a slow breathing loop and an occasional fidget | Manual M3-M-1 |
| `M3-A-005` | Enemies telegraph their attack early enough for a player to react | Manual M3-M-2 |

### Performance (`P`)

| ID | Requirement | Acceptance |
|---|---|---|
| `M3-P-001` | Animation costs under 1 ms/frame with 4 players and 8 enemies | Manual M3-M-4, Profiler |
| `M3-P-002` | Off-screen and distant characters use culled animator updates | EditMode: assert `cullingMode == CullUpdateTransforms` |

## 4. Out of scope

- Facial animation, cloth, and IK — later, if ever; not 2004
- Weapon trails and particle effects → *mech-06-audio-feedback*
- Class-specific animation sets → [mech-04-classes](mech-04-classes.md)

## 5. Open decisions

| # | Question | Options | Blocking? |
|---|---|---|---|
| M3-D1 | Which animation source? | (a) Quaternius animation pack — licence already cleared, matches the existing character style; (b) Mixamo retarget; (c) hand-authored | **Yes** — this decides the retarget pipeline. Default recommendation: (a), because the licence is already in the repo and the rigs match |
| M3-D2 | Animator Controller as an authored asset, or built in code like the HUD? | (a) authored `.controller` asset; (b) generated by `SceneBuilder` | No — default (a); controllers are a poor fit for code generation and Unity's tooling is genuinely better here. This is a deliberate exception to the "generate everything" rule and needs a `DECISIONS.md` entry |
| M3-D3 | Do enemies and players share one controller with different clips, or get separate controllers? | shared / separate | No — default shared, overridden per character with an `AnimatorOverrideController` |

## 6. Verification plan

Automated per §3. Animation quality is inherently visual — most criteria are manual
and only the user may tick them.

> **M3-M-1 — locomotion feels right.**
> Walk, then run, then stop, in a straight line and in circles. Check: no sliding
> feet, transitions snap rather than smear, idle breathes, jumping has three distinct
> beats. Strafe with Q while running — the body should lean.

> **M3-M-2 — combat reads.**
> Pull one guardian. Watch a full fight without using abilities. Every swing should
> have a wind-up you can see, the damage number should appear when the weapon
> connects, hits should make you flinch without stopping you moving, and death should
> be a fall, not a disappearance.

> **M3-M-3 — the room is alive.**
> Stand still and watch Mira and Toma for 30 seconds. They must not be statues.
> Interact — they should turn to face you.

> **M3-M-4 — cost.**
> Profiler with 4 players and 8 enemies: the Animator sample stays under 1 ms/frame.
