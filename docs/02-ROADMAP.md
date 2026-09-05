# 02 · Roadmap

Ordered. Each phase finishes before the next begins. No parallel phases.

The order comes from the project owner: **finish a room and a questline first, then
perfect mechanics, then design classes.**

---

## Phase 1 — The vertical slice ▸ *in progress*

Take the one room and the one questline that already exist and finish them to a
standard we would ship. Everything after this phase is measured against it.

| Spec | What it covers |
|---|---|
| [room-01-oathfire-chamber](specs/room-01-oathfire-chamber.md) | The 20×16 m hall: layout, collision, lighting, props, NPC placement, the brazier |
| [quest-01-oathfire-trial](specs/quest-01-oathfire-trial.md) | Accept → kill 3 guardians + collect 3 embers → warden → rekindle → turn in |

**Exit criteria.** A player launches the game, is greeted by Mira, accepts the trial,
fights, rekindles the Oathfire, turns in, and is rewarded — with no placeholder
geometry, no console errors, no soft-locks, no stuck-on-furniture, and every step
legible without reading the code.

Animation and audio were the largest visible gaps in the slice. Both were closed by
the single-player rework on `main`: `CharacterVisual` drives idle, movement, melee,
casting and death, and `ChamberAmbience` / `TorchFlicker` / `OathfireVisual` give the
room life. Phase 2 refines them rather than building them.

---

## Phase 2 — Perfect the mechanics

Only after Phase 1's room and quest are closed. Animation and audio both landed with
the single-player rework, so this phase now starts from a moving, audible game rather
than from static meshes.

| Order | Spec | Why here |
|---|---|---|
| 1 | [mech-01-movement-camera](specs/mech-01-movement-camera.md) | Steering, momentum, jump, camera occlusion and orbit — the thing the player touches every second |
| 2 | [mech-02-combat-gcd](specs/mech-02-combat-gcd.md) | GCD, swing timers, resources, interrupts, threat, the target frame |
| 3 | [mech-03-animation](specs/mech-03-animation.md) | Now a *refinement* pass: blending, transition control, and getting swing impact onto the damage frame |
| 4 | *mech-05-ai-navigation* (to be written) | Aggro, leash, pathing around furniture (`D-04`), pull mechanics |
| 5 | *mech-06-audio-feedback* (to be written) | Per-action audio on top of the existing procedural ambience (`ChamberAmbience`) |

**Exit criteria.** Someone who played WoW in 2004 recognises the feel: the GCD reads
correctly, casts get interrupted the way they expect, mobs leash, the camera behaves,
and every action has an animation and a sound.

---

## Phase 3 — Class design

| Spec | Covers |
|---|---|
| [mech-04-classes](specs/mech-04-classes.md) | Class identity, resource ownership, the ability-loadout system, talent-free 1.12-style kits |

Depends on Phase 2 because a class is only as good as the combat model under it.
The roster is an **open decision** — see the spec.

**Exit criteria.** Class is chosen at character creation, drives the resource bar and
the ability loadout from data, and two classes play distinctly different in the same
fight.

---

## Phase 4 — Content systems, then Room 2

Only once one room, one questline, the mechanics, and the classes are proven.

- Generalise `QuestData`/`QuestManager` beyond the single hardcoded trial (`D-05`)
- Zone/room transition framework (doors, load boundaries, per-room spawn tables)
- Room 2 authored against the now-proven pipeline
- Persistence: character save/load (`D-11` — progress currently lasts one session)
- Multiplayer, if it is ever wanted again: `Archive/Networking/` holds the previous
  Netcode layer. Returning to it is a deliberate decision with a `DECISIONS.md`
  entry, never an incidental change

---

## Explicitly not now

Recorded so nobody starts them: mounts, professions, PvP, auction house, raid-size
groups, procedural content, and any second questline before Phase 4.
Multiplayer is out of scope: it was deliberately removed to finish the single-player
room first.
