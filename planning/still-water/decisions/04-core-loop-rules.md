# Core Loop Rules

- **Status:** Open
- **Type:** domain-model
- **Blocks:** Phase 2

## Question
What are the exact rules and states of one level (reveal, crossing, splash, catch, timer, stars)?

## Why it matters
These rules decide the `LevelController` state machine, the HUD, star logic, and what the online server has to copy. Validate the edge cases before coding.

## Draft model

### Level states
```
Intro(goal text) → Reveal → Crossing ⇄ InWater → Won | Lost
                                   └─ Paused (from any playable state)
```

### Player states
`Grounded → Airborne → Grounded` · `Airborne/Grounded(on fake) → InWater → Grounded (climbed)` · `InWater → Caught`

### Invariants
- The timer starts when Reveal ends (not during the reveal).
- Splash count +1 each time the player enters the water.
- Every splash strengthens the Listener (+10% speed for the rest of the level).
- Fake platforms sink ~0.4 s after the player lands; they stay sunk for the rest of the attempt.
- Win is checked on touching the Exit trigger; saving happens before the Result screen.

## Edge cases to validate
1. Can the player walk forward during the Reveal, or are they frozen? *(Proposed: frozen.)*
2. Timer: on every level, or only some? What happens at 0, an instant loss? *(Proposed: all levels; 0 = loss.)*
3. Re-show the pattern: allowed? Cost? *(Proposed: once, and you lose the ★★★ chance.)*
4. Climb-out spot: the nearest safe platform, or only the last one you stood on? *(Proposed: any safe platform you swim to.)*
5. Can you swim the whole way to the exit? *(Must be prevented: swim speed < monster speed, and the exit has a tall edge you can't climb from the water.)*
6. Does landing on a fake platform reveal it permanently (so a retry is easier)? *(Proposed: retry uses the same layout; the player's memory improves, which is fine.)*
7. Does the safe path reshuffle between attempts? Options: never (fixed per level) / on every retry / only when you leave and come back. *(Proposed: fixed per level for the lab. That makes difficulty predictable for playtests. Reshuffling is a post-lab idea.)*

## Recommendation
Accept the proposed answers above unless the user prefers otherwise, then reflect them in DESIGN.md §2, §4 and §6.

## Decision
_(fill in when locked)_
