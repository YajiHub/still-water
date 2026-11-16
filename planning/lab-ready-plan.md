# Lab-ready build — 2026-11-16

## Destination
Stable Android APK with five offline levels and same-Wi-Fi / hotspot 1v1 racing before school (~07:00 next day). User delegated all remaining decisions, waived predictions, requested controlled subagents. No unbounded agent fan-out: max one agent at a time; no concurrent builds.

## Locked decisions (by: delegated)
- Prioritize playable build over visual rewrite / papers. Existing monster and graphics stay unless broken; water arc deferred.
- Online arena = existing Level01, no fakes/movers/sinking, independent Listener per player. Quick match + join by Colyseus room ID. Two ready clients → 3 s countdown → 180 s race. First validated exit wins; timeout nearest exit wins, equal distance draw; disconnect after match starts = forfeit, waiting disconnect = cancel.
- Client moves instantly; sends bounded positions at 15 Hz; server validates finite/bounds/speed, derives water entry from position, simulates Listener, decides catches and finish. Catch = server teleports to start, 3 s penalty; client waits for server respawn packet. Online never changes offline save.
- Snapshot JSON strings at 10 Hz (NoState room), remote capsule interpolated; no schema/codegen dependency. Messages: ready {}, pos {x,y,z,yaw}; server snapshot string {phase,countdown,remaining,winner,reason,players:[{id,x,y,z,yaw,splashes,penalty,monsterX,monsterZ,monsterState}]}, respawn {x,y,z}. Server rules/config constants in race config module.
- LAN endpoint editable and saved locally; no network initialization until Online selected. Timeout/error/leave return to lobby with clear message. No reconnect, rematch through lobby.
- Existing Node server on laptop :2567 for fastest demo; deployed VM :8080 optional, user operates VM. Laptop server != phone-host requirement, remains documented deviation.

## Scope / file impact
Server: src/logic/race.ts + tests, src/rooms/RaceRoom.ts + tests, src/app.config.ts registration. Unity: Scripts/Net/{LanRace,OnlineLevelController}.cs, UI/MainMenu.cs entry, Level/LevelController.cs online bypass, Player/PlayerMotor.cs server teleport; UI sound toggle/credits if time. Docs: decisions 07/10, this plan, AGENTS/MAP status.

## Execution
- [x] 🤖 Test-first race rules + real room integration tests (ready/countdown, input rejection, splash/catch/respawn, finish, timeout, forfeit).
- [x] 🤖 Unity persistent LAN session/lobby, bounded connect cancellation, JSON snapshots, online scene adapter and opponent view; offline unchanged.
- [ ] 🤖 Sequential independent review; run server lint/typecheck/test/build.
- [ ] 🧑 Unity compile + EditMode tests; Editor + phone online smoke test.
- [x] 🤖 Minimum polish: sound toggle + credits; bounded monster fix only if visibly broken.
- [ ] 🧑 Two-device LAN full race, disconnect/unreachable, airplane-mode offline, five-level unlock/save checks, APK build and copy for school.

No commit/push or VM changes unless asked. Actual device/Unity evidence required before completion claims.
