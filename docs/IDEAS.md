# Ideas Parking Lot

> Drop any idea here, half-formed is fine. Nothing here is a commitment.
> When an idea is worth deciding on, a Wayfinder session moves it into the MAP
> (Fog of War → Active Frontier → a decision file). Then delete it here or mark it ➜ moved.
>
> Format: `- [tag] idea — why it might be good / worry`
> Tags: [gameplay] [level] [monster] [online] [art] [audio] [ui] [tech] [post-lab]

## From the kickoff discussion
- [gameplay] Re-show the pattern once per level for a star penalty. Good for accessibility; could make it too easy.
- [gameplay] Branching paths and decoy tiles next to the true path. Harder memory without longer levels.
- [gameplay] Throwable object (e.g. a pool ball) that makes noise to lure the Listener away. Adds strategy; is it scope creep?
- [monster] The Listener only moves while you move ("red light, green light" style). Very tense; may conflict with timed sinking platforms.
- [online] Co-op mode: one player sees the pattern, the other has to cross (call out the path). Very fun and social, but needs voice or ping markers.
- [online] Your splash also alerts the Listener toward your opponent if they're in the water. Sabotage potential.
- [online] Emote / ping wheel during the race.
- [level] Level 6+: a dark "night" pool lit only by a flashlight cone.
- [level] Vertical level: ladders and diving boards, climbing upward out of a flooding room.
- [art] Caustic light patterns that ripple across the walls. Cheap and very atmospheric.
- [audio] Use the phone's haptics on splash and when the Listener gets close.
- [ui] The pattern-reveal chime doubles as an audio cue for players who struggle to see the glow.
- [post-lab] Endless mode with procedurally generated pools + an online leaderboard.
- [post-lab] Play Store release (needs the $25 developer fee ≈ ₱1,400–1,500, plus Google's closed test with 12 testers for 14 days for new personal accounts). Until then: GitHub Releases / itch.io.
- [post-lab] Cosmetic skins unlocked by stars.
- [tech] Unity CI with GameCI: run Unity tests and build the APK in GitHub Actions on every tag, attaching it to the Release. Strong portfolio item; needs Unity license activation as a repo secret, and builds are slow (~15–30 min).
- [tech] A second deploy job that pushes the same server image to Render for internet play.

## New ideas
- [gameplay] **Continuous mode** (user, 2026-11-05): levels joined by doors: the exit door opens onto a corridor into the next, bigger hall, without leaving to the menu; you can walk back. Lab R3 still needs a result (per hall, or one at the end of the run). Cheap version for Phase 5: door-open animation + fade straight into the next level, with a short result toast. Full version (connected halls, walking back) is post-lab.
- [online] **Co-op on the same map** (user, 2026-11-05): two players cross the same hall together and wait for each other at the door. Weigh it against the 1v1 race in [Online Match Format](../planning/still-water/decisions/07-online-match-format.md).
- [gameplay] Diving under the surface (post-lab): see the Listener's shape below you; needs underwater rendering, breath and 3D swim controls.
- 
