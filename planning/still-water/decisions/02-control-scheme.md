# Control Scheme

- **Status:** Locked (2026-10-27) (by: me)
- **Type:** grill
- **Affects:** Phase 1 (feel prototype), Phase 4 (netcode: positions must be synced)

## Question
Tap-to-hop between tiles, or free movement with a virtual joystick and a jump button?

## Options considered
| Option | For | Against |
|---|---|---|
| Tap a tile to hop to it | Easy on phones; trivial to sync online | Feels restrictive; little skill; not enough challenge for gamers |
| **Virtual joystick + Jump** | The player controls timing and failure; real platforming skill; closer to the inspiration | Harder to make feel fair on touch; online needs position sync + interpolation |

## Decision
**Floating virtual joystick (left half of the screen) + Jump button (bottom-right)**, free 3D movement, third-person camera. Built on **Unity Starter Assets – Third Person** (free, official, includes on-screen mobile controls and the new Input System).

## Required to make it fair (see [DESIGN.md §3.2](../../../docs/DESIGN.md#32-feel-helpers-draft-required-in-some-form))
Blob shadow under the player · coyote time · jump buffer · partial air control · slightly forgiving platform colliders · variable jump height. All values tunable.

## Consequences
- Phase 1 is dedicated to feel. Don't build Levels 2–5 until Level 1 feels good (also lab guidance).
- Online mode must sync continuous positions → see [Netcode Model](./10-netcode-model.md).
- Adds ~2–3 days vs tap-to-hop.
- Fallback if testers find it frustrating: stronger assists, then an aim nudge toward the nearest platform.
