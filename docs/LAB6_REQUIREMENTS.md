# Lab 6 Requirements — ITSD 85 "Automated CI/CD from Local Dev to GitHub to Deployment Server"

> Condensed from the lab handout (dated Oct 20, 2026; 31 pages). Use this as the acceptance checklist.
> **In this project, the app that goes through the pipeline is the game's Colyseus server (`server/`),
> not the handout's sample Express app.** See [Lab 6 Integration](../planning/still-water/decisions/13-lab6-integration.md).
> We follow the handout as written and don't ask for exceptions (by: me, 2026-11-03). The shared repo and the `lab6-v1.0` tag are explained in the README and the paper.

## What the handout asks you to build
- A small Node.js web API **with automated tests** → ours: the game server + tests on its game logic
- A `Dockerfile` + `compose.yaml` that package and run it
- `.github/workflows/ci-cd.yml` with two jobs:
  - **test** on GitHub's free cloud runner (`ubuntu-latest`): install, **lint**, **test**
  - **deploy** on **your own Ubuntu Server VM** through a **self-hosted runner**: only after `test` passes on `main`
- A running deployment whose **`/health`** shows the **commit SHA** as the version

## Required tools (all free)
Git (SSH key to GitHub) · Node.js 24 LTS · VS Code / VSCodium · `node:test` · ESLint · GitHub Actions ·
self-hosted runner · VirtualBox · Ubuntu Server 24.04 LTS (or newer) · Docker Engine + Compose plugin.
VM: 2 vCPU, 2–4 GB RAM, 25 GB disk.

## Parts, mapped to this project

| Part | Handout | Our version | Done |
|---|---|---|---|
| 1 Local env (30 min) | Git, Node 24, editor, Git identity, **SSH key** → `ssh -T git@github.com` | Same. Switch the repo remote to SSH. *(CP1: screenshot of versions + ssh -T)* | [ ] |
| 2 App + tests (45 min) | Express app, `/`, `/health` (version from `APP_VERSION`), `node:test`, ESLint, Dockerfile, compose | Game server: add `/` status page + `/health` returning `{status, version}`; tests on `server/src/logic/`; ESLint for TS; Dockerfile + compose. *(CP2: tests passing + `/` showing "dev")* | [ ] |
| 3 Push to GitHub (20 min) | **Public** repo, fork-PR approval set to "Require approval for all external contributors" | Same repo as the game (public). Set the approval option. *(CP3: repo file list)* | [ ] |
| 4 CI (30 min) | `test` job: checkout, setup-node 24 (npm cache), `npm ci`, lint, test | Same, with `working-directory: server` and a TS build step. *(CP4: green test job)* | [ ] |
| 5 Server VM (60 min) | VirtualBox Ubuntu Server, port forwards (2222→22 ssh, 8080→8080 web), Docker, manual dry-run deploy | **VMware Workstation instead of VirtualBox** (already installed; same role: a local hypervisor, free for personal use). Same. The 8080 forward also carries the game WebSocket, so no extra forward is needed (see SYSTEM_ATLAS §8). *(CP5: hello-world + "manual" version in the browser)* | [ ] |
| 6 CD (60 min) | Self-hosted runner as a service, label `deploy`; `deploy` job: `needs: test`, main-push only, `concurrency`, `APP_VERSION=${{ github.sha }}`, compose up `--build`, health-check loop, image prune | Same. *(CP6: runner Idle, green test→deploy, browser shows the SHA matching GitHub)* | [ ] |
| 7 End-to-end (60 min) | Ruleset `protect-main` (PR required, status check `test`, block force push); **Test A** good change deploys; **Test B** broken change blocked; **Test C** rollback (Method 1 revert via PR; Method 2 `--no-build` with an old image tag); record push-to-live times | Test A: change the `/` status page text. Test B: break a game-logic function (e.g. winner decision) so a test fails. Test C: as in the handout. | [ ] |

Handout notes: action versions `actions/checkout@v7` and `actions/setup-node@v7` were current in Oct 2026 (bump if deprecated).
The registration token for the runner expires after about 1 hour.

## Deliverables
- Repo link (public; or add the instructor as a collaborator). The **Actions history must show**: ≥ 1 green test→deploy, ≥ 1 red test from Test B, and the Test C rollback run. **Do not delete workflow runs.**
- README with: What this project does · Pipeline diagram (`docs/pipeline.png`) · How to run locally · How the deployment works · Results (push-to-live times) · Screenshots (`docs/screenshots/cp1-versions.png`, …) · Paper (ours: `docs/paper-lab6.pdf`). *(In our shared README, this is the "Lab 6: CI/CD" section.)*
- Tag + Release. The handout says `v1.0`; ours is **`lab6-v1.0`** (see the integration decision), with the paper attached.
- No secrets committed: no `.env`, runner token or private SSH key (if one leaks: delete, rotate, tell the instructor).
- Live demo, 2 min: change the homepage text, merge, show it deployed without touching the server.
- Google Classroom: repo link, release link, paper PDF `ITSD85_Lab<no>_<Section>_<Surname>_Paper.pdf`, optional recording of Tests A–C; private comment: name, latest commit ID on `main`, anything unfinished.

## Results table (Part 7.5): fill in during testing
| Test | Expected | Actual | Push-to-live (min:sec) | Screenshot |
|---|---|---|---|---|
| A – good change (PR #3, `5bf732e`) | test ✓, deploy ✓, new text + SHA live | ✅ as expected | 1:13 | `cp-testA.png` |
| B – broken change (PR #4, `999b4ea`: `<=` → `>=` in `isMovePlausible`) | test ✗, merge blocked, server unchanged | ✅ `npm test` failed (4/9 `isMovePlausible` tests), merge blocked, deploy skipped, server stayed on `5bf732e`; PR closed unmerged | n/a | `cp-testB.png` |
| C – bad change shipped (PR #5, `0b946bb`: DEBUG banner, tests still green) | bad text live | ✅ banner live | 1:15 | `cp-testC-bad.png` |
| C – rollback (Method 1: revert PR #6, `1fc7ded`) | old content + new SHA live | ✅ banner gone, `1fc7ded` live | 1:02 (time to restore) | `cp-testC-m1.png` |
| C – rollback (Method 2: `--no-build` with `5bf732e`) | previous SHA live within seconds | ✅ `5bf732e` live, no CI involved | 0:00.48 (`time`) | `cp-testC-m2.png` |

For the paper, record **at least 5 deploys'** push-to-live times (merge → health check confirmed the new SHA).

| # | Commit | Change | Push-to-live |
|---|---|---|---|
| 1 | `1f9d0bf` | add deploy job (first deploy, cold Docker cache) | 4:22 (test start → deploy done) |
| 2 | `5bf732e` | Test A | 1:13 |
| 3 | `0b946bb` | Test C bad change | 1:15 |
| 4 | `1fc7ded` | Test C revert | 1:02 |
| 5 | this docs PR | results table | _fill in_ |

## Grading
Final = **50% lab work + 40% paper + 10% submission/GitHub practice.**

| Lab work rubric | Pts |
|---|---|
| Local setup & repo (Parts 1–3): SSH push works, clean repo, meaningful commits | 15 |
| CI workflow (Part 4): lint + tests on push and PR, green run shown | 20 |
| Server, Docker & runner (5–6.1): runner Idle after a VM reboot | 15 |
| Automated deployment (6.2–6.3): only after green tests, health check verifies the SHA | 20 |
| End-to-end tests (Part 7): A, B, C with timings, ruleset active | 15 |
| Lab report & guide questions | 10 |
| Live demo | 5 |

## Paper (separate from the Lab 5 paper)
IEEE conference format, **4–6 pages**, Times New Roman 10 pt, **≥ 10 references (≥ 5 from the last 5 years)**, Zotero, draw.io, LanguageTool.
Sections: Title · Abstract (150–250 words, at least one measured result) · Index Terms · I Introduction (problem, RQs) · II Related Work (5–8 sources) · III System Design (Fig. 1 pipeline, Table I free tools vs the paid tools they replace) · IV Implementation (Listing ≤ 20 lines of ci-cd.yml) · V Evaluation (Tests A/B/C, push-to-live over ≥ 5 deploys, Fig. 2 chart) · VI Discussion · VII Conclusion · Acknowledgment (state AI tool use) · References · Appendix (CP screenshots 1–6, results table, guide answers, links).
Suggested RQs: **RQ1** Can a fully free toolchain test and deploy automatically? **RQ2** Lead time (merge→live) and time to restore? **RQ3** Does the quality gate stop failing changes (change failure rate)?
Writing: third person, past tense; numbers with units and n; every figure and table captioned and referenced in the text.

## Guide questions (paper appendix)
1. CI vs Continuous Delivery vs Continuous Deployment: which did you build?
2. Why `needs: test`? What happens without it?
3. With no public IP, how could GitHub deploy to the server?
4. Why `npm ci` instead of `npm install`?
5. Why use the commit ID as the version? Give two benefits.
6. Rollback Method 1 vs Method 2: when would you choose each?
7. Security risks of a self-hosted runner on a public repo, and your mitigations.
8. Name one paid CI/CD or hosting product and the free tool that replaced it.
