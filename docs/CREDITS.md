# Credits

> Every asset that is not original must be listed here (and on the in-game Credits screen) **in the same
> commit that adds it**. Check each license: CC0 needs no credit (but credit anyway), CC-BY requires
> credit, and **CC-BY-NC is not allowed if the game may be released commercially later**.
> No characters, music or logos from commercial games, films or brands.

## Team
| Name | Role |
|---|---|
| _TBD_ | |

## Tools
Unity 6 (Unity Personal) · Colyseus (MIT) · Node.js · Docker · Render · Blender · Krita / GIMP · Audacity · jsfxr

## Assets
| Asset | Used for | Author | Source link | License | Modified? |
|---|---|---|---|---|---|
| Starter Assets: First Person + Third Person \| Character Controllers v2.0.2 | Player controller base, touch joysticks | Unity Technologies | Unity Asset Store | Unity Asset Store EULA | yes |
| `Listener_Custom.fbx`, `T_Listener_Skin/Normal/Veins.png` (+ `art/listener/Listener.blend`) | The Listener's body, rig and swim / idle / lunge animations (in use since 2026-11-16) | Original: generated in Blender by `art/listener/build_listener.py` (this project) | — | own work | — |
| Universal Base Characters (Standard): `Superhero_Male_FullBody.fbx`, male normal map | The previous Listener body (eyes/eyebrows hidden, own pale skin, limbs stretched/thinned in code); unused since 2026-11-16 | Quaternius | https://quaternius.itch.io/universal-base-characters | CC0 1.0 | yes |
| Universal Base Characters (Standard): `Superhero_Female_FullBody.fbx`, `Superhero_Male_FullBody.fbx`, light skin base colours, normal maps, `T_Eye_Brown`, `Hair_Long` / `Hair_SimpleParted` (rigged to head bone) + `T_Hair_1/2_BaseColor` (all in `Art/Player`) | The player's two selectable bodies (since 2026-11-16) | Quaternius | https://quaternius.itch.io/universal-base-characters | CC0 1.0 | yes (by `art/player/build_player_bodies.py`, into `PlayerFemale/PlayerMale_FullBody.fbx`: hair merged onto each body; since 2026-11-17 also original torn, soaked clothes made from the body mesh, and new baked textures `T_Player*_Skin` (the light skin made pale, grimy, bruised) and `T_Player*_Clothes(_Normal)`; own materials) |
| Universal Animation Library (Standard): `UAL1_Standard.fbx` | Player idle / jog / sprint / jump / swim / climb animations (Listener swim / idle until 2026-11-16) | Quaternius (with Gonzalo Furnier) | https://quaternius.itch.io/universal-animation-library | CC0 1.0 | no (retargeted) |
| Tiles020 (1K-JPG: Color, NormalGL) | Pool floor, platforms, walls, ceiling | ambientCG (Lennart Demes) | https://ambientcg.com/view?id=Tiles020 | CC0 1.0 | no (tinted per material) |
| Universal Animation Library 2 (Standard): `UAL2_Standard.fbx` | Player animations from this library, if any (Listener rise / lunge until 2026-11-16) | Quaternius (with Gonzalo Furnier) | https://quaternius.itch.io/universal-animation-library-2 | CC0 1.0 | no (retargeted) |
| | | | | | |

## Fonts
| Font | Author | Source | License |
|---|---|---|---|
| | | Google Fonts | SIL Open Font License |

## Audio
| Sound | Author | Source link | License |
|---|---|---|---|
| All music and the Listener's scream (`Resources/Music/*.ogg`: menu, ambience, reveal music box, hunt layer, race, catch / win / lose stingers, `sfx_listener_scream`) | Original: synthesized by `art/music/compose.py` (this project) | — | own work |
| Water Splash 02 (`Resources/Audio/Splash.mp3`, the player falling in) | Universfield | https://pixabay.com/sound-effects/ (id 352021) | Pixabay Content License (free, commercial use, no attribution required; not CC0: not redistributed on its own) |
