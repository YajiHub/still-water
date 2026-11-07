# Water and Hall Look

- **Status:** Locked (2026-11-06): water (by: me), hall (by: delegated)
- **Type:** prototype / research
- **Blocks:** Phase 3b water + hall tasks ([Visual Pass](./19-visual-pass.md))

## Findings
- Shader Graph 17.3 is installed (URP dependency); Particle System module installed.
- `Mobile_RPAsset`: depth texture **off**, opaque texture off, render scale 0.8 (Look C sets 0.6), HDR on. Turning depth on adds a phone cost; edge foam normally needs it.
- Water is **opaque** by design (the Listener is only seen where it breaks the surface) — keep it.
- **ambientCG** and **Poly Haven** are CC0 (docs.ambientcg.com/license, polyhaven.com/license). ambientCG **Tiles020** = "Blue Cyan Light Pool Tiles" (use the 1K-JPG download, not the 442 MB set).

## Decision
**Water (by: me): own shader, no depth texture.**
- Opaque teal; two drifting wave normal maps; lamp glints (specular); darker / more reflective at grazing angles (fresnel).
- **Changed 2026-11-07 (by: delegated): plain URP Lit** with a baked wave normal (main + detail, drifting in `PoolWater`): Forward+ lamp glints and reflections for free, nothing to fail compiling on the phone. Was: a **hand-coded URP shader** (`.shader` text) rather than a Shader Graph asset (by: delegated): same result, but the agent can write, diff and review it; a Shader Graph would need step-by-step node building in the Editor. The wave normal texture is baked once by an Editor menu from noise (no download).
- **Splash** particles when the player falls in; **expanding ripple rings** for the Listener's wake and a swimming player; **foam rings** around platform bases as a mesh Build Level adds (fakes depth foam).

**Hall (by: delegated, change any time):** ambientCG **Tiles020** on pool floor, walls and platform tops (tiling by world size so tiles stay square); the 35 hall lamps get a **fixture** (housing + emissive panel built from primitives by Build Level — no model download). The edge glow stays the main readability cue.

## Rejected
- Depth-based foam: real contact foam everywhere, but a phone fps cost with 35 lamps.
- Asset Store water: not CC0 (EULA), fights Look C, more to maintain.

## Human steps
Download ambientCG **Tiles020** (1K-JPG) and unzip **outside** the project, e.g. `C:\Users\LEGION2\Downloads\ambientcg\Tiles020`.
