"""Still Water: the two player bodies, dressed for the poolrooms (horror look, 2026-11-17).

    blender -b --factory-startup --python art/player/build_player_bodies.py -- <Quaternius "Universal Base Characters[Standard]" folder> client/Assets/_Project/Art/Player [preview_dir]

Starts from the Quaternius base bodies (CC0), which are bare skin and underwear, and for each one:
1. Merges a Quaternius hairstyle onto the body's skeleton ("rigged to head bone": the hair is moved onto the body's
   armature, its "Head" vertex group drives it, the spare skeleton is deleted). Female: Hair_Long. Male: Hair_SimpleParted.
2. Makes clothes from the body's own mesh: the faces under the garment are copied, pushed out a centimetre along their
   normals, and cut at the hems/cuffs with noise so the edges are ragged. They keep the body's bone weights, so they bend
   with every animation, and its UVs, so they bake into the same layout. One "Clothes" mesh per body (top + trousers).
   Female: an oxblood long-sleeve thermal torn near the wrists, dark jeans ripped at mid-calf.
   Male: a stained off-white T-shirt with torn short sleeves, dark work trousers frayed at the ankle. Both barefoot.
3. Bakes new textures (Cycles) into the output folder, smoothness in the alpha:
   T_Player<Name>_Skin.png        the original skin made pale and cold: dark-ringed eyes, grime (worse on the legs),
                                  bruises, scratches; wet below the knee.
   T_Player<Name>_Clothes.png     fabric, stains, mildew; soaked and dark from the shins down, splashes above.
   T_Player<Name>_Clothes_Normal.png  weave and creases.
4. Exports Player<Name>_FullBody.fbx (armature + body, Eyes, Eyebrows, Hair, Clothes). Unity imports it as one Humanoid
   (PlayerImport); PlayerBodyBuilder assigns the materials by renderer name.
With a preview_dir it also renders front, back and mid-run views (UAL1 jog) of each body there, to check the look.
"""
import math
import os
import sys

import bmesh
import bpy
import numpy as np
from mathutils import Vector, noise

args = sys.argv[sys.argv.index("--") + 1:]
SRC, OUT = args[0], os.path.abspath(args[1])
PREVIEW = os.path.abspath(args[2]) if len(args) > 2 else None
HERE = os.path.dirname(os.path.abspath(__file__))
UAL1 = os.path.abspath(os.path.join(HERE, "..", "..", "client", "Assets", "_Project", "Art", "Listener", "UAL1_Standard.fbx"))

SKIN_SIZE, CLOTH_SIZE, NORMAL_SIZE = 2048, 2048, 1024

# Per body: the hairstyle and the outfit. Colours are linear RGB. Cut values are fractions along a bone (0 at its
# joint, 1 at the next joint): sleeve_upper on the upper arm (short sleeves), sleeve_fore on the forearm (long
# sleeves; 0 = none), leg_calf on the shin.
BODIES = [
    dict(name="Female", body="Superhero_Female_FullBody.fbx", hair="Hair_Long.fbx",
         top=(0.075, 0.012, 0.012), pants=(0.020, 0.026, 0.042), hair_tint=(0.16, 0.12, 0.10),
         sleeve_upper=1.0, sleeve_fore=0.80, leg_calf=0.50, top_off=0.013, flare=0.004, seed=3),
    dict(name="Male", body="Superhero_Male_FullBody.fbx", hair="Hair_SimpleParted.fbx",
         top=(0.30, 0.27, 0.20), pants=(0.035, 0.030, 0.022), hair_tint=(0.12, 0.10, 0.09),
         sleeve_upper=0.45, sleeve_fore=0.0, leg_calf=0.92, top_off=0.018, flare=0.022, seed=11),
]
# Metres the garments sit off the skin: the top (top_off above) over the trousers' waistband; the trousers widen by up
# to `flare` towards the ankle so they hang like cloth instead of leggings.
PANTS_OFFSET = 0.008
MIN_ISLAND = 40                           # faces: smaller stray bits of garment are dropped


# ---------------------------------------------------------------- import + hair

def import_body(cfg):
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.fbx(filepath=os.path.join(SRC, "Base Characters", "Unity", cfg["body"]))
    arm = next(o for o in bpy.data.objects if o.type == "ARMATURE")
    before = set(bpy.data.objects)
    bpy.ops.import_scene.fbx(filepath=os.path.join(SRC, "Hairstyles", "Rigged to Head Bone", "FBX (Unity)", cfg["hair"]))
    new = set(bpy.data.objects) - before
    hair_arm = next(o for o in new if o.type == "ARMATURE")
    for hair in (o for o in new if o.type == "MESH"):
        world = hair.matrix_world.copy()
        hair.parent = arm
        hair.matrix_world = world
        for mod in hair.modifiers:
            if mod.type == "ARMATURE":
                mod.object = arm
        hair.name = hair.data.name = "Hair"
    bpy.data.objects.remove(hair_arm, do_unlink=True)
    body = max((o for o in bpy.data.objects if o.type == "MESH"), key=lambda o: len(o.vertex_groups))
    use_uv(body)
    return arm, body


def use_uv(obj, name="UVMap"):
    uv = obj.data.uv_layers[name]
    obj.data.uv_layers.active = uv
    uv.active_render = True


def joints(arm):
    """World positions of each bone's head (its joint)."""
    mw = arm.matrix_world
    return {b.name: mw @ b.head_local for b in arm.data.bones}


# ---------------------------------------------------------------- clothes

def along(p, a, b):
    """How far p is from joint a towards joint b (0 at a, 1 at b)."""
    ab = b - a
    return (p - a).dot(ab) / max(ab.length_squared, 1e-9)


def rag(p, seed, amp):
    """Ragged-edge noise: smooth, but with a torn-looking high-frequency part."""
    q = p * 18.0 + Vector((seed, seed * 0.37, 0))
    return amp * (0.7 * noise.noise(q) + 0.3 * noise.noise(q * 3.7))


def garment(body, arm, cfg, piece):
    """A copy of the body cut down to the top or the trousers, pushed off the skin."""
    J = joints(arm)
    names = {g.index: g.name for g in body.vertex_groups}
    mw = body.matrix_world
    mw_inv, rot = mw.inverted(), mw.to_3x3()
    hem_z = J["pelvis"].z + 0.01        # the top's hem, over the trousers' waistband
    waist_z = J["pelvis"].z + 0.09      # the trousers' waist
    neck_z = J["neck_01"].z + 0.015     # crew neck
    seed = cfg["seed"] + (0 if piece == "top" else 50)

    def keep(v, p):
        if not v.groups:
            return False
        dom = names[max(v.groups, key=lambda g: g.weight).group]
        side = dom[-1]
        if piece == "top":
            if dom == "neck_01":
                return p.z < neck_z + rag(p, seed, 0.006)
            if dom.startswith(("spine", "clavicle")):
                return p.z > hem_z + rag(p, seed, 0.03)
            if dom == "pelvis":
                return p.z > hem_z + rag(p, seed, 0.03)
            if dom.startswith("upperarm"):
                if cfg["sleeve_fore"] > 0:
                    return True
                t = along(p, J[f"upperarm_{side}"], J[f"lowerarm_{side}"])
                return t < cfg["sleeve_upper"] + rag(p, seed, 0.09)
            if dom.startswith("lowerarm") and cfg["sleeve_fore"] > 0:
                t = along(p, J[f"lowerarm_{side}"], J[f"hand_{side}"])
                return t < cfg["sleeve_fore"] + rag(p, seed, 0.08)
            return False
        # trousers
        if dom in ("pelvis", "spine_01"):
            return p.z < waist_z + rag(p, seed, 0.008)
        if dom.startswith("thigh"):
            return True
        if dom.startswith("calf"):
            t = along(p, J[f"calf_{side}"], J[f"foot_{side}"])
            return t < cfg["leg_calf"] + rag(p, seed, 0.07)
        return False

    obj = body.copy()
    obj.data = body.data.copy()
    obj.name = obj.data.name = f"Clothes_{piece}"
    body.users_collection[0].objects.link(obj)

    me = obj.data
    keep_v = [keep(v, mw @ v.co) for v in me.vertices]
    ankle_z = J["foot_l"].z
    bm = bmesh.new()
    bm.from_mesh(me)
    bm.verts.ensure_lookup_table()
    bm.normal_update()
    for v in bm.verts:
        p = mw @ v.co
        if piece == "top":
            off = cfg["top_off"]
        else:
            down = min(max((waist_z - p.z) / max(waist_z - ankle_z, 1e-6), 0.0), 1.0)
            off = PANTS_OFFSET + cfg["flare"] * down * down
        n = (rot @ v.normal).normalized()
        v.co = mw_inv @ (p + n * off)
    bmesh.ops.delete(bm, geom=[f for f in bm.faces if not all(keep_v[v.index] for v in f.verts)], context="FACES")
    bmesh.ops.delete(bm, geom=[v for v in bm.verts if not v.link_faces], context="VERTS")
    drop_small_islands(bm)
    bm.to_mesh(me)
    bm.free()
    attr = me.attributes.new("piece", "FLOAT", "POINT")
    attr.data.foreach_set("value", [1.0 if piece == "top" else 0.0] * len(me.vertices))
    print("GARMENT", cfg["name"], piece, len(me.polygons), "faces")
    return obj


def drop_small_islands(bm):
    bm.faces.ensure_lookup_table()
    seen, small = set(), []
    for f in bm.faces:
        if f.index in seen:
            continue
        island, stack = [], [f]
        seen.add(f.index)
        while stack:
            g = stack.pop()
            island.append(g)
            for e in g.edges:
                for h in e.link_faces:
                    if h.index not in seen:
                        seen.add(h.index)
                        stack.append(h)
        if len(island) < MIN_ISLAND:
            small.extend(island)
    if small:
        bmesh.ops.delete(bm, geom=small, context="FACES")
        bmesh.ops.delete(bm, geom=[v for v in bm.verts if not v.link_faces], context="VERTS")


def dress(body, arm, cfg):
    parts = [garment(body, arm, cfg, "top"), garment(body, arm, cfg, "pants")]
    bpy.ops.object.select_all(action="DESELECT")
    for p in parts:
        p.select_set(True)
    bpy.context.view_layer.objects.active = parts[0]
    bpy.ops.object.join()
    clothes = bpy.context.view_layer.objects.active
    clothes.name = clothes.data.name = "Clothes"
    use_uv(clothes)
    return clothes


# ---------------------------------------------------------------- baking

class Nodes:
    """Small helper for building shader node trees."""

    def __init__(self, mat):
        mat.use_nodes = True
        self.t = mat.node_tree
        self.t.nodes.clear()

    def n(self, kind, **props):
        node = self.t.nodes.new(kind)
        for k, v in props.items():
            if k.startswith("in_"):
                node.inputs[int(k[3:])].default_value = v
            else:
                setattr(node, k, v)
        return node

    def link(self, a, b):
        self.t.links.new(a, b)

    def value(self, v):
        node = self.n("ShaderNodeValue")
        node.outputs[0].default_value = v
        return node.outputs[0]

    def math(self, op, a, b=None, clamp=False):
        node = self.n("ShaderNodeMath", operation=op, use_clamp=clamp)
        for i, x in enumerate((a, b)):
            if x is None:
                continue
            if isinstance(x, (int, float)):
                node.inputs[i].default_value = x
            else:
                self.link(x, node.inputs[i])
        return node.outputs[0]

    def mix(self, a, b, fac, blend="MIX"):
        node = self.n("ShaderNodeMix", data_type="RGBA", blend_type=blend)
        for sock, x in ((node.inputs[0], fac), (node.inputs[6], a), (node.inputs[7], b)):
            if isinstance(x, (int, float)):
                sock.default_value = x
            elif isinstance(x, tuple):
                sock.default_value = (*x, 1.0)
            else:
                self.link(x, sock)
        return node.outputs[2]

    def noise(self, pos, scale, detail=4.0, rough=0.55, distort=0.0):
        node = self.n("ShaderNodeTexNoise", in_2=scale, in_3=detail, in_4=rough)
        node.inputs["Distortion"].default_value = distort
        self.link(pos, node.inputs["Vector"])
        return node.outputs["Fac"]

    def ramp(self, x, lo, hi):
        """0 below lo, 1 above hi, smooth between."""
        node = self.n("ShaderNodeMapRange", interpolation_type="SMOOTHSTEP", in_1=lo, in_2=hi, in_3=0.0, in_4=1.0)
        self.link(x, node.inputs[0])
        return node.outputs[0]


def new_image(name, size, non_color=False):
    img = bpy.data.images.new(name, size, size, alpha=True)
    if non_color:
        img.colorspace_settings.name = "Non-Color"
    return img


def bake_setup():
    sc = bpy.context.scene
    sc.render.engine = "CYCLES"
    sc.cycles.device = "CPU"
    sc.cycles.samples = 4
    sc.render.bake.margin = 12
    sc.render.bake.use_clear = True


def bake_into(obj, tree, out_socket, img, kind="EMIT"):
    """Bake `out_socket` (a colour/value for EMIT, a BSDF for NORMAL) of obj's material into img."""
    t = tree.t
    out = next(n for n in t.nodes if n.type == "OUTPUT_MATERIAL")
    if kind == "EMIT":
        em = tree.n("ShaderNodeEmission")
        tree.link(out_socket, em.inputs[0])
        tree.link(em.outputs[0], out.inputs[0])
    else:
        tree.link(out_socket, out.inputs[0])
    tex = tree.n("ShaderNodeTexImage", image=img)
    t.nodes.active = tex
    bpy.ops.object.select_all(action="DESELECT")
    obj.select_set(True)
    bpy.context.view_layer.objects.active = obj
    bpy.ops.object.bake(type=kind)
    t.nodes.remove(tex)


def save_rgba(color_img, smooth_img, path):
    """colour RGB + smoothness as alpha, as one PNG."""
    w, h = color_img.size
    c = np.empty(w * h * 4, np.float32)
    s = np.empty(w * h * 4, np.float32)
    color_img.pixels.foreach_get(c)
    smooth_img.pixels.foreach_get(s)
    c[3::4] = s[0::4]
    out = bpy.data.images.new(os.path.basename(path), w, h, alpha=True)
    out.pixels.foreach_set(c)
    out.filepath_raw = path
    out.file_format = "PNG"
    out.save()
    return out


def bake_skin(body, cfg, eyes):
    """The original skin made pale and cold, with dark eye sockets, grime, bruises, scratches; wet legs."""
    src = bpy.data.images.load(os.path.join(OUT, f"T_Superhero_{cfg['name']}_Light_BaseColor.png"))
    mat = bpy.data.materials.new(f"Bake_Skin_{cfg['name']}")
    body.data.materials.clear()
    body.data.materials.append(mat)
    T = Nodes(mat)
    T.n("ShaderNodeOutputMaterial")
    uv = T.n("ShaderNodeUVMap", uv_map="UVMap")
    tex = T.n("ShaderNodeTexImage", image=src)
    T.link(uv.outputs[0], tex.inputs[0])
    pos = T.n("ShaderNodeNewGeometry").outputs["Position"]
    sep = T.n("ShaderNodeSeparateXYZ")
    T.link(pos, sep.inputs[0])
    z = sep.outputs[2]

    # Pale and cold: mostly desaturated, tinted towards grey-blue, a touch lighter.
    grey = T.n("ShaderNodeRGBToBW")
    T.link(tex.outputs[0], grey.inputs[0])
    cold = T.mix(grey.outputs[0], (0.80, 0.86, 0.90), 1.0, "MULTIPLY")
    c = T.mix(tex.outputs[0], cold, 0.7)
    c = T.mix(c, (1.18, 1.18, 1.18), 1.0, "MULTIPLY")

    # Dark, sunken eyes: a soft purple-grey shadow around each eye.
    for centre in eyes:
        ring = T.n("ShaderNodeVectorMath", operation="DISTANCE")
        T.link(pos, ring.inputs[0])
        ring.inputs[1].default_value = centre + Vector((0, 0, -0.005))
        shadow = T.math("SUBTRACT", 1.0, T.ramp(ring.outputs["Value"], 0.016, 0.034))
        c = T.mix(c, (0.30, 0.22, 0.30), T.math("MULTIPLY", shadow, 0.75), "MULTIPLY")

    # Grime: brown-green blotches, much heavier on the shins and feet.
    legs = T.math("SUBTRACT", 1.0, T.ramp(z, 0.10, 0.62))
    grime = T.ramp(T.noise(pos, 22.0, 6.0, 0.6), 0.48, 0.66)
    grime = T.math("MULTIPLY", grime, T.math("ADD", 0.25, T.math("MULTIPLY", legs, 0.75)), clamp=True)
    c = T.mix(c, (0.42, 0.40, 0.30), T.math("MULTIPLY", grime, 0.9), "MULTIPLY")

    # Bruises: a few soft purple-yellow patches.
    bruise = T.ramp(T.noise(pos, 5.0, 2.0, 0.5, 0.3), 0.66, 0.74)
    c = T.mix(c, (0.55, 0.38, 0.52), T.math("MULTIPLY", bruise, 0.7), "MULTIPLY")

    # Scratches: thin cell edges, only where a sparse mask allows.
    vor = T.n("ShaderNodeTexVoronoi", feature="DISTANCE_TO_EDGE", in_2=38.0)
    T.link(pos, vor.inputs["Vector"])
    line = T.math("SUBTRACT", 1.0, T.ramp(vor.outputs["Distance"], 0.0, 0.035))
    where = T.ramp(T.noise(pos, 4.0, 1.0, 0.5), 0.62, 0.68)
    scratch = T.math("MULTIPLY", line, where)
    c = T.mix(c, (0.40, 0.08, 0.08), T.math("MULTIPLY", scratch, 0.8), "MULTIPLY")

    # Smoothness: clammy skin, wet from the knees down.
    smooth = T.math("ADD", 0.38, T.math("MULTIPLY", legs, 0.32))

    bake_setup()
    col = new_image("skin_col", SKIN_SIZE)
    bake_into(body, T, c, col)
    sm = new_image("skin_sm", SKIN_SIZE, non_color=True)
    smv = T.n("ShaderNodeCombineColor")
    for i in range(3):
        T.link(smooth, smv.inputs[i])
    bake_into(body, T, smv.outputs[0], sm)
    return save_rgba(col, sm, os.path.join(OUT, f"T_Player{cfg['name']}_Skin.png"))


def bake_clothes(clothes, cfg):
    """Fabric, stains, mildew; soaked from the shins down with splashes above; weave + creases in the normal map."""
    mat = bpy.data.materials.new(f"Bake_Clothes_{cfg['name']}")
    clothes.data.materials.clear()
    clothes.data.materials.append(mat)
    T = Nodes(mat)
    T.n("ShaderNodeOutputMaterial")
    pos = T.n("ShaderNodeNewGeometry").outputs["Position"]
    sep = T.n("ShaderNodeSeparateXYZ")
    T.link(pos, sep.inputs[0])
    z = sep.outputs[2]
    piece = T.n("ShaderNodeAttribute", attribute_name="piece").outputs["Fac"]

    c = T.mix(cfg["pants"], cfg["top"], piece)
    # Fabric: fine fibre noise, a little lighter/darker everywhere.
    fibre = T.noise(pos, 260.0, 3.0, 0.6)
    c = T.mix(c, (1.25, 1.25, 1.25), T.math("MULTIPLY", T.math("SUBTRACT", fibre, 0.5), 0.6), "MULTIPLY")
    # Worn, faded seams/edges: big soft fade.
    fade = T.ramp(T.noise(pos, 3.0, 2.0, 0.5), 0.55, 0.8)
    c = T.mix(c, (0.38, 0.36, 0.33), T.math("MULTIPLY", fade, 0.25))
    # Stains: brownish blotches.
    stain = T.ramp(T.noise(pos, 7.0, 5.0, 0.6, 0.4), 0.56, 0.70)
    c = T.mix(c, (0.50, 0.42, 0.30), T.math("MULTIPLY", stain, 0.9), "MULTIPLY")
    # Mildew: dark green-black specks gathered in patches.
    patch = T.ramp(T.noise(pos, 5.0, 1.0, 0.5), 0.55, 0.65)
    speck = T.ramp(T.noise(pos, 70.0, 2.0, 0.5), 0.58, 0.64)
    mildew = T.math("MULTIPLY", patch, speck)
    c = T.mix(c, (0.20, 0.26, 0.18), T.math("MULTIPLY", mildew, 0.85), "MULTIPLY")
    # Old dried blood, faint, on the top only.
    blood = T.math("MULTIPLY", T.ramp(T.noise(pos, 9.0, 4.0, 0.6, 0.8), 0.70, 0.78), piece)
    c = T.mix(c, (0.35, 0.05, 0.04), T.math("MULTIPLY", blood, 0.6), "MULTIPLY")

    # Wet: soaked below a wavy waterline around the shins, plus splashes higher up.
    line = T.math("ADD", 0.55, T.math("MULTIPLY", T.math("SUBTRACT", T.noise(pos, 4.0, 2.0, 0.5), 0.5), 0.25))
    soaked = T.math("SUBTRACT", 1.0, T.ramp(T.math("SUBTRACT", z, line), -0.04, 0.06))
    splash = T.ramp(T.noise(pos, 11.0, 3.0, 0.6, 0.5), 0.62, 0.70)
    wet = T.math("MAXIMUM", soaked, T.math("MULTIPLY", splash, 0.6))
    c = T.mix(c, (0.45, 0.45, 0.45), wet, "MULTIPLY")
    smooth = T.math("ADD", 0.15, T.math("MULTIPLY", wet, 0.45))

    bake_setup()
    col = new_image("cloth_col", CLOTH_SIZE)
    bake_into(clothes, T, c, col)
    sm = new_image("cloth_sm", CLOTH_SIZE, non_color=True)
    smv = T.n("ShaderNodeCombineColor")
    for i in range(3):
        T.link(smooth, smv.inputs[i])
    bake_into(clothes, T, smv.outputs[0], sm)
    rgba = save_rgba(col, sm, os.path.join(OUT, f"T_Player{cfg['name']}_Clothes.png"))

    # Normal: weave + broad creases (wave bands, distorted).
    weave = T.noise(pos, 420.0, 2.0, 0.5)
    wave = T.n("ShaderNodeTexWave", wave_type="BANDS", in_1=7.0, in_2=9.0, in_3=2.0)
    T.link(pos, wave.inputs["Vector"])
    height = T.math("ADD", T.math("MULTIPLY", weave, 0.4), T.math("MULTIPLY", wave.outputs["Fac"], 0.6))
    bump = T.n("ShaderNodeBump", in_0=0.35, in_1=0.004)
    T.link(height, bump.inputs["Height"])
    bsdf = T.n("ShaderNodeBsdfPrincipled")
    T.link(bump.outputs[0], bsdf.inputs["Normal"])
    nrm = new_image("cloth_n", NORMAL_SIZE, non_color=True)
    bake_into(clothes, T, bsdf.outputs[0], nrm, kind="NORMAL")
    nrm.filepath_raw = os.path.join(OUT, f"T_Player{cfg['name']}_Clothes_Normal.png")
    nrm.file_format = "PNG"
    nrm.save()
    return rgba, nrm


def eye_centres():
    eyes = bpy.data.objects.get("Eyes")
    pts = [eyes.matrix_world @ v.co for v in eyes.data.vertices]
    left = [p for p in pts if p.x > 0]
    right = [p for p in pts if p.x <= 0]
    return [sum(s, Vector()) / len(s) for s in (left, right) if s]


# ---------------------------------------------------------------- export + preview

def export(cfg):
    bpy.ops.object.select_all(action="DESELECT")
    for o in bpy.data.objects:
        if o.type in ("ARMATURE", "MESH"):
            o.select_set(True)
    path = os.path.join(OUT, f"Player{cfg['name']}_FullBody.fbx")
    bpy.ops.export_scene.fbx(filepath=path, use_selection=True, object_types={"ARMATURE", "MESH"},
                             apply_scale_options="FBX_SCALE_ALL", add_leaf_bones=False, bake_anim=False,
                             mesh_smooth_type="FACE")
    print("EXPORTED", path, sorted(o.name for o in bpy.data.objects if o.type in ("ARMATURE", "MESH")))


def preview_material(name, img, tint=(1, 1, 1), smooth_from_alpha=True, smooth=0.4, normal=None):
    mat = bpy.data.materials.new(name)
    T = Nodes(mat)
    out = T.n("ShaderNodeOutputMaterial")
    bsdf = T.n("ShaderNodeBsdfPrincipled")
    T.link(bsdf.outputs[0], out.inputs[0])
    uv = T.n("ShaderNodeUVMap", uv_map="UVMap")
    tex = T.n("ShaderNodeTexImage", image=img)
    T.link(uv.outputs[0], tex.inputs[0])
    T.link(T.mix(tex.outputs[0], tint, 1.0, "MULTIPLY"), bsdf.inputs["Base Color"])
    if smooth_from_alpha:
        T.link(T.math("SUBTRACT", 1.0, tex.outputs["Alpha"]), bsdf.inputs["Roughness"])
    else:
        bsdf.inputs["Roughness"].default_value = 1.0 - smooth
    if normal is not None:
        nt = T.n("ShaderNodeTexImage", image=normal)
        T.link(uv.outputs[0], nt.inputs[0])
        nm = T.n("ShaderNodeNormalMap", uv_map="UVMap")
        T.link(nt.outputs[0], nm.inputs["Color"])
        T.link(nm.outputs[0], bsdf.inputs["Normal"])
    return mat


def previews(cfg, arm, body, clothes, skin_img, cloth_img, cloth_nrm):
    body.data.materials.clear()
    body.data.materials.append(preview_material("pv_skin", skin_img))
    clothes.data.materials.clear()
    clothes.data.materials.append(preview_material("pv_cloth", cloth_img, normal=cloth_nrm))
    hair_tex = bpy.data.images.load(os.path.join(OUT, "T_Hair_2_BaseColor.png" if cfg["name"] == "Female" else "T_Hair_1_BaseColor.png"))
    hair_mat = preview_material("pv_hair", hair_tex, tint=cfg["hair_tint"], smooth_from_alpha=False, smooth=0.75)  # wet, dark
    dark = bpy.data.materials.new("pv_dark")
    for o in bpy.data.objects:
        if o.name == "Hair":
            o.data.materials.clear()
            o.data.materials.append(hair_mat)
        elif o.name in ("Eyes", "Eyebrows"):
            o.data.materials.clear()
            o.data.materials.append(hair_mat if o.name == "Eyebrows" else dark)

    sc = bpy.context.scene
    sc.cycles.samples = 48
    sc.render.resolution_x, sc.render.resolution_y = 560, 900
    sc.render.film_transparent = False
    world = bpy.data.worlds.new("pv") if sc.world is None else sc.world
    sc.world = world
    world.use_nodes = True
    world.node_tree.nodes["Background"].inputs[0].default_value = (0.06, 0.08, 0.08, 1)
    world.node_tree.nodes["Background"].inputs[1].default_value = 0.6
    for loc, energy, size in (((2.5, -3.0, 3.0), 600, 3.0), ((-3.0, 2.0, 2.5), 300, 2.0)):
        light = bpy.data.objects.new("pv_light", bpy.data.lights.new("pv_light", "AREA"))
        light.data.energy, light.data.size = energy, size
        light.location = loc
        light.rotation_euler = (Vector((0, 0, 1.0)) - Vector(loc)).to_track_quat("-Z", "Y").to_euler()
        sc.collection.objects.link(light)
    cam = bpy.data.objects.new("pv_cam", bpy.data.cameras.new("pv_cam"))
    cam.data.lens = 50
    sc.collection.objects.link(cam)
    sc.camera = cam

    def shot(tag, angle):
        target = Vector((0, 0, 0.92))
        cam.location = target + Vector((math.sin(angle) * 4.2, -math.cos(angle) * 4.2, 0.2))
        cam.rotation_euler = (target - cam.location).to_track_quat("-Z", "Y").to_euler()
        sc.render.filepath = os.path.join(PREVIEW, f"player_{cfg['name'].lower()}_{tag}.png")
        bpy.ops.render.render(write_still=True)

    shot("front", math.pi)  # the bodies face +Y
    shot("back", 0.0)
    # Mid-run (UAL1 jog), to check the clothes bend without skin poking through.
    before = set(bpy.data.objects)
    bpy.ops.import_scene.fbx(filepath=UAL1)
    action = next((a for a in bpy.data.actions if "Jog_Fwd" in a.name), None)
    for o in set(bpy.data.objects) - before:
        bpy.data.objects.remove(o, do_unlink=True)
    if action:
        ad = arm.animation_data_create()
        ad.action = action
        if getattr(ad, "action_slot", None) is None and getattr(action, "slots", None):
            ad.action_slot = action.slots[0]
        sc.frame_set(int(action.frame_range[0] + (action.frame_range[1] - action.frame_range[0]) * 0.3))
        shot("run", math.pi - 0.6)
    else:
        print("PREVIEW: no Jog_Fwd action in", UAL1)


for cfg in BODIES:
    arm, body = import_body(cfg)
    clothes = dress(body, arm, cfg)
    skin = bake_skin(body, cfg, eye_centres())
    cloth, cloth_n = bake_clothes(clothes, cfg)
    export(cfg)
    if PREVIEW:
        os.makedirs(PREVIEW, exist_ok=True)
        previews(cfg, arm, body, clothes, skin, cloth, cloth_n)
