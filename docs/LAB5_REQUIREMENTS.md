# Lab Requirements — ITSD 85 "Mobile Game with Level Progression and Offline/Online Multiplayer"

> Condensed from the lab handout (dated Oct 20, 2026; 39 pages). Use this as the acceptance checklist.
> The instructor allows other stacks than Godot, as long as there is a frontend + backend and the requirements are met.
> We follow the handout as written and don't ask the instructor for exceptions (by: me, 2026-11-03). Deviations are marked ⚠️ and argued in the paper.

## Minimum requirements (all ten must pass)

| # | Requirement | How it's checked | How Still Water meets it | Done |
|---|---|---|---|---|
| R1 | ≥ 5 levels, each harder or different | Instructor plays 1–3; video shows 4–5 | Levels 1–5 in [DESIGN.md §6](DESIGN.md#6-levels-draft-meets-lab-r1-each-harder-or-different) | [ ] |
| R2 | Clear goal per level shown to the player | Goal text visible on level start | Goal banner during the Reveal phase | [ ] |
| R3 | Win and lose conditions + result screen (stars/score/time) with Retry / Next / Menu | Fail a level on purpose, then clear it | Caught or timeout = lose; door = win; Result screen | [ ] |
| R4 | Level n+1 locked until n is cleared | Lock icon, can't open | Level Select reads `unlockedLevel` | [ ] |
| R5 | Progress saved, survives closing the app | Clear L2, force-close, reopen → L3 open | JSON save written before the Result screen | [ ] |
| R6 | Offline mode works in airplane mode | Airplane mode + play | Solo campaign (+ Ghost rival) with no network code | [ ] |
| R7 | Online: two players, two devices, same match, real-time or turn-based | Two phones play one match | 1v1 race over the Colyseus server | [ ] |
| R8 | Mode selection on the main menu + clear message if the server is unreachable | Wi-Fi off → Online → error, not a crash | NetClient timeout → Connection Error screen | [ ] |
| R9 | Runs on a real phone, fixed orientation, touch controls | Installed on a team member's phone | Android APK, joystick + jump | [ ] |
| R10 | Basic polish: title screen, pause menu, sound on/off, readable text | Checklist during demo | Body text ≥ 28 px at 720p-equivalent, buttons ≥ ~9 mm | [ ] |

### Online options
- **Option A (all teams must do it):** two phones on the **same Wi-Fi**, no external server (one phone hosts).
  ⚠️ **Deviation:** a Colyseus server on a laptop on the same Wi-Fi (the pipeline-deployed VM, `ws://<laptop IP>:8080`) instead of a phone host. Argued in the paper; see [Hosting and LAN Plan](../planning/still-water/decisions/12-hosting-and-lan.md#known-deviation-from-the-handout-write-it-in-the-lab-5-paper).
- **Option B (higher grade + bonus):** internet matchmaking (the handout uses Nakama). Our equivalent is Colyseus deployed on Render.
  ⚠️ Deviation: Colyseus instead of Nakama for the "working Option B deployment" bonus; the paper explains the equivalence (internet matchmaking + an authoritative server).
- **Online design rules:** decide whether online affects offline unlocks (ours: it doesn't). Handle disconnects with a message + return to the menu. The host/server, not each phone, decides the winner.

## Bonus (up to +10)
Star rating per level + star-gated worlds · online leaderboard / player names · settings screen (volume, vibration, language) · tutorial / first-time hints · accessibility (color-blind-friendly palette, adjustable text size) · working Option B deployment.

## Rubric (100 pts)
| Criteria | Pts | "Excellent" means |
|---|---|---|
| Game design & GDD | 10 | Clear, original idea; GDD complete and followed |
| Core gameplay | 15 | Fun, responsive touch controls; clear goal per level |
| Levels & progression (R1–R5) | 15 | 5+ levels, good difficulty curve; locking, stars and saving all work |
| Offline mode (R6) | 10 | Works fully in airplane mode; **AI or pass-and-play included** |
| Online mode (R7–R8) | 15 | Two phones play a full match; disconnects handled; **Option B done** |
| UI, audio & polish (R10) | 10 | All screens, pause, sound toggle, credits; looks finished |
| Mobile build (R9) | 10 | Runs smoothly on 2+ phones; test matrix complete |
| Playtesting & feedback | 10 | 5+ testers, forms analyzed, 3+ fixes verified by retest |
| Demo, teamwork & Git | 5 | Confident demo; all members committed regularly |

## Checkpoints (5-week schedule in the handout)
| # | Due | Content |
|---|---|---|
| 1 | Wk 1 | GDD approved + photo of a paper prototype of Level 1 |
| 2 | Wk 1 | Repo set up; everyone can clone and run it |
| 3 | Wk 3 | Video: clear L1, fail L2, show L3 unlocks only after L2 and stays unlocked after a restart |
| 4 | Wk 3 | Screen recording of the airplane-mode test |
| 5 | Wk 4 | Video: two phones in one online match, result on both, error message with Wi-Fi off |
| 6 | Wk 4–5 | Screenshots of every screen on a phone + CREDITS.md |
| 7 | Wk 4 | APK + phone test matrix (2+ phones: installs, layout, touch, save, offline, online, fps, battery/heat after 10 min) |
| 8 | Wk 5 | Playtest forms, averages table, action plan with ≥ 3 fixes |

Feature freeze at the start of Week 5: only playtest fixes after that.

## Playtest (Phase 8)
5+ testers from outside the team, 15–20 min each: levels 1–3 offline + one online match. Think-aloud. Observe silently (help only after 2 min stuck, and note it). 15-item Likert form + 6 open questions (in the handout). Analysis: any item averaging < 3.5 is a problem area. A level cleared by < 60% of testers, or needing > 4 attempts on average, is too hard (except the final level). Fix the most severe and most-reported issues, then retest with 2 new players.

## Final deliverables
- GitHub repo (handout format `itsd85-lab<no>-<section>-<teamname>`; ours is [`YajiHub/still-water`](https://github.com/YajiHub/still-water), shared with Lab 6) with `README.md` (headings below, in its Lab 5 section), `docs/GDD.md`, `docs/CREDITS.md`, `docs/paper-lab5.pdf`, `docs/screenshots/`, `docs/playtest/` (anonymized).
- README headings: About the game · How to play · Offline and online modes · How to build · Download · Playtest results · Credits · Paper.
- Tag `lab5-v1.0` (the handout says `v1.0`; renamed because Lab 6 shares the repo), GitHub Release with the `.apk` + `paper-lab5.pdf` attached (the APK is never committed; 100 MB file limit).
- Demo video 2–3 min: title screen, level unlocking, a win and a loss, airplane-mode offline, online on two phones.
- **IEEE conference paper, 5–7 pages**, ≥ 12 references (≥ 6 from the last 5 years). Sections: Abstract, Intro, Related Work, Game Design, Implementation, Evaluation Method, Results (Likert means/SD, completion rates), Discussion, Conclusion. Appendix: guide-question answers, forms, action plan.
- Phone test matrix; individual contribution log (with links to commits).
- Google Classroom: repo link, release link, video link, paper PDF, contribution log, private comment (team, commit ID, phone models).
- Live demo, 7 min: pitch (30 s) → L1 + unlock (2 min) → airplane mode (1 min) → online match (2 min) → top 3 playtest findings (1.5 min).

## Guide questions (answered in the paper's appendix)
1. How does the unlock system decide which levels are open, and where is it stored on the phone?
2. What happens if the internet drops mid-match?
3. Why should the host/server decide the winner?
4. Which playtest finding surprised you most, and what did you change?
5. Which feedback item scored lowest, and what would you do with one more week?
6. Android vs iOS: what did your platform choice make easier or harder?

## Notes on the handout vs our stack
- The handout's code samples are Godot/GDScript. The *concepts* map directly: `GameState.gd` → `GameState.cs`, `ConfigFile` at `user://` → JSON at `persistentDataPath`, the ENet lobby → Colyseus room, Nakama matchmaking → Colyseus matchmaking.
- The handout says "₱0, free and open-source tools only". Unity is free but closed-source; the instructor confirmed this is acceptable.
