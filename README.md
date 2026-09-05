# Azeroth04 · The Oathfire Chamber

The current focus is **one complete single-player room**. Open `Assets/Scenes/World.unity` and press Play. You start by the hearth immediately: no login screen, host button, connection, or network manager.

The project uses Unity **6000.6.0f1** and URP. The room and prefabs are already generated. **Azeroth04 → Build Oathfire Room** rebuilds the generated World scene and character prefabs while preserving authored content definitions. Save custom scene changes separately before rebuilding.

## The room

The west side contains the hearth, Mira, Toma's supplies, and the living quarters. The east contains a marked proving circle. The Oathfire brazier stands at the back of the hall.

Speak to Mira, accept the trial, defeat three guardians and collect their embers, defeat the warden, rekindle the brazier, and return for your reward. Loot is collected automatically. The quest can be repeated after turning it in. Toma sells healing potions; Mira offers rest outside combat.

Bundled character animations now drive idle, movement, melee, casting, and death. Torches flicker, the room has quiet hearth ambience, and the Oathfire visibly lights when rekindled.

## Controls

| Input | Action |
| --- | --- |
| W / S | Forward / backward (backpedaling is slower) |
| A / D | Turn; strafe while holding right mouse |
| Q / R | Strafe left / right |
| Right mouse | Camera look and character steering |
| Left mouse | Orbit independently |
| Both mouse buttons | Run forward |
| Wheel | Camera distance |
| Space | Jump, preserving takeoff momentum |
| Tab | Cycle nearby enemies |
| 1 | Toggle auto attack |
| 2 | Ember Bolt: timed Mana attack |
| 3 | Hearthlight: timed Mana self-heal |
| 4 | Heroic Strike: Rage melee attack |
| 5 | Quick Slash: Energy melee attack |
| 6 | Drink a healing potion |
| E | Interact within 3.2 meters |
| L | Open / close quest log |
| Escape | Close panels; clear target and interrupt combat |

The Mana / Rage / Energy buttons are training controls available outside combat. Mana and health regenerate every two seconds after five seconds out of combat. Energy restores 20 each tick. Rage builds on swings and incoming damage, then decays outside combat.

Abilities share a 1.5-second global cooldown. Moving or jumping interrupts timed casts without spending the resource cost. Potions share a 60-second cooldown. Enemies that lose pursuit return home and heal. Death returns you to the hearth after five seconds.

## Content and architecture

Tune spells, items, loot, enemies, and the quest in `Assets/Resources/RPG`. Gameplay uses local observable values and events, with four assemblies:

- **Core.Runtime:** movement, camera, character stats, resource ticks, inventory storage, definitions, events, and the reusable hierarchical state machine.
- **Combat.Runtime:** targeting, attacks, casts, cooldowns, interactions, and enemy states.
- **Quest.Runtime:** trial progression, NPC dialogue, purchases, and one-time reward claims.
- **UI.Runtime:** HUD, quest log, typed dialogue, action bar, character animations, markers, and room ambience.

There are no networking types or RPCs in the active gameplay code. The former connection scripts and Bootstrap scene are preserved outside Unity's Assets folder in `Archive/Networking` for reference. They are not part of the game.

## Art

Existing textured art is retained. Generated materials use URP Simple Lit diffuse with specular highlights disabled. Extracted diffuse textures import at a maximum of 1024px. No normal, metallic, or smoothness maps are configured. The HUD uses recessed dark frames, gold edging, parchment colors, yellow quest markers, and gold/silver/copper amounts.

The inherited models remain prototype art rather than a finished custom 2004-style asset set. This pass focuses on making the chamber coherent and playable using those assets.

## Checks and current limits

`Assets/Tests` contains EditMode checks for state transitions and authored content, and a PlayMode test for loading the room directly, floor contact, jumping and air momentum, cast interruption/completion, enemy leashing, kill/collect progress, reward duplication, potion cooldowns, resource ticks, and death recovery.

Use **Window → General → Test Runner**. The PlayMode test can also render a room preview when a graphics device is available.

Progress currently lasts for the play session. Persistence, additional rooms, and online play are outside the current room-focused scope. The resource selector is a training aid, not a finished class-selection system.
