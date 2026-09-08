# M3 · Animation

> **Status:** Draft — refinement pass, not a build-from-nothing
> **Phase:** 2, order 3
> **Depends on:** [room-01-oathfire-chamber](room-01-oathfire-chamber.md)

---

## 1. Intent

Characters now move — idle, jog, melee, cast and death all play. What is missing is
*quality*: there is no blending, transitions are instant, clips are looked up by name
string, and nothing guarantees a swing's impact frame lines up with the damage it
deals.

When this is done: characters idle with weight, run with foot contact, swing weapons
on the beat of the swing timer, flinch when hit, cast with a wind-up the enemy can
read, and fall over when they die — all of it blended rather than snapped.

## 2. Current state

`CharacterVisual` (`Assets/Scripts/UI/CharacterVisual.cs`) is on both the Player and
Guardian prefabs, added by `SceneBuilder`. It drives a **legacy `Animation`
component** on the bundled character models.

Working today:

- A 0.06 s polling coroutine picks a clip from character state: `Death01` when dead,
  `Spell_Simple_Idle_Loop` while casting, `Jog_Fwd_Loop` while moving, `Sword_Idle`
  for an enemy in combat, otherwise `Idle_Loop`.
- One-shot actions are event-driven: `PlayerCombat.AbilityPerformed` →
  `Spell_Simple_Shoot` for a timed cast or `Sword_Attack` for an instant;
  `Enemy.Attacked` → `Sword_Attack`. Each holds the rig for 0.7 s via `actionUntil`.
- Clips ending in `Loop` or containing `Idle` are set to `WrapMode.Loop`, everything
  else to `ClampForever`.
- The dependency direction is correct: presentation subscribes to gameplay events,
  gameplay never calls animation code.
- The PlayMode test asserts the rig exists (`GetComponentInChildren<Animation>()`).

Gaps (`D-12`): the legacy `Animation` component gives no blending or transition
control, so every state change is a hard cut. Clips are matched by name string with
an `EndsWith` fallback, which fails silently when a clip is missing or renamed. The
fixed 0.7 s `actionUntil` window is unrelated to actual clip length or to the swing
timer, so impact frames and damage are only coincidentally aligned. There is no hit
reaction and no jump animation.

## 3. Requirements

### Functional (`F`)

| ID | Requirement | Acceptance |
|---|---|---|
| `M3-F-001` | Character prefabs carry an `Animator` rig with blending and transition control | EditMode: `M3_F_001_CharacterPrefabsHaveAnimationRig` |
| `M3-F-002` | Locomotion blends idle → walk → run from actual horizontal velocity, not from the input vector | PlayMode: `M3_F_002_LocomotionFollowsVelocity` |
| `M3-F-003` | A missing or renamed clip fails loudly (a logged warning naming the clip) rather than silently doing nothing | EditMode: `M3_F_003_MissingClipIsReported` |
| `M3-F-004` | Jump has distinct take-off, airborne, and land states, driven by `CharacterController.isGrounded` | Manual M3-M-1 |
| `M3-F-005` | Auto-attack swings play once per swing and are driven by the actual swing timer and clip length, not a fixed 0.7 s window — the damage number appears on the impact frame | PlayMode: `M3_F_005_SwingImpactAlignsWithDamage` (±100 ms) |
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
| `M3-P-001` | Animation costs under 1 ms/frame with 8 enemies present | Manual M3-M-4, Profiler |
| `M3-P-002` | Off-screen and distant characters use culled animator updates | EditMode: assert `cullingMode == CullUpdateTransforms` |

## 4. Out of scope

- Facial animation, cloth, and IK — later, if ever; not 2004
- Weapon trails and particle effects → *mech-06-audio-feedback*
- Class-specific animation sets → [mech-04-classes](mech-04-classes.md)

## 5. Open decisions

| # | Question | Options | Blocking? |
|---|---|---|---|
| ~~M3-D1~~ | ~~Which animation source?~~ | Resolved by events — bundled clips on the existing character models are already wired |
| ~~M3-D2~~ | ~~Migrate from the legacy `Animation` component to an `Animator` controller for real blending (`D-12`)?~~ | **Resolved — migrate.** The authored controller is an explicit generated-content exception recorded in `DECISIONS-0010` |
| ~~M3-D3~~ | ~~Do enemies and players share one controller with different clips, or get separate controllers?~~ | **Resolved — shared controller with `AnimatorOverrideController` per character.** See `DECISIONS-0010` |

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
> Profiler with 8 enemies: the animation sample stays under 1 ms/frame.
