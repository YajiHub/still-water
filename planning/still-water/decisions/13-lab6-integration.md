# Lab 6 Integration

- **Status:** Locked (2026-11-02) (by: me). ⚠️ Instructor confirmation still to get (expected yes).
- **Type:** grill
- **Affects:** Phase 0b (CI/CD), Phase 4 (online), repo name, tags, README, both papers

## Question
Lab 6 (CI/CD) normally uses a sample Express app in its own repo. Should it use this game project instead, and how?

## Options considered
| Option | For | Against |
|---|---|---|
| **One repo; the Lab 6 pipeline deploys the game's `server/`** | Shared level data stays in sync; one set of AI context; Lab 6 adds tests + `/health` + a workflow to a server we're building anyway; strong portfolio story | Repo name and `v1.0` tag conflict between the two handouts; needs instructor OK |
| Separate repo for the server (Lab 6), game repo for Lab 5 | Matches both handouts' repo rules | Level data duplicated across repos; two contexts; more admin |
| Do Lab 6 with the sample app as written | Zero risk | Extra 6 hours that don't help the game |

## Decision
**One public repo.** The Lab 6 CI/CD pipeline lints, tests and deploys **`server/`** (the Colyseus game server) to an Ubuntu Server VM through a self-hosted runner.

Details:
- **Repo name (proposed):** `itsd85-lab5-6-<section>-<surname>`. Rename if the instructor prefers otherwise.
- **Tags/releases:** `lab5-v1.0` (APK + Lab 5 paper) and `lab6-v1.0` (Lab 6 paper).
- **Workflow:** `.github/workflows/ci-cd.yml` at the repo root, with jobs running in `server/`.
  **No workflow-level `paths:` filter.** The `protect-main` ruleset requires the `test` check, and a workflow
  that is skipped by a path filter never reports that check, which would block merging Unity-only PRs. The test job is
  fast, so it runs on every PR/push. Deploying on every push to `main` is also fine (it gives more push-to-live samples for the paper).
- **README:** one README with a Lab 5 section and a Lab 6 section (each with the headings its handout requires).
- **Two papers**, same project (`docs/paper-lab5.pdf`, `docs/paper-lab6.pdf`): Lab 5 = game design + playtest; Lab 6 = pipeline + lead time / change failure rate.
- **The VM doubles as the LAN game server** for Lab 5's same-Wi-Fi play (see [Hosting and LAN Plan](./12-hosting-and-lan.md)).

## Consequences
- From Phase 0b on, changes reach `main` through pull requests with a green `test` check (the human merges their own PRs).
- Game logic on the server must live in plain, decorator-free modules so it's easy to unit-test (see [Server Test and Lint Tooling](./14-server-test-tooling.md)).
- If the instructor says no: either split `server/` into its own repo (the game only needs the server URL), or do the sample app and add the pipeline to the game server later.

## Message to send the instructor
> "Can I use my Lab 5 game's Node.js server (Colyseus, TypeScript) as the app for Lab 6, in one repository with separate tags `lab5-v1.0` and `lab6-v1.0`? The pipeline would lint, test and deploy the server folder to my VM through a self-hosted runner."
