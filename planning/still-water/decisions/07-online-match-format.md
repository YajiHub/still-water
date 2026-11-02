# Online Match Format

- **Status:** Open
- **Type:** grill
- **Blocks:** [Netcode Model](./10-netcode-model.md), Phase 4

## Question
What exactly happens in an online match?

## Why it matters
It decides what the server must simulate and what gets synced. Every extra interaction between players multiplies netcode work.

## Sub-questions with proposals
| # | Question | Options | Proposal |
|---|---|---|---|
| 1 | Mode | Race / co-op / sabotage | **Race (1v1)**, same layout + same seed |
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
_(fill in when locked)_
