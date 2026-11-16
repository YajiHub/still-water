# The Listener: source

Original model, generated entirely by [`build_listener.py`](build_listener.py) (Blender 5.3, headless). No hand sculpting.
`Listener.blend` is the saved result, for looking at or tweaking in Blender.

Rebuild after editing the script (run from the repo root):

```bash
"C:/Program Files/Blender Foundation/Blender 5.3/blender.exe" -b --factory-startup --python art/listener/build_listener.py -- client/Assets/_Project/Art/Listener/Listener_Custom.fbx art/listener/Listener.blend [preview_dir]
```

It writes `T_Listener_Skin.png` next to the FBX. It takes about 1 minute (most of it the Cycles bake).

Then in Unity: *Still Water → Art Spike → Build Listener Prefab*. The prefab keeps its GUID, so every level picks it up.

- **Frame:** faces +Y, up +Z, chest at the origin, metres. FBX exported with Blender's default axes, the same round
  trip as the Quaternius files.
- **Rig:** Generic, 54 bones. `Root` (moved only by the lunge), spine `Hips…Head`, `Jaw_L/R` (the head's two halves),
  arms with four fingers each, `SmallArm_*` (mantis arms), `Tail1…8`, `Leg_*` / `Foot_*` (fused to the tail).
  `ListenerAI` finds the bone named `Chest`.
- **Scale:** modelled at 1, exported ×1.5 (`SCALE`): ~6.3 m nose to tail.
- **Mesh:** ~19.5k tris, 5 material slots (the order Unity follows):
  0. `Listener_Skin`, using `T_Listener_Skin.png` (1024², baked from a procedural shader, written next to the FBX).
     ListenerAI's look glow edits this slot.
  1. `Listener_Mouth`: the inside of the head, the claws.
  2. `Listener_Glow`: the sound pits. ListenerAI pulses it by state.
  3. `Listener_Throat`: the ember.
  4. `Listener_Hair`.
- **Clips:** `Swim_Fwd_Loop` (48 f), `Swim_Idle_Loop` (90 f), `Zombie_Scratch` (60 f, the catch; played at speed 1
  under the catch camera), all at 30 fps. The names match `ListenerLook`.
- With a preview folder given, it also renders Workbench PNGs and three dark EEVEE shots (the glow in the dark).
