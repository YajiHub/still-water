# Netcode Model

- **Status:** Locked (2026-11-16, by: delegated under deadline authority)
- **Type:** domain-model
- **Blocks:** Phase 4

## Question
Who simulates what, what is synced, how often, and how is cheating or desync handled?

## Draft model (to validate once the match format is locked)
| Concern | Proposal |
|---|---|
| Player movement | **Client-side**: each phone moves its own character instantly (no input lag) |
| Position sync | Client sends `pos` at 15–20 Hz; the server stores it in `RaceState`; the other client interpolates with a ~100 ms buffer |
| Validation | The server rejects speeds above max run/jump speed + tolerance, and positions on sunk platforms; it flags rather than kicks |
| Layout / safe path | The server generates the seed. Either both sides run the same deterministic layout function **or** the server sends the safe-platform list after the reveal. *(Pick one: deterministic code in both C# and TS is a desync risk; sending the list is simpler.)* |
| Fake-platform sinking | The server is authoritative: a client reports "landed on platform X", and the server broadcasts "platform X sinks" (each player has their own copy of the layout) |
| Splash detection | The client reports `fell`; the server sanity-checks against the y position / water height |
| Listener (online) | Simulated on the server at a fixed tick (10–20 Hz) per player; clients display it with interpolation |
| Finish & winner | The client reports `reachedExit`; the server verifies the position is near the exit and records the server timestamp; the **server announces the winner** |
| Disconnect | Colyseus `onLeave` → forfeit; client `onLeave`/error → Connection Error screen |
| Reconnect | Out of scope for the lab (maybe allow a short reconnect window later) |

## Known limitation (fine for a class project; write it in the paper)
Client-authoritative movement means a modified client could fake positions. Validation catches only obvious cases.

## Decision
[Lab-ready build](../../lab-ready-plan.md) locks this pass: client-side movement, bounded finite/speed validation on server; server derives water entry, simulates per-player Listener, decides catch/respawn, finish/timeout/forfeit. Fixed Level01 layout on both sides, no moving/sinking/fake platforms online. Two scene-ready clients trigger countdown; positions at 15 Hz, complete JSON-string snapshots at 10 Hz via a NoState room (no schema generation); client interpolates opponent. Configurable LAN endpoint; no connection until user selects Online; timeout/disconnect offers lobby/menu. Catch respawns at start with 3 s penalty. No reconnect. Protocol fields and scope in lab-ready plan. This is plausibility checking, not cheat-proof authoritative player physics.
