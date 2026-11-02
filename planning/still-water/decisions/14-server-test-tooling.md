# Server Test and Lint Tooling

- **Status:** Open
- **Type:** research
- **Blocks:** Phase 0b (CI/CD)

## Question
How do we lint and test a **TypeScript** Colyseus server in CI, staying close to the Lab 6 handout (which uses plain JS, `node:test` and ESLint)?

## Why it matters
The handout's commands (`npm ci`, `npm run lint`, `npm test`) must work in the `test` job. Colyseus state classes use **decorators** (`@type`), and Node's built-in TypeScript type stripping doesn't support them, so tests can't just run `.ts` files with plain `node --test`.

## Options
| Option | For | Against |
|---|---|---|
| **A. `node:test` + `tsx`** (`node --import tsx --test test/**/*.test.ts`) | Same test runner as the handout; tiny setup | One extra dev dependency |
| B. Compile with `tsc`, then `node --test dist/test/` | No extra runtime dependency; also proves the build compiles | Slower; two steps |
| C. Vitest | Nicer output, watch mode | Departs from the handout's `node:test` |

Lint: **ESLint + typescript-eslint** (flat config `eslint.config.js`), recommended rules.
Also add `npm run build` (`tsc --noEmit` or a full build) to CI so type errors fail the pipeline.

## Testable design rule (applies whatever the choice)
Put game rules in **pure functions** under `server/src/logic/` (no Colyseus imports, no decorators), e.g.:
- `isMovePlausible(prev, next, dtMs, limits)`: rejects teleports and over-speed moves
- `safePathFromSeed(layout, seed)`: the same seed always gives the same path
- `decideWinner(players)`: the earliest server-timestamped finish wins; forfeit rules
Rooms call these functions. Tests import them directly. These are also the functions to break on purpose for Lab 6's Test B.

## Recommendation
**A** (`node:test` + `tsx`) + ESLint/typescript-eslint + a `tsc --noEmit` step. Scripts: `lint`, `typecheck`, `test`, `build`, `start`.

## Decision
_(fill in when locked; verify the exact commands against the installed Colyseus template's package.json)_
