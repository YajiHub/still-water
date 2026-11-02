# Netcode Model

- **Status:** Blocked (by [Online Match Format](./07-online-match-format.md))
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
_(fill in when unblocked and locked)_
