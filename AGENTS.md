# AGENTS.md — Project Azeroth04 (Classic 2004 RPG)

## 1. Project Overview
A single-player/LAN 3D action RPG developed in Unity (URP), replicating the core mechanics, UI paradigms, and artistic feel of 2004 *World of Warcraft* (Vanilla / Patch 1.12). 

Key gameplay pillars include:
- Tab-targeting camera & combat system.
- Global Cooldown (GCD), resource management (Mana, Rage generated on swing/damage, Energy ticking).
- Classic quest log workflow (NPC interaction radius, dialogue typing, kill/collect tracking, turn-in rewards).
- Third-person character controller with mouse-look steering (Right-Click steer, Left-Click orbit, jump momentum).

---

## 2. Art & Asset Constraints (Strict)
Do not suggest, import, or generate assets that violate the 2004 aesthetic:
- **Materials/Shading:** Must strictly use Hand-Painted Diffuse or URP Simple Lit/Unlit shaders. Do not configure Metallic, Smoothness, Normal maps, or Subsurface Scattering.
- **Texture Resolutions:** Keep textures between 256x256 and 1024x1024. Textures must have painted ambient occlusion and lighting baked in.
- **Mesh Topology:** Low-to-mid poly with exaggerated proportions (chunky geometric silhouettes). Never use flat-polygon faceted low-poly styles (e.g., standard low-poly kits without textures).
- **UI Styling:** Classic fantasy skeuomorphism (ornate stone/parchment borders, gold/silver coin indicators, yellow `!` and `?` over NPC heads, floating combat text with arcade numbers).

---

## 3. Architecture & Code Conventions

### Engine & Language Standards
- **Engine:** Unity 2023.2+ (Universal Render Pipeline).
- **Language:** C# (.NET 8 / C# 9+).
- **Assemblies:** Code must reside in modular Assembly Definitions (`Core.Runtime`, `Combat.Runtime`, `Quest.Runtime`, `UI.Runtime`).

### Core Design Patterns
- **Data-Driven Definitions (ScriptableObjects):**
  - Spells, Abilities, Quests, Items, and Loot Tables must be defined via `ScriptableObject`.
  - Do not hardcode spell stats or quest conditions in MonoBehaviour scripts.
- **Event-Driven UI:**
  - Game logic must never directly reference UI components. Use C# Actions or GameEvents (`OnHealthChanged`, `OnQuestProgressUpdated`, `OnCastStarted`).
- **State Machines for Combat & AI:**
  - Use explicit Hierarchical Finite State Machines (HFSM) for character combat states (`Idle`, `Moving`, `Casting`, `Stunned`) and NPC AI (`Wander`, `Aggro`, `Chase`, `Combat`, `Leash`).
- **Tick & Update Efficiency:**
  - Minimize work in `Update()`. Use timers or coroutines for periodic polling (e.g., health regeneration every 2 seconds, energy ticks).
  - Cache all component references (`GetComponent<T>()` in `Awake()`, never in `Update()`).

### Code Example: Spell / Ability Structure
```csharp
// CORRECT: ScriptableObject for definition, component executes logic
[CreateAssetMenu(menuName = "RPG/Spell Data")]
public class SpellData : ScriptableObject
{
    public string spellName;
    public float castTime;
    public float cooldown;
    public float range;
    public int resourceCost;
    public ResourceType resourceType;
}

public class CastController : MonoBehaviour
{
    public event Action<SpellData, float> OnCastStart;
    public event Action OnCastInterrupted;
    // Implementation driven by State Machine
}