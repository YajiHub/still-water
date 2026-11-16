# 2026-11-16: Original Listener, player bodies, music, co-op race circuit, settings, Render hosting

## Focus
A long iterative session, playtested between rounds by the user (Editor and phone). It replaced the Quaternius
Listener with an original Blender monster, gave the player a real body, added original music, turned online into a
2–4 player co-op race through all five rooms, added a Settings page, and put the server on the internet for free.
Merged to `main` as PR #20 (`6431b11`, plus the earlier Visual Pass / Level 5 commits) and PR #21 (`render.yaml` fix).

## What Was Built / Changed

**Listener**
- **The model:** an original Listener built entirely by `art/listener/build_listener.py` (Blender 5.3, headless; the
  `.blend` is saved next to it).
  - An eel tail with withered legs, a gaunt ribbed torso with a hump, ~4.5 m of arms with needle fingers and black
    claws, mantis arms, ragged skin flaps and hair.
  - A blank egg head that splits into two toothed halves around a lamprey throat.
  - Exported at 1.8×, about 7.6 m long.
- **Texture:** baked colour (smoothness in the alpha), normal and vein-emission maps. Sound pits glow and pulse with
  the AI state, and the vein glow follows the look.
- **Rig:** Generic, 54 bones, with procedural `Swim_Fwd_Loop`, `Swim_Idle_Loop` and `Zombie_Scratch` (the 2 s catch).
  `ListenerPrefabBuilder` puts the model under a root turned 180°, because a Generic rig keeps the FBX's −Z facing.
- **Behaviour:** `PlatformAvoidance` steers it round platforms, and it ducks under one only when its chest is inside
  it. For the last 4 m it goes straight at the splash.
- **The catch:** `CatchCamera` gives a low shot looking up; the Listener squares up to you and screams (`sfx_listener_scream`).

**Player**
- Quaternius CC0 female and male bodies with hair merged in Blender (`art/player/build_player_bodies.py`).
- *Build Player Bodies* puts them in `Resources/Player`. `PlayerAvatar` (added by `PlayerMotor`) animates them with
  `PlayerLook`, and shows only the shadow in first person.

**Music**
- 8 original tracks plus the scream, synthesized by `art/music/compose.py` (Blender's Python writes the OGG).
- `MusicDirector` plays the menu theme, the reveal music box, the ambience, a hunt layer that follows
  `ListenerAI.Threat`, and the catch / win / lose stingers.

**Menu and UI**
- *Build Menu Backdrop*: a dark poolroom with the Listener circling (`MenuListener`, `MenuAtmosphere`).
- `Gui` restyled and sized from the safe area. The title page is a left column.
- Touch controls rebuilt: glass discs with glowing rims, a PUSH button online.
- **Settings page** (decision 11): master/music volume, look sensitivity, button size, left-handed layout, camera,
  character. Removed: Reset progress, the Sound On/Off button, the Look A/C toggle.

**Online: the co-op race circuit** (decision 07 amendment + revision, SYSTEM_ATLAS §4.2)
- Server `circuit` room (`logic/circuit.ts`, `rooms/CircuitRoom.ts`). The client's `CircuitWorld` loads Level02–05
  additively, keeps each `Level` root, and opens doorways between them.
- **Rules:** 2–4 players; open doors (a race); a Listener per room with the solo tuning (`listenerTuning.json`); every
  catch shown to everyone; PUSH with knockback.
- **Points:** first to each door, finishing places, minus splashes and catches, plus a team bonus if everyone gets
  through in time.
- **Other fixes:** ferries now move relative to their room (`PlatformMover`). The client predicts the Listeners between
  the 10 Hz updates.

**Hosting**
- `render.yaml`: Render free tier, Singapore, Docker from `server/`, deploys `main` after the checks pass.
- The client wakes a sleeping `wss://` server (up to 90 s) and accepts `https://` URLs.
- Live at `https://still-water-server.onrender.com`. Hotspot play is described in `docs/SETUP.md` §8.

## Verification
- **Server:** `npm test` **175/175**; `npm run typecheck`, `npm run lint` and `npm run build` are clean.
- **Docker:** a local build with `PORT=10000` served `/health` and a 5-room circuit.
- **Live Render:** `/health` OK, and a real Colyseus client joined `circuit` over `wss://` in 0.9 s with the right
  Listener speeds.
- **Client:** the runtime, editor and EditMode test code compile (`dotnet build` of the Unity-generated projects, with
  the new files added). Unity batch mode isn't available here: no licence token.
- **Playtested by the user** (Editor/phone) between rounds: Listener facing, size and catch; the player bodies; the
  co-op race (rounds 1–2); online Listener speed, now "good".
- **Not run yet:** Unity EditMode Test Runner (new: `CircuitSnapshotTests`, `PlayerLookTests`,
  `ListenerTuningParityTests`), a phone match over Render, and the latest Listener / Settings changes on a phone.

## Mistakes & Gotchas
- **Generic rigs keep the FBX's facing:** Blender +Y becomes Unity −Z. The Quaternius body only looked right because a
  Humanoid avatar hides it.
- **Skinned mesh bounds:** a skinned mesh is culled by its rest-pose bounds. A big, rearing body needs
  `updateWhenOffscreen`.
- **Dive checks hide the Listener:** the water is opaque, so a wide "platform ahead" check hid it most of the time in
  the dense levels. Dive only when its chest is inside a platform.
- **PowerShell 5.1 `Get-Content` / `Set-Content`** mangled UTF-8 (`·` became `Â·`) and added BOMs. Repaired. Edit with
  Python or the Edit tool (saved as an agent memory).
- **Silent stingers:** the stinger AudioSource started at volume 0, so `PlayOneShot` was silent.
- **World-space ferry points** broke when the circuit moved rooms. They're now relative to the room.
- **Render:** `dockerfilePath` / `dockerContext` are read relative to `rootDir`, not the repo root (PR #21).
- **Push access:** this shell can't use the SSH key (`id_rsa_yaji`, not loaded). Pushes went once over HTTPS with the
  `gh` login, leaving the config untouched.
- **Auto-fix:** couldn't be enabled from the session ("repository could not be matched"). Use the PR bar.

## Decisions
- Listener: an original, non-humanoid Blender model; v2/v3 bigger and scarier (decision 06 amendments) (by: me).
- Player bodies: both, picked in the menu (now Settings) (by: me).
- Music composed in code (by: me).
- Online: 2–4 player co-op race; open doors; a Listener per room; per-player catches; pushing (decision 07) (by: me).
  Lobby, points and timer are agent defaults in `CIRCUIT_CONFIG`.
- Settings page; remove Reset progress and the Look A/C toggle (decision 11) (by: me).
- Free internet hosting on Render (by: me, "prepare Render").

## Next Step
1. In Unity: **Test Runner → EditMode → Run All**.
2. If not done since the last Listener change: *Build Listener Prefab*, then **Build And Run** on each phone.
3. A 2+ phone co-op run over Render (`https://still-water-server.onrender.com`). Check the push strength, control
   size, the catch camera, and fps in rooms 3–5.
4. Locally: `ssh-add ~/.ssh/id_rsa_yaji`, then `git switch main` and `git pull`.
