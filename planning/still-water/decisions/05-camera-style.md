# Camera Style

- **Status:** Open
- **Type:** prototype
- **Blocks:** Phase 1

## Question
How does the third-person camera behave during jumps?

## Why it matters
In 3D platforming, most "unfair" deaths come from the camera hiding depth. On a phone the right thumb is busy with Jump, so camera rotation can't depend on constant thumb-dragging.

## Options
| Option | For | Against |
|---|---|---|
| **A. Auto-follow behind the player, slightly elevated, with optional drag-to-look on the right half** | Familiar; works with Starter Assets + Cinemachine | Auto-rotation can swing at bad moments |
| B. Fixed-angle chase camera (always looks "forward" toward the exit, high angle) | Very readable depth; no camera management | Less freedom; the level must flow in one direction |
| C. First-person | Scariest; immersive | Hardest to judge landings; no blob shadow visible |

## Prototype plan (Phase 1, ~2 hours)
Build A and B in the grey-box Level 1 and play each on the phone for 5 minutes. Measure: how many missed jumps feel like *my* fault vs the camera's.

## Recommendation
Start with **B** (levels are one-directional anyway), allowing a little drag-to-look. Fall back to A if B feels too on-rails.

## Decision
_(fill in after the prototype)_
