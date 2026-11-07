# Listener Asset Source

- **Status:** Locked (2026-11-06) (by: me): **A. Quaternius CC0**
- **Type:** research
- **Blocks:** Phase 3b Listener work ([Visual Pass](./19-visual-pass.md)); monster look per [Title and Monster Identity](./06-title-and-monster.md)

## Question
Where do the Listener's humanoid body and its swim / crawl / rise animations come from, at ₱0, licensed for a public repo and a possible commercial release?

## Findings (read from the sources 2026-11-06; web search tool was down, pages fetched directly)
| Source | What | License | Notes |
|---|---|---|---|
| Quaternius **Universal Base Characters** (quaternius.com/packs/universalbasecharacters.html, quaternius.itch.io/universal-base-characters) | 6 humanoid bodies (Superhero / Regular / Teen, M/F), avg **~13k tris**, FBX/OBJ/glTF; humanoid rig "compatible with retargeting in any engine" | **CC0** ("free to use in personal, educational and commercial projects") | Free version = exported models; Source (paid) adds .blend + eye/skin shaders, not needed |
| Quaternius **Universal Animation Library** (…/universal-animation-library) | 120+ anims on the same universal humanoid rig: "jog, sprint, push, **crawling, swimming**, sitting, death…"; root-motion and no-root-motion versions; v3.0 fixed Unity scale warnings | **CC0** | "Compatible with other common rigs (Mixamo)" |
| Quaternius **Universal Animation Library 2** (…/universal-animation-library-2) | 130+ anims incl. **zombie locomotion**, parkour | **CC0** | |
| Adobe **Mixamo** (helpx.adobe.com/creative-cloud/faq/mixamo-faq.html) | Huge library, auto-rigger | "royalty free… create video games"; **not CC0** | Publishing raw FBX in a public repo not covered by the FAQ; Adobe account needed |
| Own Blender model | — | own | Days of learning; animations still needed |

## Decision
**Quaternius CC0**: one Universal Base Character body + Animation Library 1 (swim, crawl, death) + Library 2 (zombie locomotion for an unnatural rise / lunge). Credit anyway in [CREDITS.md](../../../docs/CREDITS.md) in the same commit. Mixamo not used (license fit for a public repo unclear).

## How it becomes the Listener
- Own pale, wet-looking skin material (URP Lit, low smoothness variation); hair meshes off; eyes covered by skin (no eyes).
- **Too-long arms:** stretch forearm/hand bones in `LateUpdate` after the Animator (scale on bones the humanoid retarget doesn't animate), so the FBX stays untouched.
- Swim face-down for roaming / glimpses (only spine + shoulder blades above the water line); rise + arms-out pose for the catch.
- Driven by the existing `ListenerAI` states (no gameplay change); a prefab replaces the primitive body, wired by an Editor menu like the other builders.

## Risks → first build task is a spike (~1 h)
1. ~~Eyes separate?~~ Yes: own `Eyes` object (checked in the download).
2. Bone stretch survives the humanoid Animator each frame.
3. Phone fps with one 13k-tri skinned mesh + 35 lamps (compare to the L3/L4 numbers).
If 1 or 2 fails: Mixamo auto-rigger on the same CC0 body, or a short Blender edit, revisited then.

## Download checked (2026-11-06, `C:\Users\LEGION2\Downloads\quaternius`, read-only)
- **Free (Standard) = a subset:** bodies are only **Superhero Male / Female** (Regular/Teen are in the paid Source); each animation library has **43 clips**, not 120+/130+. All files CC0 (`License*.txt`).
- **Body** `Superhero_Male_FullBody.fbx` (0.8 MB): body mesh **12,566 tris** + separate **`Eyes`** (MI_Eyes, 768 tris) and **`Eyebrows`** (984 tris) objects → **eyeless = switch those two renderers off** (risk 1 resolved). 65 bones, UE-mannequin names (`spine_01..03`, `upperarm_l`, `lowerarm_l`, `hand_l`, …) → forearm stretch target is `lowerarm_*`/`hand_*`. Superhero build is muscular: make it gaunt by thinning limb/spine bones (scale x/z) in the same `LateUpdate` as the stretch.
- **Clips for the Listener:** `Swim_Fwd_Loop` (roam / hunt, face-down), `Swim_Idle_Loop` (lingering), `LayToIdle` (surfacing / rise for the catch, sped up), `Zombie_Scratch` (catch lunge), `Zombie_Idle_Loop` (arms-out hold), `Death01` (spare). **No crawl** in the free UAL1.
- **Sizes:** `UAL1_Standard.fbx` 23 MB, `UAL2_Standard.fbx` 24 MB (the `_RM` root-motion twins are not needed: `ListenerAI` moves it in code); textures Normal 3.6 MB + Roughness 1.9 MB + Light base colour 1.4 MB. ~**54 MB** into git, plain (each file < 100 MB GitHub limit, no LFS) (by: delegated).
- Body choice **Male** (taller, broader frame reads better stretched) (by: delegated; Female is a drop-in swap, same rig).

## Human steps
Download (free, "just give me the download") the three packs from itch.io into a folder **outside** `client/Assets` first; the agent picks the needed FBX files to import (no `.meta` edits; Unity creates them).
