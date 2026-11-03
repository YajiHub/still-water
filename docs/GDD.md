# Still Water *(working title)*
Team: _TBD_ Platform: Android Orientation: landscape

> One-page Game Design Document in the format the lab requires (Phase 1, Step 1.2).
> **Draft for Checkpoint 1.** This is a summary: full details live in [DESIGN.md](DESIGN.md).
> Before submitting, re-check that this page still matches DESIGN.md.

## 1. Pitch
Memorize the safe path, jump across a flooded nightmare pool, and never make a splash, because something in the water is listening.

## 2. Genre and target players
3D horror platformer + memory challenge. Casual-to-mid-core mobile players aged roughly 13+ who enjoy short horror and skill-based platformers. Atmospheric scares, no gore.

## 3. Core mechanic and touch controls
Watch the safe platforms glow in sequence, then cross the water by **running and jumping** between them. Left thumb: floating virtual joystick (move). Right thumb: Jump button. Third-person camera. Fake platforms look the same but sink. Falling in causes a splash that attracts the monster, and you must climb back out before it reaches you.

## 4. Goal, win condition, lose condition
- **Goal:** reach the Exit door on the far side of the pool.
- **Win:** touch the Exit door.
- **Lose:** caught by the monster while in the water, or the level timer runs out.

## 5. Level plan
| Level | Goal | What is new or harder | Time / limit |
|---|---|---|---|
| 1 | Remember the glowing path and reach the door | Basics: wide platforms, short gaps, 5-tile path, hints | 120 s |
| 2 | Cross without splashing for 3 stars | 7-tile path, wider gaps, faster monster | 120 s |
| 3 | Time your jumps on drifting platforms | **New:** moving platforms | 120 s |
| 4 | Cross while the lights fail | **New:** fog, flickering lights, platforms that sink if you wait; pattern shown once | 100 s |
| 5 | Escape the Drain | Everything combined, smaller platforms, fastest monster, 10-tile path | 120 s |

## 6. Unlocking rule
Clearing level *n* unlocks level *n+1*. Progress is saved on the phone immediately. Stars: ★ clear · ★★ ≤ 1 splash · ★★★ no splashes and under par time.

## 7. Offline mode
Solo campaign (levels 1–5) playable in airplane mode. Planned extra: **vs computer** "Ghost Swimmer" rival that races you on the same level and gets better each level.

## 8. Online mode
Real-time **1v1 race** on two phones. Both get the same level and the same safe path. First to the Exit wins. Data sent: player positions (~15–20×/s), plus events (fell, climbed, reached exit). A Colyseus game server holds the path, validates moves, runs the monster, and **decides the winner**. Being caught online costs a time penalty instead of a loss. Online results do not affect offline unlocks. If the server is unreachable or a player disconnects, a clear message appears and the game returns to the menu.

## 9. Screens
Title, Main Menu, Settings, Credits, Mode Select, Level Select, Level (HUD), Pause, Result, Lobby, Online Match, Online Result, Connection Error.

## 10. Art and sound style + asset sources
Liminal "poolrooms" horror: pale tiled halls, still green water, harsh light, fog, film grain. Low-to-mid poly, not HD. Sound: quiet room tone, a loud reverberant splash, heartbeat as the monster nears. Sources: own models, Kenney (CC0), Poly Pizza / Sketchfab (CC0 / CC-BY), Mixamo animations, Freesound (per-asset license), jsfxr, Google Fonts (OFL). All listed in [CREDITS.md](CREDITS.md).

## 11. Risks and simpler fallbacks
| Risk | Fallback |
|---|---|
| Jumping feels unfair on a phone | Bigger platforms, stronger coyote time and edge forgiveness. Last resort: an auto-aim nudge toward the nearest platform. |
| Online real-time sync too hard in time | Send fewer updates and only show the opponent's progress (platform index) instead of a smooth 3D ghost |
| Monster AI or animation too much work | Monster shown as a surfacing silhouette + ripple wake (no full body animation) |
| Water shader too heavy for low-end phones | Plain animated texture water, fewer post-processing effects |
| Cloud server sleeping during the demo | Run the server on a laptop on the same Wi-Fi (also covers the lab's same-Wi-Fi Option A) |
