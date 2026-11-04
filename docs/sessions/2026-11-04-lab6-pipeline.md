# Session 2026-11-03 → 11-04 — Lab 6 pipeline end to end

## Focus
Finish Phase 0 leftovers and complete Phase 0b (Lab 6 CI/CD): VM, self-hosted runner, deploy job, ruleset, Tests A/B/C.

## What Was Built / Changed
- **Colyseus SDK embedded** in `client/Packages/io.colyseus.sdk` (identical to `0bb05cb`, minus the broken `Runtime/Example/Schema` symlink) → Android builds work again; Third Person Playground runs on the phone (PR #1).
- **Camera Style** updated: first person + jump aids is the leading option, third person the fallback; still Open until the Phase 1 phone test.
- **VM:** VMware Workstation (instead of VirtualBox) + Ubuntu Server 26.04.1 LTS, user `deploy`, NAT on VMnet8 (`192.168.95.132`), forwards 2222→22 and 8080→8080, Docker; manual dry run OK. Firewall rule for 8080; the phone reaches `:8080`.
- **Self-hosted runner** `stillwater-vm` (label `deploy`) as a service. Installed in the home folder: work dir `~/_work/still-water/still-water/server`.
- **`deploy` job** in `.github/workflows/ci-cd.yml`: `needs: test`, push-to-main only, `concurrency`, `APP_VERSION=${{ github.sha }}`, compose up `--build`, health check that waits for **this** SHA, `docker image prune -f`. `.github/actionlint.yaml` declares the `deploy` label (PR #2).
- **Ruleset `protect-main`**: PR required, check `test`, no force pushes.
- **Tests A/B/C** (PRs #3–#6): new status-page tagline; `isMovePlausible` broken on purpose (blocked); DEBUG banner shipped then rolled back by revert PR and by `--no-build` with an old SHA.
- Results table, 5 deploy timings, screenshots, Phase 0b ✅ (PRs #7, #8).

## Verification
- `npm run lint` exit 0 · `npm run typecheck` exit 0 · `npm test` 13/13, exit 0 · actionlint exit 0.
- GitHub Actions on `main` (`ad9d11e`): success. Every deploy verified via the API + `/health` showing the merge SHA.
- Timings (merge → health check): 4:22 (cold), 1:13, 1:15, 1:02, 1:11. Restore: Method 1 1:02, Method 2 0.48 s.
- Test B: `npm test` failed (4/9 `isMovePlausible` tests), merge blocked, deploy skipped.
- Ownership check: 2 questions (pipeline machine order: had it reversed; VM down during a merge: mostly right).

## Mistakes & Gotchas
- The Colyseus SDK's `upm` branch ships a **symlink**; on Windows it becomes a plain file with no `.meta` → "immutable folder" error that fails the build. I first mislabeled that red message as a warning.
- Unity's build report (`Library/LastBuild.buildreport`) had the real message: `Failed to process scene before export`.
- actionlint rejects custom runner labels unless they're listed in `.github/actionlint.yaml`.
- Runner installed in `~` instead of `~/actions-runner`, so paths differ from the handout.
- The VMware console for Ubuntu Server has no clipboard → use `ssh -p 2222 deploy@localhost` from Windows Terminal.
- `temp/` is git-ignored → screenshots must go to `docs/screenshots/`.
- Docker images are ~522 MB each on a 25 GB disk; old SHAs will need pruning eventually.
- An empty `client/Assets/Resources/` folder exists; its `.meta` is deliberately left untracked.

## Decisions
- VMware Workstation instead of VirtualBox; Ubuntu 26.04.1 (handout: 24.04 or newer). Deviations noted in LAB6_REQUIREMENTS.
- The agent may `git add`/`git commit`; the user pushes and merges (by: me).
- Camera Style: first person leads, decided by the Phase 1 prototype.

## Next Step
`/skill:execute-phase Phase 1` — grey-box Level 1, movement feel, first vs third person test on the phone.
Leftovers: screenshots CP1, CP3, CP4, CP6, Test A, C-bad (save directly to `docs/screenshots/`); Checkpoint 1 (GDD + paper-prototype photo). Keep the VM running whenever you merge to `main`.
