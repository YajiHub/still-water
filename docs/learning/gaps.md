# Knowledge gaps

> Added by `/skill:wrap-up`. Tick `[x]` when I answer it correctly in a later session.

- [ ] How an env var travels CI → `compose.yaml` → Dockerfile `ARG`/`ENV` → `process.env` (and why read it per request) — `server/src/app.config.ts`, `server/compose.yaml` (2026-11-03)
- [ ] Health check must verify the **new version**, not just "it answers" (stale vs fresh deploy) — deploy job in `.github/workflows/ci-cd.yml` (2026-11-03)
- [ ] npm runs in the folder that has `package.json` (cwd matters) — `server/` (2026-11-03)
- [ ] TypeScript basics: `const`, arrays, `.every()`, `Math.hypot`, returning a boolean expression — `server/src/logic/movement.ts` (2026-11-03)
- [ ] Unity: an inactive GameObject in a scene doesn't run (touch controls missing) — `Playground.unity` / `UI_TouchScreenInput` (2026-11-03)
- [ ] Unity 6 blocks plain HTTP unless "Allow downloads over HTTP" is set — Player Settings, `NetTest.cs` (2026-11-03)
