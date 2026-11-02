# Still Water — Design (living document)

> **Status:** living draft. This is the **source of truth for game design**. Everything here can change.
> Each section is labeled:
> - **Locked:** decided. Change it by reopening its decision file in `planning/still-water/decisions/`.
> - **Draft:** the current direction. Fine to build on, expected to be tuned.
> - **Open:** not decided yet. Do not build it as if it were final. See the MAP's Active Frontier.
>
> Numbers are **starting values for playtesting**, not requirements.

---

## 1. Concept

### 1.1 Pitch (Draft)
*Memorize the safe path, jump across a flooded nightmare pool, and never make a splash, because something in the water is listening.*

### 1.2 Inspiration (reference only, do not copy)
A Roblox poolrooms horror game: safe platforms flash in a pattern ("remember them"), players hop across green water, and falling in draws a crab-like monster. Later stages add moving platforms. **Still Water takes the loop, not the content.** The monster, art, sounds and level layouts are original.

### 1.3 Experience goals (Draft)
1. **Tension through stillness:** the room is quiet, bright and empty, and that is what makes it scary.
2. **Skill, not luck:** if you fall, it's because you missed a jump or forgot the pattern, never because the controls failed you.
3. **Recoverable mistakes:** a splash is a heart-pounding scramble, not instant death.
4. **Short sessions:** a level takes 1–3 minutes on a phone.

### 1.4 Target players (Draft)
Casual-to-mid-core mobile players, roughly 13+, who like short horror and skill platformers (the Roblox horror crowd). Not gory: the horror comes from atmosphere, sound and the chase.

---

## 2. Core loop (Draft)

```
Level start ─► Goal text shown ─► REVEAL: safe platforms glow in sequence (memorize)
     ─► CROSS: joystick + jump across platforms to the exit door
          ├─ land on a safe platform ─► keep going
          ├─ land on a fake platform ─► it sinks after a short delay ─► you fall in
          └─ miss a jump ─► you fall in
     ─► SPLASH: the Listener hears it and swims toward you
          ├─ climb back onto the last safe platform in time ─► keep going (splash counted)
          └─ caught ─► LOSE ─► Result screen (Retry / Menu)
     ─► reach the exit door ─► WIN ─► stars ─► unlock next level ─► Result screen (Next / Retry / Menu)
```

Optional pressure: a level timer. *(Open: does every level have a timer, or only some? See [Core Loop Rules](../planning/still-water/decisions/04-core-loop-rules.md).)*

---

## 3. Controls and movement

### 3.1 Scheme (Locked — [Control Scheme](../planning/still-water/decisions/02-control-scheme.md))
- **Left thumb:** a floating virtual joystick that appears where the thumb touches the left half of the screen.
- **Right thumb:** a **Jump** button (bottom-right). Possibly a small **Look** drag area on the right half later.
- **Camera:** third-person follow camera *(Open: fixed angle vs player-rotatable. See [Camera Style](../planning/still-water/decisions/05-camera-style.md))*.
- **Orientation:** landscape, auto-rotating between Left and Right (Locked — [Orientation](../planning/still-water/decisions/03-orientation.md)).
- **Base:** Unity **Starter Assets – Third Person** (free, official), adapted.

### 3.2 Feel helpers (Draft, required in some form)
These make jumping on a phone feel fair. Players don't notice them, but they notice when they're missing.

| Helper | What it does | Starting value |
|---|---|---|
| Blob shadow | Dark circle under the player that shows where you'll land. **Most important.** | always on |
| Coyote time | You can still jump shortly after walking off an edge | 0.10 s |
| Jump buffer | A jump pressed shortly before landing still fires | 0.10 s |
| Air control | How much you can steer mid-air (0–1) | 0.6 |
| Edge forgiveness | Platform colliders slightly wider than the visual | +0.10 m per side |
| Variable jump height | Releasing Jump early gives a shorter hop | on |

### 3.3 Movement numbers (Draft, tune in playtest)
| Parameter | Start |
|---|---|
| Run speed | 4.5 m/s |
| Jump height | 1.2 m |
| Gravity | −20 m/s² (snappier than real gravity) |
| Platform size | 2 × 2 m (smaller in later levels) |
| Gap between platforms | 1.5–2.5 m (L1) → up to 3.2 m (L5) |
| Swim speed | 2.0 m/s |
| Climb-out | swim to the edge of the last safe platform and press Jump |

All values live in a tunable config (ScriptableObject), never hard-coded.

---

## 4. Platforms and the pattern

### 4.1 Platform types (Draft)
| Type | Behavior | First appears |
|---|---|---|
| **Safe** | Solid. Part of the memorized path. | L1 |
| **Fake** | Looks identical to Safe. Sinks ~0.4 s after you land. | L1 |
| **Moving** | Safe platform that slides back and forth on a fixed path. | L3 |
| **Sinking (timed)** | Safe, but sinks if you stand on it longer than N seconds. | L4 |
| **Start / Exit** | Large solid areas at both ends. The Exit has a door. | L1 |

### 4.2 Pattern reveal (Draft)
- At level start, the player stands on the Start platform. Safe platforms **glow one by one in path order**, with a soft chime that rises in pitch per step. Then all glows fade.
- Pattern length and how long each glow lasts are set per level (see §6).
- **Open:** can the player see the pattern again (e.g. once per level, costing a star)? See [Core Loop Rules](../planning/still-water/decisions/04-core-loop-rules.md).
- **Open:** is the path always one tile wide, or are there branches and decoys next to it? (Branches make memory harder without making the path longer.)

### 4.3 Layout (Draft)
Hand-built layouts per level (not procedural) for the lab. The same layout plus a **seed** decides which platforms are safe. Online, both players get the same seed so the race is fair.

---

## 5. The Listener (monster)

### 5.1 Identity (Open — [Title and Monster Identity](../planning/still-water/decisions/06-title-and-monster.md))
Working concept: a pale, eyeless, long-limbed thing that lives under the water. Usually only a ripple, a dorsal ridge or a head breaking the surface can be seen. It hunts **by sound**. Must be original and not resemble the Roblox creature.

### 5.2 Behavior (Draft)
| State | Trigger | Behavior |
|---|---|---|
| Dormant | level start | Out of sight. Occasional distant ripple or sound for atmosphere. |
| Alerted | player splashes | Surfaces at the spawn point farthest from the player, lets out a sting sound, starts swimming toward the splash. |
| Hunting | after alert | Swims toward the player's current position at hunt speed. Can't leave the water. |
| Catch | reaches a player who is in the water | Short catch animation and camera jolt, then LOSE (offline) or a penalty (online, see §8). |
| Returning | player climbed out before the catch | Lingers ~2 s near the platform, then sinks back to Dormant. |

- Splashes **stack**: each extra splash in a level makes the next alert faster (e.g. +10% hunt speed). This rewards clean runs.
- Pathfinding: straight-line chase on the water plane, with simple steering around platform pillars (NavMesh on the water plane if needed).

### 5.3 Monster numbers (Draft)
| Level | Hunt speed | Alert delay (sound → start swimming) |
|---|---|---|
| 1 | 2.5 m/s | 1.5 s |
| 2 | 3.0 m/s | 1.2 s |
| 3 | 3.3 m/s | 1.0 s |
| 4 | 3.6 m/s | 0.8 s |
| 5 | 4.0 m/s | 0.6 s |

---

## 6. Levels (Draft; meets lab R1 "each harder or different")

Lab guidance: L1 teaches, L2 tightens, L3 adds something new, L4 combines it with pressure, L5 uses everything.

| # | Name (working) | Goal text shown at start | What's new or harder | Pattern length / glow time per tile | Timer |
|---|---|---|---|---|---|
| 1 | Shallow End | "Remember the glowing path. Reach the door." | Teaches the basics. Wide platforms, short gaps, first-time hints. | 5 / 0.8 s | 120 s (generous) |
| 2 | Deep End | "Cross without a single splash for 3 stars." | Longer path, wider gaps, faster monster | 7 / 0.7 s | 120 s |
| 3 | Current | "Time your jumps. Some platforms drift." | **New:** moving platforms | 7 / 0.6 s | 120 s |
| 4 | Lights Out | "The lights are failing. Trust your memory." | **New:** fog and flickering lights, timed sinking platforms | 8 / 0.5 s, shown once | 100 s |
| 5 | The Drain | "Everything you've learned. Get out." | Long path with every mechanic, smaller platforms, fastest monster | 10 / 0.45 s | 120 s |

Extra levels (6+) are post-lab ideas. See [IDEAS.md](IDEAS.md).

### 6.1 Win, lose, stars (Draft)
- **Win:** reach the Exit door.
- **Lose:** caught by the Listener, or the timer runs out (if the level has one).
- **Stars (1–3):** ★ clear · ★★ clear with ≤ 1 splash · ★★★ clear with 0 splashes **and** under the par time.
- **Unlock:** clearing level *n* unlocks *n+1* (lab R4). Optional bonus: total stars gate a later "world".

---

## 7. Offline mode (Draft)

- **Solo campaign:** levels 1–5 with locking, stars and saved progress. **Required.**
- **Vs computer ("Ghost Swimmer"):** an AI rival crosses the same level on a pre-recorded or scripted route, with mistakes that become rarer at higher levels. First to the door wins. The lab rubric's *Excellent* band for offline mode asks for "AI or pass-and-play included", so this is worth building if time allows. *(Open, see [Offline Rival](../planning/still-water/decisions/09-offline-rival.md).)*
- Works fully in airplane mode. No network code runs offline.

## 8. Online mode (Draft; details Open in [Online Match Format](../planning/still-water/decisions/07-online-match-format.md))

- **Format:** real-time **1v1 race**. Both players cross the same layout with the same seed (same safe path). First to the Exit wins.
- **What you see:** the opponent as a slightly translucent figure. No collisions between players.
- **Splash online:** the Listener hunts whoever splashed. If caught, that player respawns at their last safe platform after a **time penalty** (e.g. 3 s) instead of losing outright, so the race keeps going.
- **Who decides:** the server holds the layout and seed, checks positions for impossible moves, decides splashes, catches and finishes, and announces the winner (lab rule: the host/server decides).
- **Effect on offline progress:** online results do **not** unlock offline levels (the lab's simplest rule).
- **Lobby (Draft):** Quick Match (matchmaking) + Create/Join Private Room by code. A clear error screen when the server can't be reached.
- **Disconnect:** if a player leaves mid-match, the other wins by forfeit and sees a message, then returns to the menu.

---

## 9. Screens (lab flow)

Title → Main Menu (Play, Settings, Credits) → Mode Select (Offline / Online)
- Offline → Level Select (lock icons, stars) → Level (HUD: goal, timer, splash count, pause) → Result (Retry / Next / Menu)
- Online → Lobby (Quick Match / Room code, status text) → Online Match → Online Result → back to Lobby
- Connection Error (message + back to Mode Select)
- Pause menu (Resume, Restart, Sound on/off, Quit to menu)
- Settings (music volume, SFX volume, vibration, camera sensitivity, *optional:* text size)
- Credits

Android back gesture = Back on every screen.

---

## 10. Art direction (Draft — [Art Style](../planning/still-water/decisions/08-art-style.md) is Open)

- **Mood:** liminal and clinical. Endless pale-tiled pool halls, still green-turquoise water, harsh overhead light, haze in the distance. Scary because it is empty and quiet.
- **Fidelity:** low-to-mid poly with good lighting. Not HD realism. Options: *clean liminal* (smooth, soft light) or *PSX-style* (low-res textures, vertex jitter, dithering). Pick after a 1-day prototype.
- **The look comes from (URP):** fog, bloom on the lights, vignette, film grain, slight chromatic aberration when alerted, flickering lights in L4, caustics projected on the tiles.
- **Water:** a simple URP Shader Graph water (scrolling normals, depth tint, foam at edges), plus splash particles and ripples.
- **Player:** a plain, unbranded figure (e.g. a Mixamo character, or a simple low-poly person in a swimsuit or hoodie).
- **Performance budget:** 60 fps target, 30 fps minimum on a mid-range Android. Baked lighting wherever possible. Few real-time lights.

## 11. Audio direction (Draft)
- No music during play by default, only room tone: HVAC hum, dripping, distant water lapping. Silence is the horror.
- A pattern chime that rises in pitch per platform.
- **Splash** is loud and has reverb. It should make the player flinch.
- The Listener: a low wet rumble when it surfaces, a heartbeat that speeds up as it gets close.
- Menu music: a slow, detuned ambient loop.
- Sources: Freesound (check each license), jsfxr for UI sounds, own recordings (real splashes!). Every asset goes in [CREDITS.md](CREDITS.md).

## 12. Progression extras (bonus; Draft)
Stars per level, a total-star gate for a later world, a tutorial overlay on L1, settings, accessibility (adjustable text size, a color-blind-safe glow color, a reduced-flicker option for L4), and an online leaderboard (fastest clear per level) if Supabase is added.
