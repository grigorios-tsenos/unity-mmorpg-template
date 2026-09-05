# 01 · Architecture

Ground truth for how the code is laid out today. If you change the shape of the
system, update this file in the same commit.

---

## 1. Assemblies

| Assembly | Path | May reference |
|---|---|---|
| `Core.Runtime` | `Assets/Scripts/Core` | Netcode, Unity |
| `Combat.Runtime` | `Assets/Scripts/Combat` | Core |
| `Quest.Runtime` | `Assets/Scripts/Quest` | Core |
| `UI.Runtime` | `Assets/Scripts/UI` | Core, Quest, Combat |
| `Azeroth.Editor.Tests` | `Assets/Tests/Editor` | all runtime |
| `Azeroth.PlayMode.Tests` | `Assets/Tests/PlayMode` | all runtime |

**The arrow never reverses.** Gameplay assemblies must not reference `UI.Runtime`.

Namespaces do not map 1:1 to folders — `PlayerController` and `CameraFollow` live in
`Assets/Scripts/Core` but use `MmoTemplate.Player`; `PlayerSpawner` uses
`MmoTemplate.Network`. Most gameplay is `MmoTemplate.Rpg`. Don't "fix" this without
a decision entry; the asmdef boundaries are what matter.

---

## 2. Runtime map

```
Bootstrap.unity                     World.unity
├── NetworkManager (UTP)            ├── GameManager  [NetworkObject]
│     PlayerPrefab = Player         │     ├── PlayerSpawner   (server: scatter spawns)
│     Prefabs      = Guardian       │     ├── EnemySpawner    (server: spawn guardians/boss)
├── BootstrapCanvas                 │     ├── QuestManager    (server: trial state machine)
└── Bootstrap (NetworkBootstrap)    │     └── ChatRelay       (relay client→server→all)
                                    ├── RpgHud                (builds all UI in code)
                                    ├── Main Camera + CameraFollow
                                    └── Chamber               (all scenery, NPCs, brazier)
```

Player prefab: `CharacterController` + `NetworkObject` + `NetworkTransform` +
`PlayerController` + `PlayerStats` + `PlayerCombat` + `Visual` (wayfarer.glb).
Guardian prefab: same minus the player scripts, plus `Enemy`.

Both are on **layer 2 (Ignore Raycast)** so line-of-sight checks and camera occlusion
(`layerMask = 1`, i.e. Default only) see world geometry but not characters. Anything
that should block LOS must be on layer 0 with a collider.

---

## 3. Authority model

Server-authoritative, `NetworkTransform` in server authority mode.

| Flow | Path |
|---|---|
| Movement | client `Update` reads input → `SubmitRpc(input, heading, jump)` at 20 Hz → server `FixedUpdate` moves the `CharacterController` → `NetworkTransform` replicates |
| Ability use | client key → `CastRpc(slot)` → server validates (GCD, cooldown, range, LOS, resource, movement) → `CastingSlot`/`CastEnd` NetworkVariables → server `FinishCast` applies damage |
| Interaction | client `E` → `InteractRpc(networkObjectId)` → server range-checks 3.2 m → `IInteraction.Interact` → `ShowDialogRpc` to owner |
| Dialogue choice | client → `ChooseRpc(id)` → server checks the id is in `allowedActions` **and** still in range → `IInteraction.Choose` |
| Quest progress | server only; `Stage`/`Kills`/`Collected` NetworkVariables + `NetworkList` of participants/claimants |

**Anti-cheat invariants already enforced — do not regress them:**
- `allowedActions` is a server-side whitelist consumed once per dialogue; arbitrary
  action IDs cannot grant rewards.
- `Claimed` is appended *before* rewards are granted, so a double turn-in is impossible.
- `SubmitRpc` rejects non-finite floats; input is clamped to unit magnitude server-side.
- Stale input (>0.25 s) zeroes movement, so a dropped client doesn't slide forever.
- `ChangeResourceRpc` refuses during combat, casting, or death.

---

## 4. Event bus

`GameEvents` (`Core.Runtime`) is a static event hub and the **only** channel from
gameplay to presentation.

| Event | Raised by | Consumed by |
|---|---|---|
| `Notification` | server→owner RPCs (`NotifyRpc`, `FeedbackRpc`) | HUD toast |
| `Chat` | `ChatRelay`, quest announcements | HUD chat log |
| `Prompt` | `PlayerCombat` proximity poll | HUD "[E] …" line |
| `Dialog` | `PlayerCombat.ShowDialogRpc` | HUD dialogue window |
| `DialogueChoice` | HUD button click | `PlayerCombat` → server |
| `EnemyKilled` | `Enemy.TakeDamage` on death | `QuestManager` |
| `Damage` | `Enemy` / `PlayerStats` damage RPCs | HUD floating combat text |
| `InputBlocked` | HUD (dialog/chat/journal open) | `PlayerController`, `PlayerCombat`, `CameraFollow` |

`GameEvents` resets all handlers on `SubsystemRegistration`, so domain reloads and
repeated PlayMode tests start clean. Any new static state must do the same.

---

## 5. State machines

`HierarchicalStateMachine<T>` (`Core.Runtime`) — transitions exit up to the common
ancestor, then enter down to the leaf. Used by:

- **`PlayerCombat`**: `Alive → { Mobile → { Idle, Moving }, Casting, Stunned }`
  mirrored into the replicated `PlayerStats.State` enum.
- **`Enemy`**: `Alive → { Passive → Wander, Engaged → { Aggro, Chase, Combat }, Leash }`
  mirrored into the replicated `Enemy.State`.

The replicated enum is what the HUD and (soon) the animation layer read. Add states
to the machine *and* the enum together, or the two drift.

---

## 6. Generated content

| Output | Generator | Menu |
|---|---|---|
| `Assets/Scenes/Bootstrap.unity`, `World.unity` | `Assets/Editor/SceneBuilder.cs` | MMORPG Template ▸ Build Playable Scenes |
| `Assets/Prefabs/Player.prefab`, `Guardian.prefab` | `SceneBuilder.cs` | ″ |
| `Assets/Resources/RPG/*.asset` | `Assets/Editor/ClassicContentBuilder.cs` | (called by SceneBuilder) |
| `Assets/Materials/Classic_*.mat` | `SceneBuilder.ApplyClassicMaterials` | ″ |

`ClassicContentBuilder.Asset<T>` **preserves existing assets** — it only creates what
is missing. To change a number in a shipped asset you must edit the asset (or delete
it and regenerate). Adding a new field to a `ScriptableObject` therefore does *not*
retro-populate old assets; say so in your plan if that matters.

---

## 7. Data definitions

| Type | Fields that matter |
|---|---|
| `SpellData` | `castTime`, `cooldown`, `range`, `resourceCost`, `resourceType`, `power`, `globalCooldown`, `healing`, `autoAttack` |
| `EnemyData` | `health`, `damage`, `speed`, `attackRange`, `aggroRange`, `leashRange`, `attackInterval`, `loot` |
| `ItemData` | `itemId`, `displayName`, `description`, `priceCopper`, `healing`, `questItem` |
| `LootTable` | `entries[] {item, chance, count}`, `copper`, `experience` |
| `QuestData` | `title`, `briefing`, `guardianKills`, `collectCount`, `collectItem`, `rewardItem`, `rewardCopper`, `rewardXp`, `rewardPotions` |

`QuestData` is currently shaped for exactly one quest (kill N + collect N + boss).
Generalising it is a Phase 4 concern — see `docs/02-ROADMAP.md`. Do not generalise it
early; the vertical slice comes first.

---

## 8. Known structural debt

Recorded here so nobody rediscovers it. Fixing any of these needs a requirement ID.

| # | Issue | Where |
|---|---|---|
| D-01 | Respawn point is a literal `(-2, 0.2, 3.5)` | `PlayerStats.Recover` |
| D-02 | Enemy spawn circle is a literal `(4.8, 0.2, -1.5)` | `EnemySpawner.circleCenter` |
| D-03 | `PlayerSpawner` scatters in a ±4 box at y=1, unaware of the room's furniture | `PlayerSpawner.PlacePlayer` |
| D-04 | Enemies steer straight at their target with no navigation — they walk into tables | `Enemy.FixedUpdate` |
| D-05 | Quest logic (stages 0–5) and NPC dialogue text are hardcoded, not data | `QuestManager`, `Interactable` |
| D-06 | No `Animator` on any character; models are static meshes | `SceneBuilder`, prefabs |
| D-07 | No audio of any kind | project-wide |
| D-08 | Corpses despawn instantly; loot is auto-granted to the killer only | `Enemy.TakeDamage` |
| D-09 | Brazier is a primitive cylinder + sphere, not art | `SceneBuilder.BuildScenery` |
| D-10 | Ability loadout is a fixed 5-slot array; resource type is switched by RPC with no class identity | `PlayerCombat.Awake`, `PlayerStats.ChangeResourceRpc` |
