# Unity MMORPG Template

> **Working on this project?** Read **[AGENTS.md](AGENTS.md)** first — it is the
> contract every agent (Claude Code, Codex) and human follows here. The
> specifications live in **[docs/](docs/README.md)**; the live state of play is
> **[docs/05-STATUS.md](docs/05-STATUS.md)**.
>
> This README describes the original networking scaffold. Parts of the "one-time
> setup" below are now automated by *MMORPG Template ▸ Build Playable Scenes*
> (`Assets/Editor/SceneBuilder.cs`) — see
> [docs/01-ARCHITECTURE.md](docs/01-ARCHITECTURE.md) §6.

A fresh Unity project scaffold using **Netcode for GameObjects (NGO)** — Unity's
official multiplayer solution — wired the same way as this repo's Godot MMORPG
template: an **authoritative server**, replicated player transforms, RPC-driven
input and chat, and a login screen that can host, join, or run headless.

## What's here vs. what you still need to do

Unity projects can't be fully hand-authored as text the way Godot's `.tscn`
files can — scenes and prefabs are Editor-managed binary/YAML with GUID
cross-references. So this scaffold gives you:

- ✅ A valid project skeleton (`ProjectVersion.txt`, `Packages/manifest.json`
  pre-declaring Netcode for GameObjects, Unity Transport, Input System, TextMeshPro)
  that Unity Hub can open directly.
- ✅ Complete, compile-ready **C# scripts** implementing the full networking
  model (see below).
- ⬜ **You wire the scene** in the Editor — a 10-minute, one-time step listed
  below, because that part genuinely requires the GUI (dragging a prefab
  reference onto the NetworkManager, etc.).

## One-time setup

1. **Install a matching Editor.** Open **Unity Hub** (already installed at
   `/Applications/Unity Hub.app`), sign in (or create a free Personal account —
   this step needs to be done by you), then *Installs ▸ Install Editor* and
   pick a **6000.0 LTS** version (the project targets `6000.0.23f1`; Hub will
   offer to install/match that automatically when you open the project).
2. **Open the project**: Hub ▸ *Open* ▸ select `/Users/greg/Claude/unity-mmorpg-template`.
   Unity will import and fetch the packages in `Packages/manifest.json`
   automatically (needs network access, first import takes a few minutes).
3. **Build the two scenes** (File ▸ New Scene, save into `Assets/Scenes/`):

   **`Bootstrap.unity`** (set as the first Build Settings scene):
   - Add a `NetworkManager` GameObject: *GameObject ▸ Create Empty* → name it
     `NetworkManager` → *Add Component ▸ Netcode ▸ NetworkManager* → *Add
     Component ▸ Unity Transport*.
   - Add a Canvas with: Name (InputField), Address (InputField), Port
     (InputField), Host/Join/Dedicated buttons, a Status (Text).
   - Add an empty GameObject with `NetworkBootstrap.cs`, drag the UI fields
     into its Inspector slots.

   **`World.unity`** (add to Build Settings after Bootstrap):
   - Add `GameManager` empty GameObject with `PlayerSpawner.cs`.
   - Add a chat Canvas (InputField + scrolling Text) with `ChatManager.cs`,
     field references wired the same way; hook the InputField's *On End Edit*
     event to `ChatManager.OnChatSubmitted`.
   - Add a simple ground `Plane` for players to stand on.

4. **Build the Player prefab** (`Assets/Prefabs/Player.prefab`):
   - A capsule or character model + `CharacterController`.
   - `Add Component ▸ Netcode ▸ NetworkObject`.
   - `Add Component ▸ Netcode ▸ Netcode Components ▸ Network Transform` — set
     **Authority Mode = Server**.
   - `Add Component ▸ PlayerController` (this repo's script).
   - Drag the finished prefab into the `NetworkManager` component's
     **Player Prefab** slot back in `Bootstrap.unity`.

That's the entire one-time wiring; after that everything is driven by the
scripts below and by Unity's normal Play-mode / Build workflow.

## Scripts

```
Assets/Scripts/
  Network/
    NetworkBootstrap.cs   Login screen logic — Host / Join / Dedicated Server,
                           name/address/port fields, headless -server launch flag.
    PlayerSpawner.cs       Server-only: scatters each connecting player's spawn
                           point so avatars don't stack on connect.
  Player/
    PlayerController.cs    Server-authoritative movement. Clients send an input
                           vector via ServerRpc; only the server moves the
                           CharacterController. NetworkTransform replicates the
                           result to everyone.
    CameraFollow.cs         Simple third-person follow rig for the local player.
  UI/
    ChatManager.cs          Client -> ServerRpc -> ClientRpc chat relay, plus
                           local system messages (join/leave, connection status).
```

## Running it

- **In-editor, one machine, two roles:** *Window ▸ Multiplayer Play Mode... ▸
  Add Virtual Player*, then press Play. One instance can Host, the other Joins
  `127.0.0.1`.
- **Headless dedicated server (after building):**

  ```bash
  ./Build/UnityMmoTemplate.app/Contents/MacOS/UnityMmoTemplate -batchmode -nographic -server -port 24565
  ```

  (`Application.isBatchMode` + the `-server` flag route straight into
  `NetworkBootstrap.StartDedicatedServer` without ever showing the login UI —
  build the project first via *File ▸ Build Settings ▸ Build*.)

## Where to build next

- Persistence: hook character save/load into `PlayerSpawner`'s connect callback.
- Authentication: validate a token/session before `NetworkManager.Singleton.StartClient()`
  is allowed to complete (NGO's `ConnectionApprovalCallback`).
- Interest management: NGO replicates to everyone by default — for real player
  counts, look at `NetworkObject.CheckObjectVisibility` or a third-party spatial
  interest-management package once you have more than a handful of concurrent players.
- Combat/NPCs/items: add server-authoritative systems the same way as movement —
  server computes, `NetworkVariable`s or `ClientRpc`s replicate the result.
