# Online Match Format

- **Status:** Locked (2026-11-16, by: delegated under deadline authority)
- **Type:** grill
- **Blocks:** [Netcode Model](./10-netcode-model.md), Phase 4

## Question
What exactly happens in an online match?

## Why it matters
It decides what the server must simulate and what gets synced. Every extra interaction between players multiplies netcode work.

## Sub-questions with proposals
| # | Question | Options | Proposal |
|---|---|---|---|
| 1 | Mode | Race / co-op (same map, wait for each other at the door: user idea 2026-11-05) / sabotage | **Race (1v1)**, same layout + same seed; co-op to be weighed when this is grilled |
| 2 | Which level? | Fixed arena / host picks from levels they've unlocked / random from 1–3 | **Random from a pool of online-only layouts (start with 1)** |
| 3 | Caught online | Out of the match / time penalty + respawn | **3 s penalty, respawn on the last safe platform** |
| 4 | One Listener or one per player? | Shared / each player has their own | **One per player** (simpler and fair; each player's monster only sees its owner) |
| 5 | Can players see each other? | Full 3D ghost / progress bar only | **Translucent 3D ghost, no collision** (fallback: progress bar) |
| 6 | How do players find each other? | Quick match / room code / both | **Both** (room code is reliable for the demo) |
| 7 | Time limit | none / 3 min | **3 min; if both are still crossing, the one closer to the exit wins** |
| 8 | Disconnect | Remaining player wins by forfeit | **Forfeit win + message** |
| 9 | Rematch | Button on the result screen | **Yes, if time allows** |

## Recommendation
Accept the proposals. They keep the server simple: no player-to-player interaction except "who finished first".

## Decision
Deadline scope in [Lab-ready build](../../lab-ready-plan.md) supersedes proposals above: 1v1 race on existing Level01; quick match + room ID; independent server-simulated Listener per player; catch = respawn at start with 3 s penalty; 180 s limit, nearest exit wins at timeout (equal = draw); non-colliding opponent avatar; disconnect during countdown/race = forfeit. No rematch protocol or internet hosting this pass. Online never changes offline progression. Laptop-hosted LAN/hotspot remains a documented deviation from literal phone hosting.

**Amendment, 2026-11-16 (by: me): the co-op circuit replaces the 1v1 race online.** The client uses the new
`circuit` room. The old `race` room and its tests stay on the server, unused.

For the separately selected two-player phone-hosted circuit, see [Hosting and LAN Plan](12-hosting-and-lan.md).

Decided by me:
- **2–4 players.**
- **The circuit:** rooms 1→5 joined end to end in one scene. You walk through each door into the next room: no
  loading, no Next button.
- **One shared Listener per room.** It hears everyone, and swimming is noisy, so it goes for the nearest swimmer.
- **Team doors:** a room's door opens only when every player has reached it.
- **Team clear + personal ranking:** the team wins by clearing room 5 before time runs out, and each player is
  ranked by points.
- **Caught:** back to the current room's start with a 3 s penalty.

Defaults set by the agent (in `CIRCUIT_CONFIG`, server/src/logic/circuit.ts; change them there):
- **Lobby:** starts at once when 4 players are in. With 2–3 who are all ready, it waits 15 s for more (the wait
  restarts when someone joins). Joining closes at the countdown.
- **Time limit:** 15 min.
- **Points:** door arrival 3/2/1/0, splash −1, caught −3, +5 each when the team clears.
- **Disconnects:** the team carries on and stops waiting at the door for whoever left. Everyone gone = run over.
- **What you see of teammates:** their chosen bodies, with P1–P4 name tags, and you hear their splashes.

Protocol: `ready {body}`, `pos`, `geometry` → `circuit {rooms}`, `respawn`, `snapshot`. See SYSTEM_ATLAS.

**Revision, 2026-11-16, after the first co-op test (by: me).** These replace the team doors and the shared Listener:
- **Open doors: a race.** You run the circuit at your own pace. You can still wait for or help a teammate, but
  nobody has to.
- **A Listener in every room.** It hears and hunts whoever swims in its room. Being caught sends you back to that
  room's start.
- **Every catch is shown to everyone:** the room's Listener rears up where it happened and screams. Your own catch
  also gets the catch camera before you respawn.
- **Pushing:** PUSH (or F) shoves the nearest player in front of you, within 1.8 m and a 70° cone, at 7 m/s. It has a
  1.2 s cooldown. The server relays it, and a pushed player may move faster than the speed check allows for 1.5 s.
- **Points:** first/second/third/fourth to each door 3/2/1/0, finishing places 10/6/3/1, splash −1, caught −3, and
  +5 each if everyone gets through in time.
- **End of the run:** when everyone is through, or when time runs out.
- **Same Listeners as solo (2026-11-16):** each room's Listener uses its solo level's numbers. They live in
  `server/src/logic/listenerTuning.json`: hunt 8/11/14/18/20 m/s, no alert delay from room 2. Before this, online used
  Level 1's 8 m/s everywhere. `ListenerTuningParityTests` (EditMode) fails if the file and the LevelConfigs differ, so
  change both together.
