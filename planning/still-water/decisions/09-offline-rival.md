# Offline Rival

- **Status:** Open
- **Type:** grill
- **Blocks:** Phase 3 (optional feature)

## Question
Build an offline "vs computer" mode, and how?

## Why it matters
The rubric's *Excellent* band for Offline mode (10 pts) says "Works fully in airplane mode; **AI or pass-and-play included**". A solo campaign alone only reaches *Satisfactory*.

## Options
| Option | How | Cost |
|---|---|---|
| **A. Ghost Swimmer (scripted)** | An AI runner follows the level's safe path with per-level timing and a chance to "slip" (fall, then climb back out). Better each level. | Low–medium. Reuses the platform path and the remote-player view from online mode. |
| B. Replay ghost | Record your best run and race it | Low, but it's not really "vs computer" |
| C. Pass-and-play | Two players take turns on one phone; fastest time wins | Low; weaker fit for a real-time platformer |

## Recommendation
**A**, built in Phase 3 or right after Phase 4 (it can reuse `RemotePlayerView`). C is the cheap fallback if time runs out.

## Decision
_(fill in when locked)_
