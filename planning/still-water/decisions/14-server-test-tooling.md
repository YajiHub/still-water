# Server Test and Lint Tooling

- **Status:** Locked (2026-11-03) (by: delegated: the user asked for the recommended option, verified by a spike on this machine)
- **Type:** research
- **Blocks:** Phase 0b (CI/CD)

## Question
How do we lint and test a **TypeScript** Colyseus server in CI, staying close to the Lab 6 handout (which uses plain JS, `node:test` and ESLint)?

## Why it matters
The handout's commands (`npm ci`, `npm run lint`, `npm test`) must work in the `test` job, and must also give a correct exit code locally on Windows, or "run the tests before saying it's done" is meaningless.

## Options
| Option | For | Against |
|---|---|---|
| **A. `node:test` + `tsx`** (`node --import tsx --test "test/**/*.test.ts"`) | The handout's test runner (built into Node); `tsx` is already a template dependency; verified clean on Windows | — |
| B. Compile with `tsc`, then `node --test` on `build/` | No TS loader at test time | Slower; two steps; tests run against build output |
| C. Keep the template's mocha | Zero change | **Crashes on Windows after passing** (see Findings); departs from the handout |
| D. Vitest | Nicer output, watch mode | Another tool; departs from the handout |

## Findings (verified 2026-11-02/03, Windows 11, Node 24.13.0, npm 11.19.0)
- Template (`create-colyseus-app@0.18.1 --ts --preset minimal`, colyseus 0.18.10): `"test": "mocha -r tsx test/**.test.ts --exit --timeout 15000"`, `test/MyRoom.test.ts` boots the real server with `@colyseus/testing`. Other scripts: `start` (`tsx watch src/index.ts`), `build` (`tsc -p tsconfig.build.json`), `clean`, `loadtest`. No lint or typecheck.
- **mocha on Windows:** `1 passing`, then `Assertion failed: !(handle->flags & UV_HANDLE_CLOSING), file src\win\async.c` → exit code `-1073740791`, so a green test looks red locally.
- **`node --import tsx --test` on Windows:** a pure-logic test **and** the same room test via `@colyseus/testing` both pass with **exit code 0, no crash** (spike files removed afterwards).
- **No decorators:** Schema 5 defines state with `schema({...})` (`src/rooms/schema/MyRoomState.ts`), so the old worry that type stripping can't handle `@type` decorators no longer applies. `tsx` (esbuild) runs it all anyway.
- **Registry:** the user's global npm registry was `registry.npmmirror.com`, so the lockfile pointed there. Fixed with `server/.npmrc` (`registry=https://registry.npmjs.org/`) and a regenerated lockfile, so CI and the VM fetch from the public npm registry.

## Decision
**A: `node:test` + `tsx`, ESLint flat config + typescript-eslint, and a separate `tsc --noEmit` typecheck.**

`server/package.json` scripts after Phase 0b:

| Script | Command |
|---|---|
| `start` | `tsx watch src/index.ts` (template, unchanged) |
| `build` | `npm run clean && tsc -p tsconfig.build.json` (template, unchanged) |
| `typecheck` | `tsc --noEmit` |
| `lint` | `eslint .` (`eslint.config.js`: `@eslint/js` recommended + `typescript-eslint` recommended; ignore `build/`, `node_modules/`) |
| `test` | `node --import tsx --test --test-concurrency=1 "test/**/*.test.ts"` (quoted so Node expands the glob the same way on Windows and Linux; one file at a time because `@colyseus/testing`'s `boot()` always uses port 2568, and parallel test files collided and hung) |

- Convert `test/MyRoom.test.ts` to `node:test` + `node:assert/strict`; remove `mocha` and `@types/mocha`.
- CI `test` job order: `npm ci` → `npm run lint` → `npm run typecheck` → `npm test`.
- Installed 2026-11-03: eslint 10.14.0, @eslint/js 10.0.1, typescript-eslint 8.72.1, @types/node 24 (TypeScript 5.9.3). Verified on Linux in a `node:24` container: same results as Windows.
- `npm audit --omit=dev`: 15 (12 low, 3 moderate), all via Colyseus's OAuth module (`grant` → `request-oauth` → `uuid` < 11.1.1). The only fix offered is downgrading to Colyseus 0.15. Accepted: auth is off and we never call `uuid` with `buf`. Re-check on each Colyseus update.

## Testable design rule
Put game rules in **pure functions** under `server/src/logic/` (no Colyseus imports), e.g.:
- `isMovePlausible(prev, next, dtMs, limits)`: rejects teleports and over-speed moves
- `safePathFromSeed(layout, seed)`: the same seed always gives the same path
- `decideWinner(players)`: the earliest server-timestamped finish wins; forfeit rules

Rooms call these functions. Tests import them directly. These are also the functions to break on purpose for Lab 6's Test B. Room-level tests (`@colyseus/testing`) stay few and only cover wiring.

## Why (portfolio + lab)
`node:test` is built into Node, which is what the handout uses. Plus lint, typecheck and test as separate CI gates: a reviewer can see three separate quality checks, and the Lab 6 paper can show which one caught a broken change.
