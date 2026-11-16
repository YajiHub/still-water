"""Still Water: merges a Quaternius hairstyle into each Quaternius body (CC0) so the player isn't bald.

    blender -b --factory-startup --python art/player/build_player_bodies.py -- <Quaternius "Universal Base Characters[Standard]" folder> client/Assets/_Project/Art/Player

The hairstyles ("rigged to head bone") come with their own copy of the skeleton. Here the hair mesh is moved onto the
body's armature (its "Head" vertex group drives it), the spare skeleton is deleted, and one FBX per player is exported:
PlayerFemale_FullBody.fbx (Hair_Long) and PlayerMale_FullBody.fbx (Hair_SimpleParted). Unity imports each as a single
Humanoid (PlayerImport); PlayerBodyBuilder assigns the materials by renderer name: the body, Eyes, Eyebrows, Hair.
"""
import os
import sys

import bpy

args = sys.argv[sys.argv.index("--") + 1:]
SRC, OUT = args[0], args[1]
BODIES = [("Female", "Superhero_Female_FullBody.fbx", "Hair_Long.fbx"),
          ("Male", "Superhero_Male_FullBody.fbx", "Hair_SimpleParted.fbx")]

for name, body_file, hair_file in BODIES:
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.fbx(filepath=os.path.join(SRC, "Base Characters", "Unity", body_file))
    body_arm = next(o for o in bpy.data.objects if o.type == "ARMATURE")
    before = set(bpy.data.objects)
    bpy.ops.import_scene.fbx(filepath=os.path.join(SRC, "Hairstyles", "Rigged to Head Bone", "FBX (Unity)", hair_file))
    new = set(bpy.data.objects) - before
    hair_arm = next(o for o in new if o.type == "ARMATURE")
    for hair in (o for o in new if o.type == "MESH"):
        world = hair.matrix_world.copy()
        hair.parent = body_arm
        hair.matrix_world = world
        for mod in hair.modifiers:
            if mod.type == "ARMATURE":
                mod.object = body_arm
        hair.name = hair.data.name = "Hair"
        print("HAIR", name, len(hair.data.polygons), "faces, groups", [g.name for g in hair.vertex_groups])
    bpy.data.objects.remove(hair_arm, do_unlink=True)
    for o in bpy.data.objects:
        o.select_set(True)
    path = os.path.join(OUT, f"Player{name}_FullBody.fbx")
    bpy.ops.export_scene.fbx(filepath=os.path.abspath(path), use_selection=True, object_types={"ARMATURE", "MESH"},
                             apply_scale_options="FBX_SCALE_ALL", add_leaf_bones=False, bake_anim=False,
                             mesh_smooth_type="FACE")
    print("EXPORTED", path, sorted(o.name for o in bpy.data.objects))
