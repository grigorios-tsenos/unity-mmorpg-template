# 01 · Architecture

Ground truth for how the code is laid out today. If you change the shape of the
system, update this file in the same commit.

**The project is single-player.** There is no server, no client, and no networking in
`Assets/`. The former Netcode layer is preserved in `Archive/Networking/` for
reference only — it is outside the Unity project and does not compile into the game.

---

## 1. Assemblies

| Assembly | Path | References |
|---|---|---|
| `Core.Runtime` | `Assets/Scripts/Core` | *(none)* |
| `Combat.Runtime` | `Assets/Scripts/Combat` | Core |
| `Quest.Runtime` | `Assets/Scripts/Quest` | Core, Combat |
| `UI.Runtime` | `Assets/Scripts/UI` | Core, Combat, Quest, UnityEngine.UI |
| `Azeroth.Editor.Tests` | `Assets/Tests/Editor` | all runtime |
| `Azeroth.PlayMode.Tests` | `Assets/Tests/PlayMode` | all runtime |

**The arrow never reverses.** Gameplay assemblies must not reference `UI.Runtime`.

Namespaces do not map 1:1 to folders — `PlayerController` and `CameraFollow` live in
`Assets/Scripts/Core` under `MmoTemplate.Player`; most gameplay is `MmoTemplate.Rpg`.
`CharacterVisual` sits in `UI.Runtime` but uses the `MmoTemplate.Rpg` namespace. The
asmdef boundaries are what matter, not the namespace names.

---

## 2. Runtime map

One scene. Press Play in `World.unity` and you are standing in the room.

```
World.unity
├── GameManager
│     ├── EnemySpawner        spawns guardians / the warden into the circle
│     └── QuestManager        the trial stage machine
├── RpgHud                    builds the entire UI in code at runtime
├── Main Camera + CameraFollow
├── Chamber ambience          ChamberAmbience — procedurally generated hearth bed
├── Player                    (prefab instance, placed at the hearth)
└── Chamber                   all scenery, NPCs, torches, the Oathfire
```

Player prefab: `CharacterController` + `PlayerStats` + `PlayerController` +
`PlayerCombat` + `CharacterVisual` + `Visual` (wayfarer.glb).
Guardian prefab: `CharacterController` + `Enemy` + `CharacterVisual` + toma.glb.

Both are on **layer 2 (Ignore Raycast)** so line-of-sight checks and camera occlusion
(`layerMask = 1`, Default only) see world geometry but not characters. Anything that
should block line of sight must be on layer 0 with a collider.

---

## 3. State ownership

`ObservableValue<T>` (`Core.Runtime`) is the spine: a plain value that raises
`OnValueChanged(previous, next)` only when the value actually changes. It replaced
`NetworkVariable<T>` when networking was removed, and it keeps the same
subscribe-to-change shape the HUD was already written against.

| Flow | Path |
|---|---|
| Movement | `PlayerController.Update` reads input → `SetMovement(input, heading, jump)` → `FixedUpdate` moves the `CharacterController`. `ReadInput = false` lets tests drive movement directly |
| Ability use | key → `Use(slot)` → `TryCast` validates GCD, cooldown, range, LOS, resource, movement → `CastingSlot`/`CastEnd` → `FinishCast` applies the effect and raises `AbilityPerformed` |
| Interaction | `E` → nearest `InteractionPoint` within 3.2 m → `Interact(player)` → `SendDialog` |
| Dialogue choice | HUD button → `ChooseAction(id)` → the id must be in the offered whitelist **and** the player still in range → `IInteraction.Choose` |
| Quest progress | `QuestManager` owns `Stage`, `Kills`, `Collected` as `ObservableValue<int>` |

**Invariants worth protecting — these are what stop stray calls corrupting state:**
- `ChooseAction` consumes a whitelist of offered action IDs; an ID that was never
  offered does nothing. Asserted by the PlayMode test.
- The reward claim is recorded before rewards are granted, so a double turn-in
  cannot pay twice. Asserted.
- Casts spend their resource in `FinishCast`, never at cast start — interrupting by
  moving costs nothing. Asserted.
- Stale/absent input zeroes movement rather than sliding.

---

## 4. Event bus

`GameEvents` (`Core.Runtime`) is a static event hub and the **only** channel from
gameplay to presentation.

| Event | Raised by | Consumed by |
|---|---|---|
| `Notification` | gameplay feedback | HUD toast |
| `Chat` | quest announcements | HUD log |
| `Prompt` | `PlayerCombat` proximity poll | HUD "[E] …" line |
| `Dialog` | `PlayerCombat.SendDialog` | HUD dialogue window |
| `DialogueChoice` | HUD button click | `PlayerCombat.ChooseAction` |
| `EnemyKilled` | `Enemy` death | `QuestManager` |
| `Damage` | `Enemy` / `PlayerStats` | HUD floating combat text |
| `InputBlocked` | HUD (dialog/chat/journal open) | `PlayerController`, `PlayerCombat`, `CameraFollow` |

Per-character presentation subscribes to gameplay events directly rather than through
the bus: `CharacterVisual` listens to `PlayerCombat.AbilityPerformed` and
`Enemy.Attacked`. Gameplay still never calls animation code — the dependency points
the right way.

`GameEvents` resets all handlers on `SubsystemRegistration`, so domain reloads and
repeated PlayMode runs start clean. Any new static state must do the same.

---

## 5. State machines

`HierarchicalStateMachine<T>` (`Core.Runtime`) — transitions exit up to the common
ancestor, then enter down to the leaf. Used by:

- **`PlayerCombat`**: `Alive → { Mobile → { Idle, Moving }, Casting, Stunned }`,
  mirrored into `PlayerStats.State`.
- **`Enemy`**: `Alive → { Passive → Wander, Engaged → { Aggro, Chase, Combat }, Leash }`,
  mirrored into `Enemy.State`.

The mirrored enum is what the HUD and `CharacterVisual` read. Add states to the
machine *and* the enum together, or the two drift.

---

## 6. Presentation layer

| Script | Does |
|---|---|
| `RpgHud` / `RpgHud.Classic` | The entire UI, built in code: player frame, quest tracker, target frame, cast bar, action bar with cooldown sweeps, journal, dialogue, floating combat text |
| `CharacterVisual` | Drives the legacy `Animation` rig from character state — idle, jog, sword idle, attack, cast, death. Reads state; never writes it |
| `OathfireVisual` | Subscribes to `QuestManager.Changed` and scales/lights the brazier flame when the trial is rekindled |
| `TorchFlicker` | Per-torch light variation |
| `ChamberAmbience` | Generates a quiet hearth bed procedurally at runtime — no audio files are shipped |

---

## 7. Generated content

| Output | Generator | Menu |
|---|---|---|
| `Assets/Scenes/World.unity`, `Assets/Prefabs/*.prefab` | `Assets/Editor/SceneBuilder.cs` | Azeroth04 ▸ Build Oathfire Room |
| `Assets/Resources/RPG/*.asset` | `Assets/Editor/ClassicContentBuilder.cs` | (called by SceneBuilder) |
| `Assets/Materials/Classic_*.mat` | `SceneBuilder.ApplyClassicMaterials` | ″ |
| Texture import settings | `Assets/Editor/ClassicTextureImporter.cs` + `ClassicTextureMap.json` | on import |

`ClassicContentBuilder.Asset<T>` **preserves existing assets** — it only creates what
is missing, so changing a number in that file does nothing to an already-committed
`.asset`. Delete the asset to regenerate it. (There is one explicit exception: the
quest briefing is patched in place after creation. Follow that pattern if you need to
migrate authored data.)

---

## 8. Known structural debt

Recorded so nobody rediscovers it. Fixing any of these needs a requirement ID.

| # | Issue | Where |
|---|---|---|
| D-02 | Enemy spawn circle is a literal `(4.8, 0.2, -1.5)` | `EnemySpawner` |
| D-04 | Enemies steer straight at their target with no navigation — they walk into furniture | `Enemy.FixedUpdate` |
| D-05 | Quest logic (stages 0–5) and NPC dialogue text are hardcoded, not data | `QuestManager`, `Interactable` |
| D-08 | Corpses despawn instantly; loot is auto-granted | `Enemy.TakeDamage` |
| D-09 | The brazier bowl and flame are still primitive cylinder + sphere, although `OathfireVisual` now drives a proper lit state | `SceneBuilder.BuildScenery` |
| D-10 | Ability loadout is a fixed 5-slot array; resource type is a runtime toggle with no class identity | `PlayerCombat`, `PlayerStats` |
| D-11 | Progress lasts only for the play session — no persistence | project-wide |
| D-12 | Animation uses the legacy `Animation` component with clip lookup by name string; no blending or transition control | `CharacterVisual` |

*Resolved since the first draft of this document:* D-01 (hardcoded respawn point),
D-03 (spawn scatter), D-06 (no animation), D-07 (no audio) — all closed by the
single-player rework on `main`.
