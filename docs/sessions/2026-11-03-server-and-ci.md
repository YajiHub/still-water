# Session 2026-11-02 → 11-03 — Unity project, Colyseus server, CI test job

## Focus
Finish Phase 0 setup and start Phase 0b (Lab 6 CI/CD).

## What Was Built / Changed
- **Unity `client/`** (6000.3.27f2, Android active): Player settings (`Still Water`, `com.itsd85.yajihub.stillwater`, Company `YajiHub`, Auto Rotation landscape L/R only, **Allow downloads over HTTP = Always allowed**), Unity `.gitignore`, Starter Assets **v2.0.2** (Cinemachine 3.1.7, Input System 1.20.1), Colyseus Unity SDK **0.18.10**.
- **`NetTest`** (`Assets/_Project/Scripts/Net/NetTest.cs` + scene `NetTest.unity`): joins `my_room` from the Editor and the phone (`ws://192.168.1.9:2567`).
- **`server/`**: `create-colyseus-app@0.18.1 --ts --preset minimal` (colyseus 0.18.10), `.npmrc` → registry.npmjs.org, `GET /` status page + `GET /health` (`APP_VERSION`), playground moved to `/playground` (dev only), ESLint 10 + typescript-eslint 8, `tsc --noEmit`, tests on `node:test` + `tsx` (mocha removed), first pure rule `isMovePlausible()` (body written by the user), multi-stage `Dockerfile`, `compose.yaml` (`8080:2567`, image tag per SHA), `.dockerignore`.
- **`.github/workflows/ci-cd.yml`**: `test` job (npm ci → lint → typecheck → test), actions pinned to commit SHAs.
- **Decisions locked:** [Orientation](../../planning/still-water/decisions/03-orientation.md), [Hosting and LAN Plan](../../planning/still-water/decisions/12-hosting-and-lan.md), [Server Test and Lint Tooling](../../planning/still-water/decisions/14-server-test-tooling.md).
- Docs: QA profile filled in; doc inconsistencies from the first status check fixed; stray `LAB_REQUIREMENTS.md` deleted.
- Commits: `b3d27bf`, `25234d2`, `b3ac312`, `d96878c` (pushed by the user).

## Verification
- `npm run lint` exit 0 · `npm run typecheck` exit 0 · `npm test` 13/13 pass, exit 0 (Windows Node 24.13.0; same in a Linux `node:24` container).
- actionlint: 0 errors. GitHub Actions `CI/CD` on `d96878c`: **success**.
- Docker image built and ran locally: container `healthy`, `/health` → `{"status":"ok","version":"manual"}`, `/playground` 404 in production.
- Phone: Playground with touch controls runs; NetTest joins the LAN server.
- Ownership check: 2 questions (env-var flow: needed the answer; stale health check: partly right).

## Mistakes & Gotchas
- Unity 6 blocks plain HTTP by default → `Insecure connection not allowed`; Colyseus matchmakes over HTTP before the WebSocket.
- Starter Assets' `UI_TouchScreenInput` ships **inactive** in the Playground scenes → no touch controls on the phone until ticked.
- mocha passes then crashes on Windows (`UV_HANDLE_CLOSING`, exit -1073740791) → switched to `node:test`.
- Parallel test files hung: `@colyseus/testing` always uses port 2568 → `--test-concurrency=1`.
- The user's global npm registry was npmmirror → lockfile pointed there; fixed with `server/.npmrc`.
- `npm test` run from the repo root → `ENOENT package.json`; run it inside `server/`.
- `npm audit --omit=dev`: 15 low/moderate via Colyseus's unused OAuth module; accepted (only "fix" downgrades Colyseus).
- `%ERRORLEVEL%` on one `cmd` line is read before the command runs; use PowerShell `$LASTEXITCODE`.
- No instructor questions (by: me): deviations (laptop server for Option A, Colyseus instead of Nakama) are documented and argued in the paper.

## Decisions
See the three locked decisions above. Task ownership changes: `NetTest` 🤝→🤖, QA profile 🧑→🤖 (user's request).

## Next Step
`/skill:execute-phase Phase 0b` → SETUP §7.4: install VirtualBox + Ubuntu Server 24.04 (2 vCPU, 2–4 GB, 25 GB; forwards ssh 2222→22, game 8080→8080), user `deploy` + OpenSSH, Docker, manual dry run → 📸 CP5.
Small leftovers: remove the old `// TODO (you)` hint comment in `server/src/logic/movement.ts`; take 📸 CP1/CP3/CP4; Third Person Playground phone build; Checkpoint 1 (GDD + paper-prototype photo).
