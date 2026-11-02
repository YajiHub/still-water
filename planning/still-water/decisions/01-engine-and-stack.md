# Engine and Stack

- **Status:** Locked (2026-10-27) (by: me, on the AI's recommendation). Reopen only if Unity proves unworkable in Phase 0–1.
- **Type:** research
- **Affects:** every phase

## Question
Which engine and backend should the game use, given: 3D horror look (not HD), Android, offline + online, ₱0 budget, 1–2 week build, and the goal of maximum value for a **software development** portfolio (not specifically game dev)?

## Options considered
| Option | For | Against |
|---|---|---|
| Godot 4 + built-in networking / Nakama (handout default) | Fastest for the lab, since the handout gives working code; open source; light | GDScript has little value outside games; 3D mobile look needs more manual work; C# on mobile still experimental |
| **Unity 6 (C#, URP) + Colyseus (Node/TS)** | C# carries over to .NET jobs; TypeScript/Node carries over to web backend jobs; best free 3D/horror ecosystem (URP post-processing, Starter Assets, Mixamo); first-class Colyseus Unity SDK; Docker + cloud deploy story | Two new things to learn at once; heavier editor; closed source (instructor OK'd it) |
| Flutter + Flame | Already known (Dart) | 2D only, so the 3D horror concept doesn't work; a 4th Flutter project adds little to the portfolio |
| Three.js / R3F + Capacitor | TypeScript everywhere | No editor; 3D + fog + water in a phone WebView is risky on low-end devices; slowest to build |
| Unity + Nakama | The handout mentions Nakama | Nakama's core is Go and its TS modules run in a custom runtime, so the skills carry over less than plain Node |
| Unity + ASP.NET SignalR | One language end-to-end | No official Unity client; IL2CPP friction; more plumbing |

## Decision
**Unity 6 (latest 6.x LTS) + URP + C#** for the client. **Colyseus (Node.js + TypeScript)** authoritative server, **Dockerized**, deployed by a CI/CD pipeline to an Ubuntu VM on the laptop (Lab 6, also the same-Wi-Fi server), with the **Render free tier** as an optional internet host. **Supabase** optional for a leaderboard.

## Why
- Portfolio: adds **C#** and **TypeScript/Node + WebSockets + Docker + cloud deploy** to a profile that is already strong in Flutter/Django/Python.
- Product: Unity's free assets and URP effects get the "scary but not HD" look fastest.
- Dev machine: Lenovo Legion Slim 7i, 16 GB RAM, which is comfortable for Unity 6.

## Consequences
- The handout's Godot code samples become *concept references*; [LAB5_REQUIREMENTS.md](../../../docs/LAB5_REQUIREMENTS.md) maps them to Unity equivalents.
- Render free tier sleeps after 15 min idle (~1 min cold start). Warm it before demos and keep the LAN fallback.
- Option A / Option B equivalence must be confirmed: see [Hosting and LAN Plan](./12-hosting-and-lan.md).
- Must check exact package versions before coding (Unity 6 ships Cinemachine 3, whose API differs from 2.x; Colyseus's schema/callback APIs change between minor versions).
