# Player Controller Architecture

- **Status:** Locked (2026-11-04) (by: me)
- **Type:** grill (after research)
- **Affects:** Phase 1 (feel prototype, camera test), Phase 2 (swim/catch), Phase 4 (online: same motor, positions synced)
- **Amends:** [Control Scheme](./02-control-scheme.md): its "built on Starter Assets – Third Person" now means *input and art only*

## Question
How do we build movement so first person and third person can be tested with the same jump feel, and the jump aids live in one place?

## Research (Starter Assets v2.0.2, `client/Assets/Starter Assets/Runtime/`)
- `FirstPersonController.cs` (267 lines) and `ThirdPersonController.cs` (391 lines) each contain their own copy of move + jump + gravity + camera.
- Missing aids: no coyote time (jump only while `Grounded`), no jump buffer (airborne code sets `_input.jump = false`), full air control, no variable jump height, no swim.
- Numbers are public fields per controller, not a shared config.
- Reusable as-is: `StarterAssetsInputs` (move/look/jump/sprint values), the touch UI (`UI_TouchScreenInput`, `TouchscreenInput`, safe-area aware), the robot model/animator, camera prefabs.
- Unity Test Framework 1.6.0 is already installed (`Packages/manifest.json`).

## Options
| Option | For | Against |
|---|---|---|
| **Own `PlayerMotor` + swappable camera rigs** | One copy of every aid; fair camera test; numbers in a ScriptableObject; rules testable; Starter Assets stays untouched | Most work up front (~1 day) |
| Edit both Starter controllers | Fastest start | Every aid written twice; edits inside a third-party package; fields instead of config |
| Copy one controller into `_Project` | Less duplication at first | Camera test no longer apples-to-apples |

## Decision
**Our own motor, separate camera rigs; Starter Assets for input and art only.**

| Piece | Folder | Role |
|---|---|---|
| `MovementConfig` (ScriptableObject) | `_Project/Scripts/Player/`, asset in `_Project/Configs/` | Every DESIGN §3.2–3.3 number (speeds, jump height, gravity, coyote, buffer, air control, swim) |
| `JumpRules` (plain C#, no `MonoBehaviour`) | `_Project/Scripts/Player/` | Coyote time, jump buffer, variable jump height: pure functions of time + inputs, so EditMode tests can call them |
| `PlayerMotor` (`MonoBehaviour`, `CharacterController`) | `_Project/Scripts/Player/` | Reads `StarterAssetsInputs`, applies `JumpRules`, air control, gravity; states Grounded / Airborne / Swimming |
| `FirstPersonLook` / `ThirdPersonLook` (own orbit script; Cinemachine dropped 2026-11-04 so both rigs read input identically) | `_Project/Scripts/Player/` + prefabs | Swappable; the motor moves relative to whichever camera is active |
| Assemblies | `_Project/Scripts/StillWater.asmdef` (refs `Unity.StarterAssets`, `Unity.InputSystem`, `ColyseusSDK`, `Unity.Cinemachine`); `_Project/Tests/EditMode/StillWater.Tests.EditMode.asmdef` | Needed for EditMode tests; `NetTest.cs` moves into this assembly too |

- Starter Assets files are **not edited**. Our prefab reuses its input component, touch UI and robot model.
- Test tool: **Unity Test Framework EditMode** for `JumpRules` (and later other pure rules). Run from Window → General → Test Runner; a CLI batch run can come later.

## Why
The camera test only means something if both cameras share identical movement. One motor also becomes the online player in Phase 4 and the base for the Ghost rival. Pure, tested jump rules mirror the server's `server/src/logic/` pattern.

## Consequences
- Phase 1 starts with ~1 day of controller work before the camera comparison.
- Update DESIGN §3.1 "Base" and SYSTEM_ATLAS §3 `PlayerMovement` → `PlayerMotor` (done with this decision).
