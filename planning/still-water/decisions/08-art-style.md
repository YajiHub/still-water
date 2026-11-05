# Art Style

- **Status:** Locked (2026-11-04) (by: me): **C. Hybrid**, after the Phase 1 phone spike
- **Type:** prototype
- **Blocks:** Phase 1 end (before making real assets)

## Question
Clean liminal look or PSX/retro-horror look?

## Why it matters
It decides texture resolution, which shaders, which post-processing, and which free assets fit. Mixing styles looks amateur.

## Options
| Option | Look | Cost |
|---|---|---|
| A. **Clean liminal** | Smooth pale tiles, soft bloom, haze, realistic-ish water (like poolrooms videos) | Needs good lighting and baked GI; water shader matters a lot |
| B. **PSX-style** | Low-res textures, vertex wobble, dithering, heavy fog | Cheap assets; very forgiving; strong "indie horror" identity; a slightly niche look |
| C. Hybrid | Clean geometry + film grain/fog/low-res render scale | A good middle ground |

## Prototype plan (~1 day, inside Phase 1)
One small grey-box room: tiled floor and walls, a water plane, 3 lights, fog. Make two versions (A and C, and B if time allows) with URP Volume profiles. Screenshot each on the phone, check fps.

## Recommendation
Probably **C (hybrid)**: the easiest to make scary at low cost, and it runs well on mid-range phones.

## Decision
**C. Hybrid horror:** clean grey-box geometry, dark teal exponential fog, cold dim ceiling lights, ACES tonemapping, bloom on the edge glow, vignette, film grain, render scale 0.6.

**Evidence (Phase 1 phone spike, 2026-11-04, mid-range Android):** A and C built as `LookProfile` assets and switched live in one build; both 55–60 fps while moving. C read as scary; A as too bright and safe. First C values were too dark (only the glow strips visible), so C was brightened (ambient 0.05→0.13, light 1.5→3 / range 14→20, fog 0.07→0.045, exposure −0.2→+0.3). Screenshots: `docs/screenshots/clean-minimal.jpg` (A), `docs/screenshots/hybrid-horror.jpg` (C, before brightening). B (PSX) not built: C covers its mood at lower asset cost.

## Consequences
- Values live in `Assets/_Project/Art/Looks/Look_C.asset` + `Volume_C.asset`; tune there, not in code. Look A stays only as the spike's comparison until Phase 5, then the Look toggle is removed.
- Assets must suit low light + fog: mid-poly, simple textures; the edge glow stays the main readability cue (also L4 "Lights Out").
- Brightness on different phones varies: a **Brightness/Gamma** slider in Settings (Phase 5).
- Water shader, caustics and splash particles (DESIGN §10) are built for this look in Phase 5.
