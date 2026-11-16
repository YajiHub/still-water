# 2026-11-06/07 — Phase 3b Visual Pass (planned and built)

## Focus
The user didn't want to keep making levels on grey-box visuals ("especially the Listener"). Planned Phase 3b (Visual Pass) before Level 5 and built its code: a real Listener, moving water with splash/ripples/foam, a pool tile hall with lamp fixtures.

## What Was Built / Changed
- **Plan:** decisions [Visual Pass](../../planning/still-water/decisions/19-visual-pass.md) (Listener + water + hall; catch moment → Phase 5), [Title and Monster Identity](../../planning/still-water/decisions/06-title-and-monster.md) monster half (pale, long-limbed, eyeless humanoid swimming face-down), [Listener Asset Source](../../planning/still-water/decisions/20-listener-asset-source.md) (Quaternius CC0), [Water and Hall Look](../../planning/still-water/decisions/21-water-and-hall-look.md); Phase 3b roadmap in MAP.
- **Listener:** Quaternius `Superhero_Male_FullBody` + UAL1/UAL2 in `Art/Listener` (Humanoid via `ListenerImport`), `Listener.prefab` + controller from *Art Spike → Build Listener Prefab* (eyes/eyebrows removed, pale wet skin). `ListenerBody`: 1.35× size, broad back (torso 1.3), thin legs, forearms 1.8×, long neck, head tilt, face mask (sliders). `ListenerLook` (pure, 8 tests): swim / tread / lunge + playback speed. `ListenerAI` plays it and slides the body so the chest sits on the water line when the back shows. *Set Up Level Loop* uses the prefab (user re-ran it on Levels 1–4).
- **Water:** URP Lit `Water.mat` with a baked tileable wave normal (main + detail, drifting in `PoolWater`), glossy, environment reflections off (*Art Spike → Set Up Water*).
- **WaterFx** (added by `PoolWater` at runtime, no rebuild): splash drops + rings on falling in, ripples behind a swimmer and in the Listener's Wake mode (old discs removed), foam rings at platform bases. Materials in `Resources/WaterFx` (copies of URP `ParticlesUnlit`).
- **Splash sound:** the user's Pixabay recording (Universfield, `Resources/Audio/Splash.mp3`), synth plop + wash as fallback.
- **Hall:** ambientCG Tiles020 (CC0) on `Tile.mat` / `Wall.mat`; `HallDressing` (added by `PoolWater`) box-projects UVs at 1.5 m per repeat and hangs a glowing shade + cable on each hall light (*Art Spike → Set Up Hall*). Crumbling / one-way / ferry edge emission 0.7 → 0.2.
- Every level starts in **Look C** (dark). Headlamp cone edge softened.
- CREDITS: Quaternius ×3, ambientCG Tiles020, Pixabay splash.

## Verification
- `npm run lint` exit 0 · `npm run typecheck` exit 0 · `npm test` **115/115** (server unchanged).
- Unity EditMode: last run **88/88 (11-06)**, **not re-run on this phase** (expect 96).
- In the Editor (user): the Listener swims face-down and is visible at the surface; water moves; splash visible and the recording plays; tiles/lamps/glow/arc fix just applied, **not yet confirmed**.
- Phone: baseline before the phase recorded (lowest fps L3 A ~50 / C ~58, L4 A ~40 / C ~50); **after not measured**.
- Ownership check skipped.

## Mistakes & Gotchas
- **Deleting and recreating the AnimatorController broke the Animator** (`controller=none`, "Animator Controller … is not valid"): an unsaved controller is read back layer-less, and any Animator pointing at the old asset loses it. Reuse the asset, clear its states, save.
- FBX clip names are `Armature|Swim_Fwd_Loop`; matching on the bare name found nothing.
- Two Editor-only snapshot attempts (AnimationMode, PlayableGraph in a preview scene) never posed the humanoid; Play mode did. Deleted the tool; Play + screenshots instead.
- `ListenerBody` only applies in Play: an Edit-mode screenshot still shows the plain human.
- New body sat at the old capsule's depth: only the raised head broke the surface → align the chest bone to the water line every frame.
- The water "arc" was first blamed on the headlamp; the real cause was the water reflecting Unity's default sky horizon (indoors) → environment reflections off.
- Synthesized splash: white noise = steam; added bubbles = fake. A real recording won.
- Thinning bones whose children bend (knees, shoulders) shears the mesh a little (noted in code).
- Inspector `SerializedObjectNotCreatableException` / `m_Targets` errors after reopening Unity: the Inspector showing a destroyed object; harmless.
- The user had to search for the prefab: always give full `Assets/...` paths in Editor steps.

## Decisions
- All Phase 3b visuals hook in through `PoolWater` (one per level) or shared materials, so levels never need rebuilding for looks (by: delegated).
- Water = URP Lit + baked normals, not a hand-coded shader (by: delegated): lamp glints from Forward+ for free, nothing to fail compiling on the phone.
- Pixabay splash recording (by: me): free commercial, no credit required, **not CC0** (no standalone redistribution); credited; swap for CC0 if the license matters later.
- Listener: bigger and broader than the gaunt plan (by: me); further enhancement deferred ("enhance the listener later"). Splash visual felt artificial → tune later.
- Edge glow: soft tint, no bloom (by: me).
- Water arc still visible after the reflection fix: **deferred** (by: me); priority = finish 5 levels.

## Next Step
In Unity: confirm the water arc is gone and tiles/lamps/edges look right after *Set Up Hall* + *Set Up Water*; run **Test Runner → EditMode → Run All** (expect 96/96); **Build And Run** and record lowest fps on L3/L4 (bar ≥ 30); 2 screenshots to `docs/screenshots/`. Then commit Phase 3b on `feat/visual-pass-3b` via PR, and `/skill:wayfinder continue planning/still-water/MAP.md` → plan Level 5.
