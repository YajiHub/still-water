# Art Style

- **Status:** Open
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
_(fill in after the prototype)_
