# Hosting and LAN Plan

- **Status:** Locked (2026-11-16, by: me). Add direct phone-hosted multiplayer to meet Lab 5 Option A.
- **Blocks:** Phase 4 and Lab 5 Checkpoint 5.

| Use | Host | Join |
|---|---|---|
| Lab 5 Option A | One Android phone runs the race in the app on the hotspot network | **Local Multiplayer → Join**, using the host's IPv4 address |
| Lab 5 Option B | Colyseus on Render | **Online → Quick Match** or **Join Room** |
| Development / Lab 6 | Colyseus on Windows (`:2567`) or in the Ubuntu VM (`:8080`) | **Online**, using the laptop's network address |

The local session is a two-player version of the five-room circuit. The host decides catches, pushes, scoring, placement, and the result. Both phones load the circuit from their installed game. Join by address for the lab demo. A lost peer ends the local session and returns the player to the Local Multiplayer screen with a message.

Use the two-phone steps in [Setup §8](../../../docs/SETUP.md#8-playing-without-internet-on-a-phone-hotspot) to verify Option A with mobile data off. Use the Render steps in [Setup §9](../../../docs/SETUP.md#9-free-internet-server-render-0) for Option B.
