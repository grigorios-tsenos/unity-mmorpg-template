# Character assets

The chamber also uses **KayKit Dungeon Remastered** models by Kay Lousberg,
CC0. See `guildhall/LICENSE.txt`, `guildhall/source.json`, and
https://github.com/KayKit-Game-Assets/KayKit-Dungeon-Remastered-1.0.

Models, clothing, skin/eye/hair textures, and humanoid animations by **Quaternius**.
Downloaded September 5, 2026 from the author's official itch.io pages:

- [Modular Character Outfits — Fantasy, Standard](https://quaternius.itch.io/modular-character-outfits-fantasy)
- [Universal Base Characters, Standard](https://quaternius.itch.io/universal-base-characters)
- [Universal Animation Library, Standard](https://quaternius.itch.io/universal-animation-library)

All three archives specify **CC0 1.0 Universal / Public Domain Dedication**.
License: https://creativecommons.org/publicdomain/zero/1.0/
Creator: https://quaternius.com/ · https://www.patreon.com/quaternius

## Adaptations for Emberveil

`tools/build_characters.mjs` assembles compatible outfit, head, eye, eyebrow,
and hair meshes onto the shared humanoid rig. Covered body geometry is omitted
to prevent clipping. Hair is weighted to the head; relevant in-place animation
clips are included by bone name. Materials use color textures with matte
roughness; normal and metal maps are omitted for a simpler painted appearance.
The wayfarer mesh is reused with a color variation for hostile guardians.

The outdoor environment, swords, terrain shader, UI illustrations and gameplay
are original project code. The chamber combines KayKit models with original
layout, lighting and encounter logic. No World of Warcraft models, textures, names, interface
files, or extracted game data are included. The requested early-2000s fantasy
MMO direction is expressed through original gameplay and these CC0 assets.

GLB imports extract adjacent PNG files; keep them with the models when sharing
the project. Source archive downloads are not needed to run the game.
