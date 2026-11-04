# Camera Style

- **Status:** Locked (2026-11-04) (by: me): **C. first person + jump aids**, after the Phase 1 phone test.
- **Type:** prototype
- **Blocks:** Phase 1
- **May reopen:** [Control Scheme](./02-control-scheme.md) (its base is Starter Assets *Third Person*; first person would switch the base to the *First Person* controller from the same v2.0.2 package, already imported)

## Question
First person or third person, and how does the camera help the player judge jumps?

## Why it matters
Two goals pull against each other. Horror wants a limited view (you hear the Listener behind you; you look down at dark water). "Skill, not luck" ([DESIGN.md §1.3](../../../docs/DESIGN.md#13-experience-goals-draft)) wants landings you can read. On a phone the right thumb is also busy with Jump.

## Options
| Option | For | Against |
|---|---|---|
| **C. First person + jump aids (leading)** | Scariest: no safe overview, the monster is unseen behind you; immersive | Landings are hard to judge without aids; motion sickness risk; reveal is harder to see from eye level |
| A. Third person, auto-follow + drag-to-look | Readable jumps; blob shadow works; current Starter Assets base | Overview of the room lowers tension; auto-rotation can swing |
| B. Third person, fixed chase angle | Most readable depth | Least scary; on-rails feel |

## Jump aids that first person needs (all tunable, all in config)
- **Landing marker:** the blob shadow projected straight down while airborne (already a required helper).
- **Visible legs/feet** when looking down (cheap version: shoes only).
- **Edge readability:** a thin light strip along platform edges (also helps the dark L4).
- **Camera help:** FOV ~75–85°, slight down-tilt in mid-air / near edges, small dip on landing.
- **Ledge grab:** barely missed jumps catch the edge (forgiving, and dangling over the water is tense).
- Keep coyote time, jump buffer and forgiving colliders.

## First-person problems to solve
- **Reveal from eye level:** raised start platform or a short high camera pan during the Reveal.
- **Monster behind you:** 3D positional audio for splash/Listener + speeding heartbeat.
- **Look controls:** drag on the right half to look, Jump bottom-right (Starter Assets First Person already ships mobile look + jump).
- **Motion sickness:** subtle head bob with a settings toggle, no narrow FOV.
- **Hybrid:** a short third-person shot only for the catch animation.

## Prototype plan (Phase 1, ~2–3 hours)
In the grey-box Level 1, build **C** (with marker + edge glow; ledge grab can come after) and **A**. Play each on the phone for 5 minutes. Measure: missed jumps that feel like *the camera's* fault, can you read the reveal, and does C cause discomfort. Ideally 1–2 other people try it too.

## Recommendation
Prototype **C first**; keep **A** as the fallback. Lock C only if missed jumps still feel fair on the phone.

## Decision
**First person with jump aids (option C).**

**Evidence (Phase 1 phone test, 2026-11-04, mid-range Android, 60 fps):** both rigs on identical movement, 10 jumps each, first person played first. First person felt more immersive, and its misses felt like the player's fault, not the camera's (the "skill, not luck" bar). Exact miss counts weren't recorded; one outside tester re-checks the feel at the Phase 1 exit bar.

**Built and kept:** blob-shadow landing marker, edge glow strips, FOV 80°, mid-air down-tilt, landing dip, coyote time, jump buffer, forgiving colliders (all in `MovementConfig`).

## Consequences
- `ThirdPersonLook` and the 1st/3rd toggle stay as a **debug tool** until Phase 5, then the toggle is removed from release builds. A third-person shot may come back only for the catch animation (hybrid, above).
- Still to do for first person, in the phase that needs it:
  - **Phase 2:** reveal from eye level (raised start or a short high pan during the Reveal); the Listener behind you (3D positional audio + heartbeat).
  - **Phase 2 or 5:** visible shoes/legs when looking down; ledge grab for barely missed jumps.
  - **Phase 5:** head-bob with a Settings toggle (motion sickness).
- [Control Scheme](./02-control-scheme.md) needs no reopen: decision 15 already moved the base to our own `PlayerMotor`.
- Online (Phase 4): the rival is seen from outside, so remote players need a visible body model (robot from Starter Assets).
