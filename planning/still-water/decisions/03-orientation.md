# Orientation

- **Status:** Locked (2026-11-02) (by: me)
- **Type:** grill
- **Blocks:** Phase 0 (project settings, UI layout, reference resolution)

## Question
Landscape or portrait? The lab requires one fixed orientation (R9).

## Why it matters
It decides the touch-control layout, the camera framing and every UI screen. Changing it later means redoing all UI.

## Options
| Option | For | Against |
|---|---|---|
| **Landscape** | Two thumbs (joystick + jump) sit naturally; wider view of the pool for judging jumps; standard for 3D action on mobile | Can't play one-handed |
| Portrait | One-handed play; matches the vertical inspiration video | Thumbs crowd the bottom of the screen; narrow view makes depth and jumps harder to judge |

## Recommendation
**Landscape** (reference resolution 1920×1080, UI scales with screen size, safe area respected for notches).

## Decision
**Landscape**, auto-rotating between Landscape Left and Landscape Right only (never portrait).

- Player Settings → Resolution and Presentation: Default Orientation = **Auto Rotation**; allowed: **Landscape Right** + **Landscape Left** only (Portrait and Portrait Upside Down unticked).
- UI: Canvas Scaler = Scale With Screen Size, reference **1920×1080**, match ~0.5; all HUD inside the device **safe area** (notches/cutouts on either side, because the phone can be flipped).
- Layout: floating joystick on the left half, Jump bottom-right, pause top-left; level name, timer and splash count along the top.

## Why
Two-thumb controls and a wide view of the pool for judging jumps. Allowing both landscape directions costs no extra UI work and stays within R9's "one fixed orientation" (landscape). If the instructor reads R9 strictly, switch Default Orientation to Landscape Left: a one-setting change.
