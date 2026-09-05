# 03 · Art Bible

The 2004 look is a hard constraint, not a mood board. A change that looks "better"
by modern standards but breaks this document is a regression.

The reference is *World of Warcraft*, Vanilla / patch 1.12 — hand-painted, high
contrast, readable at a distance, running on 2004 hardware.

---

## 1. Materials and shading

**Allowed:** `Universal Render Pipeline/Simple Lit` (the current default, with
specular highlights disabled) and `Universal Render Pipeline/Unlit`.

**Forbidden:** Lit/PBR shaders, metallic, smoothness, normal maps, height maps,
occlusion maps, emission maps, subsurface scattering, clear coat, parallax, screen
space reflections, ambient occlusion post-processing, bloom beyond a subtle threshold,
depth of field, motion blur, and any HDR-dependent look.

Lighting and ambient occlusion are **painted into the diffuse texture**, not computed.
The scene lights exist for mood and localised warmth (torches, the brazier, moonlight),
never as the primary source of form.

`SceneBuilder.ApplyClassicMaterials` already enforces this: every imported glb
material is converted to a cached `Classic_<guid>_<localid>.mat`, keeping only base
colour and base map. `ClassicTextureImporter` (driven by `ClassicTextureMap.json`)
caps extracted diffuse textures at 1024 px on import. Do not bypass either.

## 2. Textures

- 256×256 minimum, 1024×1024 maximum. 512 is the normal case.
- Hand-painted diffuse only. Visible brush direction, warm/cool colour temperature
  shifts instead of grey shadows.
- Strong local contrast: a texture must read at 15 m and at 1.5 m.
- Point or bilinear filtering; no anisotropic beyond 2×.
- No photographic source material, ever.

## 3. Meshes

- Low-to-mid poly with **exaggerated, chunky proportions** — oversized shoulders,
  hands, weapons, and boots. Silhouette first.
- Characters roughly 1,500–4,000 triangles. Props 100–1,500.
- **Not** faceted flat-shaded low-poly. That is a different (and wrong) aesthetic:
  untextured coloured planes read as 2015 indie, not 2004 MMO. Everything is textured.
- Smoothing groups that read as soft-but-simple, not high-poly-baked.

## 4. Colour and lighting

- Saturated, near-primary hues in the environment; grimy neutrals only as a backdrop.
- Directional key light stays dim (`intensity ≈ 0.55`) and cool-blue; the warmth comes
  from point lights on torches and the brazier (`RGB ≈ 1, 0.75, 0.5`).
- Flat ambient, never skybox-based ambient. Current value `RGB 0.30, 0.29, 0.34`.
- Soft shadows on the key light and the moonlight spot only. Point lights cast none.

## 5. UI

Classic fantasy skeuomorphism. The existing `RpgHud` is the reference implementation
and the target — match it rather than inventing a second style.

- Ornate stone and parchment borders; gold and bronze edges (`PanelEdge 0.59, 0.50, 0.34`).
- Parchment `0.89, 0.82, 0.66` for headings; ink `0.91, 0.91, 0.87` for body.
- Dark, near-opaque panel grounds (`0.06, 0.055, 0.05, 0.93`).
- Health bar green `0.42, 0.57, 0.26`; XP bar purple `0.53, 0.45, 0.70`.
- Yellow `!` above quest givers with an available quest, `?` above a turn-in.
  (`QuestMarker`, colour `1, 0.83, 0.1`.)
- Floating combat text: arcade-style numbers rising from the hit point, white for
  damage dealt, red for damage taken, yellow for crits.
- Coin amounts shown as gold/silver/copper, never a raw integer.
- No flat modern UI, no thin sans-serif hairlines, no drop shadows with blur, no
  translucent glassmorphism, no rounded-rectangle "app" chrome.

## 6. Animation

Covered in detail by [specs/mech-03-animation.md](specs/mech-03-animation.md). The
aesthetic rules:

- Snappy, readable, slightly exaggerated. Poses hold; there is no motion-captured
  subtlety.
- 30 fps authored keys are correct and period-accurate. Do not smooth them out.
- Weapon swings have a clear wind-up, a fast strike, and a recovery that lines up
  with the swing timer.
- Idle is a slow breathing loop with an occasional fidget, not a static pose.

## 7. Audio

The room's ambience is **generated procedurally at runtime** by `ChamberAmbience` —
no audio files ship with the project, deliberately. Keep it that way unless a
`DECISIONS.md` entry says otherwise; it avoids a licensing surface for a hobby
project and keeps the repo small.

- Short, dry, punchy combat impacts; nothing reverberant or cinematic.
- 22 kHz mono is correct and period-accurate for effects.
- Every player action should make a sound. Silence reads as a bug.

## 8. Licensing

All third-party art must be CC0 or equivalently unrestricted, with the licence text
committed under `Assets/Art/licenses/` and credited in `Assets/Art/CREDITS.md`.

Current sources: KayKit Dungeon Remastered (props), Quaternius base characters,
fantasy outfits, and animations. Anything new goes through the same paperwork in the
same commit as the asset.
