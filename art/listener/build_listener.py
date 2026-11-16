"""Still Water: builds the Listener (original model) in Blender, headless.

    blender -b --factory-startup --python art/listener/build_listener.py -- <out.fbx> <out.blend> [render_dir]

The Listener: a drowned, eel-tailed thing that swims face-down. Gaunt ribbed torso, shoulder blades and bony dorsal
spines that break the surface, too-long arms with four needle fingers and black claws, withered legs fused along a
long tail, a second pair of small mantis arms folded under the ribs, and a blank, eyeless egg of a head that splits
open vertically into two toothed halves when it catches you.

Facing +Y, up +Z, chest at the origin (ListenerAI keeps the "Chest" bone on the water line). Default FBX axes, the same
round trip as the Quaternius files. Clips: Swim_Fwd_Loop, Swim_Idle_Loop, Zombie_Scratch (the catch lunge).
"""
import bpy, bmesh, math, random, sys, os
import numpy as np
from mathutils import Vector as V, Matrix, Quaternion

ARGS = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
OUT_FBX = ARGS[0] if len(ARGS) > 0 else None
OUT_BLEND = ARGS[1] if len(ARGS) > 1 else None
RENDER_DIR = ARGS[2] if len(ARGS) > 2 else None
FPS = 30
random.seed(7)

bpy.ops.wm.read_factory_settings(use_empty=True)
scene = bpy.context.scene
scene.render.fps = FPS


# ---------------------------------------------------------------- helpers
def smooth(e0, e1, x):
    t = max(0.0, min(1.0, (x - e0) / (e1 - e0)))
    return t * t * (3 - 2 * t)


def lerp(a, b, t):
    return a + (b - a) * t


def catmull(pts, radii, per=4):
    """Densify a polyline (Catmull-Rom) with linearly interpolated radii, so tapers read smooth."""
    out_p, out_r = [], []
    n = len(pts)
    for i in range(n - 1):
        p0, p1, p2, p3 = pts[max(i - 1, 0)], pts[i], pts[i + 1], pts[min(i + 2, n - 1)]
        r1, r2 = radii[i], radii[i + 1]
        for s in range(per):
            t = s / per
            t2, t3 = t * t, t * t * t
            p = 0.5 * ((2 * p1) + (-p0 + p2) * t + (2 * p0 - 5 * p1 + 4 * p2 - p3) * t2 + (-p0 + 3 * p1 - 3 * p2 + p3) * t3)
            out_p.append(p)
            out_r.append((lerp(r1[0], r2[0], t), lerp(r1[1], r2[1], t)))
    out_p.append(pts[-1])
    out_r.append(radii[-1])
    return out_p, out_r


def sweep(bm, pts, radii, sides=12, up_hint=V((0, 0, 1))):
    """Loft elliptical rings (rx across, rz up) along pts; capped. Returns the new verts."""
    rings = []
    n = len(pts)
    for i, p in enumerate(pts):
        t = (pts[min(i + 1, n - 1)] - pts[max(i - 1, 0)]).normalized()
        up = up_hint if abs(t.dot(up_hint)) < 0.95 else V((0, 1, 0))
        side = t.cross(up).normalized()
        u = side.cross(t).normalized()
        rx, rz = radii[i] if isinstance(radii[i], tuple) else (radii[i], radii[i])
        rings.append([bm.verts.new(p + side * (rx * math.cos(a)) + u * (rz * math.sin(a)))
                      for a in (2 * math.pi * k / sides for k in range(sides))])
    faces = []
    for i in range(n - 1):
        for k in range(sides):
            faces.append(bm.faces.new((rings[i][k], rings[i][(k + 1) % sides], rings[i + 1][(k + 1) % sides], rings[i + 1][k])))
    caps = []
    for ring, p in ((rings[0], pts[0]), (rings[-1], pts[-1])):
        c = bm.verts.new(p)
        caps.append(c)
        for k in range(sides):
            faces.append(bm.faces.new((c, ring[k], ring[(k + 1) % sides])))
    bmesh.ops.recalc_face_normals(bm, faces=faces)
    return [v for r in rings for v in r] + caps


def indices(bm, groups):
    """BMVert lists -> index lists (BMVerts die with the bmesh)."""
    bm.verts.index_update()
    return [([v.index for v in verts], tag) for verts, tag in groups]


def ellipsoid(bm, center, radii, rot=None, segs=(16, 10)):
    ret = bmesh.ops.create_uvsphere(bm, u_segments=segs[0], v_segments=segs[1], radius=1.0)
    rot = rot or Matrix.Identity(3)
    for v in ret["verts"]:
        v.co = V(center) + rot @ V((v.co.x * radii[0], v.co.y * radii[1], v.co.z * radii[2]))
    return ret["verts"]


def cone(bm, base, direction, length, radius, sides=5, bend=None):
    """A tapered spike/tooth/claw; bend (a vector) curves the tip."""
    d = direction.normalized()
    bend = bend or V((0, 0, 0))
    pts = [base, base + d * length * 0.45 + bend * 0.25, base + d * length * 0.8 + bend * 0.65, base + d * length + bend]
    radii = [radius, radius * 0.62, radius * 0.3, radius * 0.04]
    return sweep(bm, pts, radii, sides=sides, up_hint=V((0, 0, 1)) if abs(d.z) < 0.9 else V((1, 0, 0)))


def new_object(name, bm, materials):
    me = bpy.data.meshes.new(name)
    bm.to_mesh(me)
    bm.free()
    ob = bpy.data.objects.new(name, me)
    scene.collection.objects.link(ob)
    for m in materials:
        me.materials.append(m)
    return ob


def activate(ob):
    for o in scene.objects:
        o.select_set(False)
    ob.select_set(True)
    bpy.context.view_layer.objects.active = ob


def apply_modifier(ob, mod):
    activate(ob)
    bpy.ops.object.modifier_apply(modifier=mod.name)


def tris(ob):
    return sum(len(p.vertices) - 2 for p in ob.data.polygons)


def mirror(p):
    return V((-p[0], p[1], p[2]))


# ---------------------------------------------------------------- materials (Unity assigns its own; these are for previews)
def material(name, color, rough):
    m = bpy.data.materials.new(name)
    m.diffuse_color = (*color, 1.0)
    m.roughness = rough
    return m


SKIN = material("Listener_Skin", (0.60, 0.66, 0.64), 0.2)    # drowned pale, faintly green, wet (baked texture)
MOUTH = material("Listener_Mouth", (0.16, 0.015, 0.025), 0.15)  # inside of the head, claws
GLOW = material("Listener_Glow", (0.45, 1.0, 0.85), 0.3)        # the sound pits: bioluminescent, pulse in Unity
THROAT = material("Listener_Throat", (0.55, 0.06, 0.02), 0.3)   # deep in the throat: a dim ember-red glow
HAIR = material("Listener_Hair", (0.015, 0.015, 0.02), 0.12)     # drowned, wet, black
MATS = [SKIN, MOUTH, GLOW, THROAT, HAIR]  # slot order = Unity's material order
SCALE = 1.8  # exported size: ~7.6 m nose to tail, ~4.5 m arm tip to arm tip (modelled at 1, scaled by the FBX export)


# ---------------------------------------------------------------- skeleton (L = -X; facing +Y; back = +Z)
BONES = {}  # name -> (head, tail, parent)


def bone(name, h, t, parent=None):
    BONES[name] = (V(h), V(t), parent)


bone("Root", (0, 0, 0), (0, 0, 0.25))
bone("Hips", (0, -0.62, 0.0), (0, -0.40, 0.02), "Root")
bone("Spine1", (0, -0.40, 0.02), (0, -0.18, 0.04), "Hips")
bone("Spine2", (0, -0.18, 0.04), (0, 0.0, 0.05), "Spine1")
bone("Chest", (0, 0.0, 0.05), (0, 0.30, 0.07), "Spine2")
bone("Neck1", (0, 0.30, 0.07), (0, 0.48, 0.13), "Chest")
bone("Neck2", (0, 0.48, 0.13), (0, 0.64, 0.19), "Neck1")
bone("Head", (0, 0.64, 0.19), (0, 1.0, 0.27), "Neck2")
TAIL_Y = [-0.62, -0.92, -1.22, -1.52, -1.82, -2.12, -2.42, -2.72, -3.02]
TAIL_Z = [0.0, -0.01, -0.03, -0.05, -0.06, -0.07, -0.07, -0.07, -0.07]
for i in range(8):
    bone(f"Tail{i + 1}", (0, TAIL_Y[i], TAIL_Z[i]), (0, TAIL_Y[i + 1], TAIL_Z[i + 1]), "Hips" if i == 0 else f"Tail{i}")

FINGER_AXIS = {}  # finger bone -> world axis that curls it toward the palm
for S, sx in (("L", 1), ("R", -1)):
    def m(p, sx=sx):  # author on the left (-X); mirror for the right
        return V((p[0] * sx, p[1], p[2]))
    bone(f"Jaw_{S}", m((-0.06, 0.70, 0.27)), m((-0.08, 0.98, 0.25)), "Head")
    # v3: shoulders ~9 cm further out each side, under a hunched hump: a wider, heavier upper body.
    bone(f"Clav_{S}", m((-0.05, 0.24, 0.10)), m((-0.37, 0.22, 0.08)), "Chest")
    bone(f"UpperArm_{S}", m((-0.37, 0.22, 0.08)), m((-0.80, 0.44, -0.05)), f"Clav_{S}")
    bone(f"Forearm_{S}", m((-0.80, 0.44, -0.05)), m((-1.12, 0.94, -0.13)), f"UpperArm_{S}")
    bone(f"Hand_{S}", m((-1.12, 0.94, -0.13)), m((-1.18, 1.08, -0.15)), f"Forearm_{S}")
    bone(f"SmallArm_{S}_1", m((-0.15, 0.06, -0.09)), m((-0.20, 0.20, -0.15)), "Chest")
    bone(f"SmallArm_{S}_2", m((-0.20, 0.20, -0.15)), m((-0.09, 0.32, -0.17)), f"SmallArm_{S}_1")
    bone(f"SmallClaw_{S}", m((-0.09, 0.32, -0.17)), m((-0.05, 0.44, -0.13)), f"SmallArm_{S}_2")
    bone(f"Leg_{S}_1", m((-0.10, -0.60, -0.03)), m((-0.15, -0.95, -0.10)), "Hips")
    bone(f"Leg_{S}_2", m((-0.15, -0.95, -0.10)), m((-0.12, -1.30, -0.09)), f"Leg_{S}_1")
    bone(f"Foot_{S}", m((-0.12, -1.30, -0.09)), m((-0.11, -1.44, -0.16)), f"Leg_{S}_2")
    # Four long fingers, no thumb, fanned from the end of the hand.
    h0, h1 = BONES[f"Hand_{S}"][0], BONES[f"Hand_{S}"][1]
    hd = (h1 - h0).normalized()
    across = V((0, 0, 1)).cross(hd).normalized()
    for i, (ang, off, ln) in enumerate(((-0.55, -0.032, 0.27), (-0.18, -0.011, 0.34), (0.18, 0.011, 0.33), (0.55, 0.032, 0.25))):
        d = (Matrix.Rotation(ang, 3, "Z") @ hd)
        d.z -= 0.12
        d.normalize()
        p0 = h1 + across * off
        p1 = p0 + d * ln * 0.55
        curl = V((0, 0, 1)).cross(d).normalized()  # rotating about this (+) bends the finger down, toward the palm
        d2 = (Matrix.Rotation(0.3, 3, curl) @ d).normalized()
        p2 = p1 + d2 * ln * 0.45
        bone(f"Finger_{S}_{i}_1", p0, p1, f"Hand_{S}")
        bone(f"Finger_{S}_{i}_2", p1, p2, f"Finger_{S}_{i}_1")
        FINGER_AXIS[f"Finger_{S}_{i}_1"] = curl
        FINGER_AXIS[f"Finger_{S}_{i}_2"] = curl

# Bones the main body may be weighted to (the rest drive separate parts: fingers, small arms, jaws).
BODY_BONES = {"Hips", "Spine1", "Spine2", "Chest", "Neck1", "Neck2", "Head"} | {f"Tail{i}" for i in range(1, 9)}
for S in "LR":
    BODY_BONES |= {f"Clav_{S}", f"UpperArm_{S}", f"Forearm_{S}", f"Hand_{S}", f"Leg_{S}_1", f"Leg_{S}_2", f"Foot_{S}"}

# ---------------------------------------------------------------- body (one volume: torso, neck, tail, arms, legs, blades)
# y, half-width, half-height, centre z: a long gaunt chest, a pinched waist, then the tail thinning to a whip.
PROFILE = [(-3.05, .014, .014, -.07), (-2.72, .028, .028, -.07), (-2.42, .042, .040, -.07), (-2.12, .056, .052, -.07),
           (-1.82, .070, .064, -.06), (-1.52, .084, .075, -.05), (-1.22, .098, .085, -.03), (-0.92, .112, .095, -.01),
           (-0.70, .170, .110, .0), (-0.58, .185, .115, .0), (-0.45, .130, .095, .01), (-0.30, .125, .090, .025),
           (-0.14, .190, .115, .035), (0.03, .250, .140, .045), (0.18, .285, .140, .06), (0.29, .230, .120, .07),
           (0.34, .130, .095, .085), (0.40, .066, .066, .11), (0.50, .056, .058, .14), (0.64, .058, .058, .19), (0.74, .055, .055, .20)]


def profile(y):
    if y <= PROFILE[0][0]:
        return PROFILE[0][1:]
    for a, b in zip(PROFILE, PROFILE[1:]):
        if a[0] <= y <= b[0]:
            t = (y - a[0]) / (b[0] - a[0])
            return tuple(lerp(a[k], b[k], t) for k in (1, 2, 3))
    return PROFILE[-1][1:]


bm = bmesh.new()
pts, radii = catmull([V((0, p[0], p[3])) for p in PROFILE], [(p[1], p[2]) for p in PROFILE], per=4)
sweep(bm, pts, radii, sides=20)
for S, sx in (("L", 1), ("R", -1)):
    def m(p, sx=sx):
        return V((p[0] * sx, p[1], p[2]))
    arm = [m((-0.05, 0.24, 0.10)), m((-0.37, 0.22, 0.08)), m((-0.59, 0.33, 0.01)), m((-0.80, 0.44, -0.05)),
           m((-0.96, 0.69, -0.09)), m((-1.12, 0.94, -0.13)), m((-1.18, 1.08, -0.15))]  # must follow the arm bones
    ar = [(.075, .065), (.10, .085), (.068, .062), (.052, .048), (.04, .036), (.029, .025), (.05, .018)]
    p, r = catmull(arm, ar, per=4)
    sweep(bm, p, r, sides=12)
    ellipsoid(bm, m((-0.80, 0.44, -0.05)), (.058, .06, .054))                 # knobbly elbow
    leg = [m((-0.10, -0.58, -0.03)), m((-0.15, -0.95, -0.10)), m((-0.12, -1.30, -0.09)), m((-0.11, -1.45, -0.16))]
    p, r = catmull(leg, [(.055, .05), (.032, .03), (.02, .02), (.026, .011)], per=4)
    sweep(bm, p, r, sides=10)
    ellipsoid(bm, m((-0.15, -0.95, -0.10)), (.036, .036, .034))              # knee
    # Shoulder blades: thin tilted plates that stand proud of the back, the first thing to break the surface.
    ellipsoid(bm, m((-0.16, 0.10, 0.165)), (.11, .15, .035), Matrix.Rotation(sx * -0.35, 3, "Y") @ Matrix.Rotation(sx * 0.25, 3, "Z"))
    ellipsoid(bm, m((-0.22, 0.25, 0.13)), (.17, .12, .085))                   # a hunched hump of trapezius
    ellipsoid(bm, m((-0.36, 0.22, 0.09)), (.09, .085, .08))                   # shoulder knob
    ellipsoid(bm, m((-0.14, -0.60, 0.03)), (.085, .07, .05))                  # hip bones
body = new_object("ListenerBody", bm, [SKIN])
remesh = body.modifiers.new("Remesh", "REMESH")
remesh.mode = "VOXEL"
remesh.voxel_size = 0.009
apply_modifier(body, remesh)
sm = body.modifiers.new("Smooth", "SMOOTH")
sm.factor, sm.iterations = 0.6, 4
apply_modifier(body, sm)


def detail(p):
    """Displacement along the normal: ribs, a sunken belly, vertebrae knuckles, gill slits on the neck."""
    x, y, z = p
    rx, rz, zc = profile(y)
    side = min(1.0, abs(x) / max(rx, 1e-3))
    top = max(-1.0, min(1.0, (z - zc) / max(rz, 1e-3)))
    d = 0.0
    if -0.27 < y < 0.29 and abs(x) < rx * 1.2:
        ry = y + 0.10 * (1 - top)                                      # ribs sweep forward toward the belly
        rib = max(0.0, math.cos(2 * math.pi * ry / 0.055)) ** 3 - 0.3
        fall = smooth(-0.27, -0.17, y) * (1 - smooth(0.19, 0.29, y))
        d += 0.012 * rib * fall * (0.3 + 0.7 * side) * (1 - 0.85 * smooth(0.55, 0.95, top))
    if -0.52 < y < -0.18 and top < 0:                                  # starved, sucked-in belly
        d -= 0.018 * math.sin(math.pi * (y + 0.52) / 0.34) * min(1.0, -top * 1.5)
    if -2.9 < y < 0.40 and top > 0.3 and abs(x) < 0.06:                # vertebrae
        spacing = 0.05 if y > -0.62 else 0.075
        d += 0.013 * max(0.0, math.cos(2 * math.pi * y / spacing)) ** 4 * (1 - abs(x) / 0.06) * smooth(0.3, 0.9, top)
    if 0.38 < y < 0.58 and side > 0.55 and abs(top) < 0.6:             # gill slits
        d -= 0.007 * max(0.0, math.cos(2 * math.pi * (y - 0.38) / 0.04)) ** 10 * smooth(0.55, 0.8, side)
    return d


me = body.data
offsets = [detail(v.co) for v in me.vertices]
normals = [v.normal.copy() for v in me.vertices]
for v, d, n in zip(me.vertices, offsets, normals):
    v.co += n * d
dec = body.modifiers.new("Decimate", "DECIMATE")
dec.ratio = min(1.0, 9000 / max(1, tris(body)))
apply_modifier(body, dec)

# ---------------------------------------------------------------- head: a blank egg that splits into two toothed halves
U_SEG, V_SEG = 32, 22
HEAD_C = V((0, 0.82, 0.28))   # high and forward: the neck goes in under the back of the skull, not its middle
HX, HYF, HYB, HZ = 0.13, 0.28, 0.19, 0.145  # half-width, front and back length, half-height
bm = bmesh.new()
sph = bmesh.ops.create_uvsphere(bm, u_segments=U_SEG, v_segments=V_SEG, radius=1.0)
unit = {v: v.co.copy() for v in sph["verts"]}
seam = {v for v in sph["verts"] if abs(v.co.x) < 1e-4 and v.co.y > 1e-3}         # the front meridian, poles excluded
bmesh.ops.split_edges(bm, edges=[e for e in bm.edges if e.verts[0] in seam and e.verts[1] in seam])
bm.verts.ensure_lookup_table()
side_of, uco = {}, {}
for v in bm.verts:
    u = unit.get(v)
    if u is None:  # a copy made by the split: same position as an original
        u = v.co.copy()
    uco[v] = u
    if abs(u.x) < 1e-4 and v.link_faces:
        side_of[v] = -1 if sum((f.calc_center_median().x for f in v.link_faces)) < 0 else 1
    else:
        side_of[v] = -1 if u.x < 0 else 1


def head_shape(u):
    fy = max(u.y, 0.0)
    x = u.x * HX * (1 - 0.32 * fy * fy)
    y = u.y * (HYF if u.y > 0 else HYB)
    z = u.z * HZ * (1 - 0.25 * fy * fy) + 0.03 * max(0.0, -u.y) * max(0.0, u.z)  # cranium swells up and back
    z -= 0.05 * fy * fy                                                            # the blank face droops
    x *= 1 - 0.12 * max(0.0, -u.z) * fy                                            # narrow, chinless underside
    p = V((x, y, z))
    n = u.normalized()
    for sx in (-1, 1):  # eye sockets that skin has grown over, a heavy brow above them, cheekbones below
        p -= n * 0.030 * math.exp(-((n - V((sx * 0.45, 0.75, 0.28)).normalized()).length / 0.22) ** 2)
        p += n * 0.016 * math.exp(-((n - V((sx * 0.42, 0.60, 0.66)).normalized()).length / 0.18) ** 2)
        p += n * 0.012 * math.exp(-((n - V((sx * 0.66, 0.66, -0.18)).normalized()).length / 0.18) ** 2)
        p -= n * 0.012 * math.exp(-((n - V((sx * 0.85, 0.15, 0.25)).normalized()).length / 0.25) ** 2)  # sunken temples
    return HEAD_C + p


seam_pts = {-1: [], 1: []}
for v in bm.verts:
    u = uco[v]
    p = head_shape(u)
    if abs(u.x) < 1e-4 and u.y > 1e-3:
        p = HEAD_C + (p - HEAD_C) * 0.965  # a faint crease down the face: the only hint of what it is
        seam_pts[side_of[v]].append((u, p))
    v.co = p
head = new_object("ListenerHead", bm, MATS)
hg = head.vertex_groups.new(name="Head")
jg = {-1: head.vertex_groups.new(name="Jaw_L"), 1: head.vertex_groups.new(name="Jaw_R")}
# Weights: the front half rides the jaw bones, the back of the skull the head bone.
bm = bmesh.new()
bm.from_mesh(head.data)
bm.verts.ensure_lookup_table()
for v in bm.verts:
    rel = v.co - HEAD_C
    uy = rel.y / HYF if rel.y > 0 else rel.y / HYB
    w = smooth(0.05, 0.45, uy)
    s = -1 if (rel.x < -1e-5 or (abs(rel.x) <= 1e-5 and sum(f.calc_center_median().x for f in v.link_faces) < 0)) else 1
    if w < 1:
        hg.add([v.index], 1 - w, "REPLACE")
    if w > 0:
        jg[s].add([v.index], w, "REPLACE")
bm.free()
sol = head.modifiers.new("Solidify", "SOLIDIFY")
sol.thickness, sol.offset, sol.use_rim = 0.012, -1.0, True
sol.material_offset, sol.material_offset_rim = 1, 1   # the inside of the head is raw red flesh
apply_modifier(head, sol)

# Teeth along both split edges (pointing across the seam), a throat behind them ringed with more.
bm = bmesh.new()
teeth = []  # (verts, bone)
for s, pts_ in seam_pts.items():
    pts_ = sorted([q for q in pts_ if abs(q[0].z) < 0.86 and q[0].y > 0.3], key=lambda q: q[0].z)
    sample = []
    for a, b in zip(pts_, pts_[1:]):
        sample += [a[1], (a[1] + b[1]) * 0.5]
    if pts_:
        sample.append(pts_[-1][1])
    for p in sample:
        inward = (HEAD_C - p).normalized()
        half = HX * math.sqrt(max(0.05, 1 - ((p.z - HEAD_C.z) / HZ) ** 2))
        ln = min(random.uniform(0.035, 0.075), 0.45 * half)
        base = p + inward * 0.008 + V((s * 0.006, 0, 0))
        dirn = V((-s, 0, 0)) + inward * 0.9 + V((0, 0, random.uniform(-0.15, 0.15)))  # into the head while shut
        teeth.append((cone(bm, base, dirn, ln, random.uniform(0.0045, 0.007), sides=4, bend=V((0, -0.006, 0))), f"Jaw_{'L' if s < 0 else 'R'}"))
throat_c = HEAD_C + V((0, -0.03, -0.015))
for ring, (ry, rr) in enumerate(((0.07, 0.08), (0.02, 0.07))):
    for k in range(11):
        a = 2 * math.pi * (k + 0.5 * ring) / 11
        base = throat_c + V((math.cos(a) * rr, ry, math.sin(a) * rr * 0.9))
        inward = (throat_c + V((0, ry - 0.03, 0)) - base).normalized()
        teeth.append((cone(bm, base, inward, random.uniform(0.035, 0.055), 0.0055, sides=4), "Head"))
teeth = indices(bm, teeth)
mouth_parts = new_object("ListenerTeeth", bm, MATS)
groups = {}
for idx, b in teeth:
    g = groups.get(b) or mouth_parts.vertex_groups.new(name=b)
    groups[b] = g
    g.add(idx, 1.0, "REPLACE")

bm = bmesh.new()
cav = ellipsoid(bm, throat_c, (0.10, 0.13, 0.105), segs=(14, 9))
bmesh.ops.reverse_faces(bm, faces=list(bm.faces))   # seen from inside: a dark hollow down the throat
ellipsoid(bm, throat_c + V((0, -0.075, -0.005)), (0.03, 0.02, 0.026), segs=(8, 5))  # the ember, deep in the gullet
cavity = new_object("ListenerThroat", bm, MATS)
for p in cavity.data.polygons:
    p.material_index = 1 if p.index < len(cavity.data.polygons) - 8 * 5 else 3
cavity.vertex_groups.new(name="Head").add(list(range(len(cavity.data.vertices))), 1.0, "REPLACE")


# ---------------------------------------------------------------- separate parts weighted by distance to their bones
def seg_dist(p, a, b):
    ab = b - a
    t = max(0.0, min(1.0, (p - a).dot(ab) / max(ab.length_squared, 1e-9)))
    return (p - (a + ab * t)).length


def weight_by_distance(ob, candidates, keep=2, power=4):
    gs = {}
    for v in ob.data.vertices:
        ds = sorted(((seg_dist(v.co, BONES[b][0], BONES[b][1]), b) for b in candidates))[:keep]
        ws = [(1.0 / (d + 0.004) ** power, b) for d, b in ds]
        tot = sum(w for w, _ in ws)
        for w, b in ws:
            if w / tot < 0.02:
                continue
            g = gs.get(b) or ob.vertex_groups.get(b) or ob.vertex_groups.new(name=b)
            gs[b] = g
            g.add([v.index], w / tot, "REPLACE")


parts = []
for S in "LR":
    bm = bmesh.new()
    for i in range(4):
        a, b = BONES[f"Finger_{S}_{i}_1"][:2]
        c = BONES[f"Finger_{S}_{i}_2"][1]
        p, r = catmull([a - (b - a).normalized() * 0.02, (a + b) * 0.5, b, (b + c) * 0.5, c],
                       [(.017, .014), (.011, .010), (.014, .012), (.009, .008), (.008, .007)], per=2)
        sweep(bm, p, r, sides=6)
    fingers = new_object(f"Fingers_{S}", bm, MATS)
    weight_by_distance(fingers, [f"Hand_{S}"] + [f"Finger_{S}_{i}_{k}" for i in range(4) for k in (1, 2)])
    parts.append(fingers)

    bm = bmesh.new()
    for i in range(4):
        b, c = BONES[f"Finger_{S}_{i}_2"][:2]
        d = (c - b).normalized()
        cone(bm, c - d * 0.012, d, 0.14, 0.011, sides=5, bend=V((0, 0, -0.08)))  # long black hooked claws
    claws = new_object(f"Claws_{S}", bm, MATS)
    for p in claws.data.polygons:
        p.material_index = 1
    weight_by_distance(claws, [f"Finger_{S}_{i}_2" for i in range(4)], keep=1)
    parts.append(claws)

    bm = bmesh.new()
    chain = [BONES[f"SmallArm_{S}_1"][0], BONES[f"SmallArm_{S}_1"][1], BONES[f"SmallArm_{S}_2"][1], BONES[f"SmallClaw_{S}"][0] + (BONES[f"SmallClaw_{S}"][1] - BONES[f"SmallClaw_{S}"][0]) * 0.3]
    p, r = catmull(chain, [(.026, .024), (.019, .018), (.015, .014), (.012, .011)], per=4)
    sweep(bm, p, r, sides=8)
    small = new_object(f"SmallArm_{S}", bm, MATS)
    weight_by_distance(small, ["Chest", f"SmallArm_{S}_1", f"SmallArm_{S}_2", f"SmallClaw_{S}"])
    parts.append(small)
    bm = bmesh.new()
    a, b = BONES[f"SmallClaw_{S}"][:2]
    cone(bm, a, b - a, 0.22, 0.013, sides=6, bend=V((0, 0, -0.08)))           # a folded scythe of a claw
    sclaw = new_object(f"SmallClaw_{S}", bm, MATS)
    for p in sclaw.data.polygons:
        p.material_index = 1
    sclaw.vertex_groups.new(name=f"SmallClaw_{S}").add(list(range(len(sclaw.data.vertices))), 1.0, "REPLACE")
    parts.append(sclaw)

# Dorsal spines: bony, raked back, tallest between the shoulder blades, a whip of small ones down the tail.
bm = bmesh.new()
spines = []
y = 0.40
while y > -2.95:
    rx, rz, zc = profile(y)
    h = (0.03 + 0.05 * smooth(0.42, 0.18, y)) if y > 0.0 else lerp(0.085, 0.012, smooth(-0.3, -2.95, y))
    base = V((0, y, zc + rz - 0.012))
    spines.append((cone(bm, base, V((0, -0.65, 1.0)), h, 0.004 + 0.22 * h, sides=4, bend=V((0, -0.25 * h, 0))), y))
    y -= 0.065 if y > -0.62 else 0.10
spines = indices(bm, spines)
spine_ob = new_object("Spines", bm, MATS)
CHAIN = ["Neck1", "Chest", "Spine2", "Spine1", "Hips"] + [f"Tail{i}" for i in range(1, 9)]
for idx, y in spines:
    b = min(CHAIN, key=lambda n: seg_dist(V((0, y, 0)), V((0, BONES[n][0].y, 0)), V((0, BONES[n][1].y, 0))))
    g = spine_ob.vertex_groups.get(b) or spine_ob.vertex_groups.new(name=b)
    g.add(idx, 1.0, "REPLACE")
# Sound pits: it has no eyes, it listens. Glowing beads in a lateral line down each flank (a moving string of lights in
# the dark is how you see it coming) and clustered in the skinned-over eye sockets.
bm = bmesh.new()
pits = []
y = 0.30
while y > -2.85:
    rx, rz, zc = profile(y)
    r = 0.008 + 0.007 * smooth(-2.8, 0.0, y)
    for sx in (-1, 1):
        pits.append((ellipsoid(bm, V((sx * rx * 0.96, y, zc + rz * 0.18)), (r * 0.7, r * 1.7, r), segs=(5, 3)), ("chain", y)))
    y -= 0.07 if y > -0.62 else 0.10
for sx in (-1, 1):
    centre = V((sx * 0.45, 0.75, 0.28)).normalized()
    t1 = centre.cross(V((0, 0, 1))).normalized()
    t2 = t1.cross(centre).normalized()
    for k in range(8):
        a, d = 2 * math.pi * k / 7, 0.0 if k == 7 else 0.16
        u = (centre + t1 * math.cos(a) * d + t2 * math.sin(a) * d).normalized()
        p = head_shape(u)
        rr = random.uniform(0.0045, 0.007)
        pits.append((ellipsoid(bm, p, (rr, rr, rr), segs=(5, 3)), ("bone", f"Jaw_{'L' if sx < 0 else 'R'}")))
pits = indices(bm, pits)
pit_ob = new_object("SoundPits", bm, MATS)
for p in pit_ob.data.polygons:
    p.material_index = 2
for idx, (kind, val) in pits:
    b = val if kind == "bone" else min(CHAIN, key=lambda n: seg_dist(V((0, val, 0)), V((0, BONES[n][0].y, 0)), V((0, BONES[n][1].y, 0))))
    (pit_ob.vertex_groups.get(b) or pit_ob.vertex_groups.new(name=b)).add(idx, 1.0, "REPLACE")
# Hair: long wet black strands from the crown, plastered down the neck and back, spreading as they go.
bm = bmesh.new()
for k in range(30):
    u = V((random.uniform(-0.8, 0.8), random.uniform(-0.98, -0.25), random.uniform(0.05, 0.9))).normalized()
    root = head_shape(u) - u * 0.004
    length = random.uniform(0.55, 1.15)
    phase, spread = random.uniform(0, 6.28), random.uniform(0.8, 1.8)
    pts, rad = [], []
    for i in range(9):
        t = i / 8
        y = root.y - length * t
        x = root.x * (1 + spread * t) + 0.025 * math.sin(phase + t * 7) * t
        rx, rz, zc = profile(y)
        top = zc + rz * math.sqrt(max(0.0, 1 - min(1.0, (x / max(rx, 1e-3)) ** 2))) + 0.004
        if abs(x) > rx:  # past the edge of the body: it hangs down the side
            top = zc - 0.4 * (abs(x) - rx) / max(rx, 1e-3) * rz
        pts.append(V((x, y, lerp(root.z, top, smooth(0.0, 0.45, t)))))
        rad.append((lerp(0.013, 0.004, t), lerp(0.0035, 0.0015, t)))  # flat wet clumps, not wires
    sweep(bm, pts, rad, sides=4)
hair = new_object("Hair", bm, MATS)
for p in hair.data.polygons:
    p.material_index = 4
weight_by_distance(hair, ["Head", "Neck2", "Neck1", "Chest", "Spine2", "Spine1", "Clav_L", "Clav_R"])
# Torn skin: ragged strips of dead skin hanging off the underside of each forearm and upper arm, trailing in the water.
# They break up the smooth silhouette.
bm = bmesh.new()
for S, sx in (("L", 1), ("R", -1)):
    for bone_name, count in ((f"UpperArm_{S}", 3), (f"Forearm_{S}", 5)):
        a, b = BONES[bone_name][:2]
        for i in range(count):
            t = (i + 0.5) / count + random.uniform(-0.06, 0.06)
            root = a + (b - a) * t + V((0, 0, -0.03))
            length = random.uniform(0.16, 0.34)
            width = random.uniform(0.03, 0.055)  # a sheet running along the arm, not a spike
            drift = V((random.uniform(-0.03, 0.03), -random.uniform(0.04, 0.12), 0))
            pts = [root + drift * (k / 5) ** 1.5 + V((0, 0, -length * k / 5)) for k in range(6)]
            along = (b - a).normalized()
            ragged = [width * (1 - 0.6 * k / 5) * random.uniform(0.6, 1.2) for k in range(6)]
            sweep(bm, pts, [(ragged[k], 0.0025) for k in range(6)], sides=4, up_hint=along.cross(V((0, 0, -1))).normalized())
flaps = new_object("SkinFlaps", bm, MATS)
weight_by_distance(flaps, [b for S in "LR" for b in (f"Clav_{S}", f"UpperArm_{S}", f"Forearm_{S}", f"Hand_{S}")])

# Masks for the bake: teeth and spines are ivory with no veins; the flaps are dead, rotting skin.
for ob, name in ((mouth_parts, "bone"), (spine_ob, "bone"), (flaps, "flap")):
    attr = ob.data.attributes.new(name, "FLOAT", "POINT")
    attr.data.foreach_set("value", [1.0] * len(ob.data.vertices))
parts += [spine_ob, head, mouth_parts, cavity, pit_ob, hair, flaps]
print("PART tris:", {p.name: tris(p) for p in parts + [body]})

# ---------------------------------------------------------------- armature, weights, join
arm_data = bpy.data.armatures.new("ListenerRig")
rig = bpy.data.objects.new("ListenerRig", arm_data)
scene.collection.objects.link(rig)
activate(rig)
bpy.ops.object.mode_set(mode="EDIT")
for name, (h, t, parent) in BONES.items():
    eb = arm_data.edit_bones.new(name)
    eb.head, eb.tail, eb.roll = h, t, 0.0
for name, (h, t, parent) in BONES.items():
    if parent:
        arm_data.edit_bones[name].parent = arm_data.edit_bones[parent]
bpy.ops.object.mode_set(mode="OBJECT")
for b in arm_data.bones:
    b.use_deform = b.name in BODY_BONES   # only for the automatic weights; restored below

for o in scene.objects:
    o.select_set(False)
body.select_set(True)
rig.select_set(True)
bpy.context.view_layer.objects.active = rig
try:
    bpy.ops.object.parent_set(type="ARMATURE_AUTO")
    heat_ok = True
except RuntimeError as e:
    print("HEAT FAILED", e)
    heat_ok = False
# Any vertex the heat solve left unweighted (or all of them, if it failed): nearest bones.
unweighted = [v.index for v in body.data.vertices if sum(g.weight for g in v.groups) < 1e-3]
print("BODY unweighted after heat:", len(unweighted), "of", len(body.data.vertices))
if unweighted:
    tmp = body.data.vertices
    for vi in unweighted:
        co = tmp[vi].co
        ds = sorted(((seg_dist(co, BONES[b][0], BONES[b][1]), b) for b in BODY_BONES))[:2]
        ws = [(1.0 / (d + 0.004) ** 4, b) for d, b in ds]
        tot = sum(w for w, _ in ws)
        for w, b in ws:
            (body.vertex_groups.get(b) or body.vertex_groups.new(name=b)).add([vi], w / tot, "REPLACE")
for b in arm_data.bones:
    b.use_deform = b.name != "Root"

for o in scene.objects:
    o.select_set(False)
for p in parts:
    p.select_set(True)
body.select_set(True)
bpy.context.view_layer.objects.active = body
bpy.ops.object.join()
body.name = body.data.name = "Listener"
if not any(m.type == "ARMATURE" for m in body.modifiers):
    mod = body.modifiers.new("Armature", "ARMATURE")
    mod.object = rig
    body.parent = rig
for p in body.data.polygons:
    p.use_smooth = True
activate(body)
try:
    bpy.ops.object.mode_set(mode="EDIT")
    bpy.ops.mesh.select_all(action="SELECT")
    bpy.ops.uv.smart_project(angle_limit=math.radians(66), island_margin=0.003)
    bpy.ops.object.mode_set(mode="OBJECT")
except RuntimeError as e:
    print("UV FAILED", e)
    bpy.ops.object.mode_set(mode="OBJECT")
print("LISTENER tris:", tris(body), "verts:", len(body.data.vertices), "materials:", [m.name for m in body.data.materials])

# ---------------------------------------------------------------- animation
REST = {b.name: b.matrix_local.to_3x3() for b in arm_data.bones}
X, Y, Z = V((1, 0, 0)), V((0, 1, 0)), V((0, 0, 1))


def q(name, axis, ang):
    """Rotation of a pose bone about a (rest) armature-space axis."""
    return Quaternion((REST[name].transposed() @ V(axis)).normalized(), ang)


def key_pose(frame, rots, root_loc=V((0, 0, 0))):
    for pb in rig.pose.bones:
        r = Quaternion()
        for axis, ang in rots.get(pb.name, ()):
            r = r @ q(pb.name, axis, ang)
        pb.rotation_mode = "QUATERNION"
        pb.rotation_quaternion = r
        pb.keyframe_insert("rotation_quaternion", frame=frame)
        if pb.name == "Root":
            pb.location = REST["Root"].transposed() @ root_loc
            pb.keyframe_insert("location", frame=frame)


def add(rots, name, axis, ang):
    rots.setdefault(name, []).append((axis, ang))


def side(S):
    return 1 if S == "L" else -1


def new_action(name, f0, f1, cyclic):
    rig.animation_data_create()
    rig.animation_data.action = None
    return name, f0, f1, cyclic


def finish_action(name, f0, f1, cyclic):
    act = rig.animation_data.action
    act.name = name
    act.use_fake_user = True
    act.use_frame_range = True
    act.frame_start, act.frame_end = f0, f1
    act.use_cyclic = cyclic
    return act


# Swim_Fwd_Loop: an eel's travelling wave down the tail, a slow, wide, spidery breaststroke, head cocked and listening.
def swim(t):
    ph = 2 * math.pi * t
    r = {}
    for i in range(8):
        add(r, f"Tail{i + 1}", Z, (0.07 + 0.028 * i) * math.sin(ph - 0.75 * (i + 1)))
    add(r, "Hips", Z, 0.07 * math.sin(ph))
    add(r, "Spine1", Z, -0.04 * math.sin(ph + 0.4))
    add(r, "Chest", Z, -0.03 * math.sin(ph + 0.8))
    add(r, "Neck1", X, 0.12)
    add(r, "Neck1", Z, 0.05 * math.sin(ph + 1.4))     # the head stays on target while the body writhes
    add(r, "Head", Y, 0.28 + 0.04 * math.sin(ph * 2))  # cocked
    pull = smooth(0.0, 0.42, t) if t < 0.42 else 1 - smooth(0.42, 1.0, t)
    bend = math.sin(math.pi * (t - 0.42) / 0.58) if t > 0.42 else 0.0
    for S in "LR":
        s = side(S)
        add(r, f"UpperArm_{S}", Z, s * (-0.35 + 1.25 * pull))
        add(r, f"UpperArm_{S}", Y, s * (0.25 * bend - 0.1))        # claws lift as the arms come round
        add(r, f"Forearm_{S}", Z, -s * 0.95 * bend)
        add(r, f"Hand_{S}", Z, -s * 0.3 * bend)
        for i in range(4):
            for k in (1, 2):
                n = f"Finger_{S}_{i}_{k}"
                add(r, n, FINGER_AXIS[n], 0.15 + 0.45 * pull * (1 - bend) - 0.1 * bend)
        add(r, f"SmallArm_{S}_2", Z, s * 0.08 * math.sin(ph * 3 + i))
        add(r, f"Leg_{S}_1", Z, 0.18 * math.sin(ph - 1.6))
        add(r, f"Leg_{S}_2", Z, 0.22 * math.sin(ph - 2.3))
    return r


# Swim_Idle_Loop: hanging upright-ish in the water, arms drifting down, fingers twitching; the head snaps
# between listening angles and the face hairlines open a crack, then shuts.
def idle(t, f):
    ph = 2 * math.pi * t
    r = {}
    for i in range(8):
        add(r, f"Tail{i + 1}", Z, (0.04 + 0.015 * i) * math.sin(ph - 0.6 * (i + 1)))
    add(r, "Neck1", X, 0.22)
    add(r, "Neck2", X, 0.12)
    # Snap-and-hold head tilts (the jerk is the scary part).
    beats = [(0, 0.32, 0.0), (22, 0.32, 0.0), (25, -0.22, 0.15), (52, -0.22, 0.15), (55, 0.45, -0.1), (78, 0.45, -0.1), (82, 0.32, 0.0), (90, 0.32, 0.0)]
    for (fa, ra, ya), (fb, rb, yb) in zip(beats, beats[1:]):
        if fa <= f <= fb:
            k = smooth(fa, fb, f)
            add(r, "Head", Y, lerp(ra, rb, k))
            add(r, "Head", Z, lerp(ya, yb, k))
            break
    crack = 0.12 * max(0.0, math.sin(math.pi * (f - 40) / 14)) if 40 <= f <= 54 else 0.0
    for S in "LR":
        s = side(S)
        add(r, f"Jaw_{S}", Z, s * crack)
        add(r, f"UpperArm_{S}", Y, -s * (0.85 + 0.08 * math.sin(ph + (0 if S == "L" else 1.7))))
        add(r, f"UpperArm_{S}", Z, s * 0.15 * math.sin(ph * 0.5 + (0.6 if S == "L" else 2.2)))
        add(r, f"Forearm_{S}", Z, -s * (0.35 + 0.15 * math.sin(ph + 0.9)))
        for i in range(4):
            tw = 0.25 + 0.3 * max(0.0, math.sin(ph * 2 + i * 1.3 + (0 if S == "L" else 2.0))) ** 6
            for k in (1, 2):
                n = f"Finger_{S}_{i}_{k}"
                add(r, n, FINGER_AXIS[n], tw)
        add(r, f"SmallArm_{S}_2", Z, s * 0.15 * max(0.0, math.sin(ph * 2 + 0.5)) ** 8)
        add(r, f"Leg_{S}_1", Z, 0.12 * math.sin(ph - 1.0))
    return r


# Zombie_Scratch (the catch): it rears out of the water, too-long arms flung wide, small arms unfolding from under the
# ribs, the head splitting open into two halves of needle teeth; then it falls on you and clamps shut around you.
# The arms swing straight at a target direction in the body's own frame (no twist). Once reared, "along the body" is
# up and the belly faces you. wide = arms flung up and out; clamp = swung forward around you, forearms closing in.
# (v3 rotated them about fixed axes instead, which rolled them over in the slam: flaps up, fingers bent backwards.)
# 2 s at 30 fps, played at normal speed so the catch camera has time to show it: a coil, the rise, a held scream with
# the head split open, then the slam.
LUNGE = [  # frame, hips pitch, rise, forward, wide, clamp, jaw, small arms, finger curl, neck pitch, head shake
    (0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.2, 0.0, 0.0),
    (5, -0.15, -0.15, -0.05, 0.0, 0.0, 0.0, 0.0, 0.3, 0.2, 0.0),
    (16, 1.00, 0.60, -0.05, 0.7, 0.0, 0.40, 0.5, -0.25, -1.0, 0.0),
    (24, 1.25, 0.90, 0.0, 1.0, 0.0, 0.90, 1.0, -0.35, -1.65, 0.1),
    (32, 1.20, 0.95, 0.05, 1.0, 0.0, 0.95, 1.0, -0.35, -1.7, -0.1),
    (39, 0.65, 0.60, 0.55, 0.0, 1.0, 1.05, 1.1, 0.9, -1.25, 0.0),
    (45, 0.60, 0.55, 0.60, 0.0, 1.0, 1.05, 1.15, 1.2, -1.2, 0.12),
    (50, 0.60, 0.55, 0.60, 0.0, 1.0, 1.05, 1.15, 1.2, -1.2, -0.12),
    (55, 0.60, 0.55, 0.60, 0.0, 1.0, 1.05, 1.15, 1.2, -1.2, 0.12),
    (60, 0.60, 0.55, 0.60, 0.0, 1.0, 1.05, 1.15, 1.2, -1.2, 0.0),
]
LUNGE_FRAMES = LUNGE[-1][0]


def lunge_values(f):
    for a, b in zip(LUNGE, LUNGE[1:]):
        if a[0] <= f <= b[0]:
            k = smooth(a[0], b[0], f)
            return [lerp(a[i], b[i], k) for i in range(1, len(a))]
    return list(LUNGE[-1][1:])


def lunge(f):
    hp, rise, fwd, wide, clamp, jaw, small, curl, neck, shake = lunge_values(f)
    r = {}
    add(r, "Hips", X, hp)
    add(r, "Tail1", X, -hp * 0.7)
    add(r, "Tail2", X, -hp * 0.25)
    for i in range(3, 9):
        add(r, f"Tail{i}", Z, 0.15 * math.sin(f * 0.5 - i))
    add(r, "Spine1", X, 0.15 * hp)
    add(r, "Chest", X, 0.1 * hp)
    add(r, "Neck1", X, neck * 0.6)
    add(r, "Neck2", X, neck * 0.4)
    add(r, "Head", Y, 0.2 + shake)
    tremble = 0.06 * jaw * math.sin(f * 2.3)  # the open halves quiver
    for S in "LR":
        s = side(S)
        add(r, f"Jaw_{S}", Z, s * (jaw + tremble))
        add(r, f"Clav_{S}", X, 0.3 * small)                     # shoulders hunch up
        out = V((-s, 0, 0))  # this side's outward direction
        A = (BONES[f"UpperArm_{S}"][1] - BONES[f"UpperArm_{S}"][0]).normalized()
        target = A.lerp((out + Y * 0.5 + Z * 0.1).normalized(), wide).lerp((out * 0.35 + Y * 0.2 - Z).normalized(), clamp).normalized()
        swing = A.rotation_difference(target)
        axis, ang = swing.to_axis_angle()
        if ang > 1e-4:
            add(r, f"UpperArm_{S}", axis, ang)
        if clamp > 0:  # forearms fold in toward the middle, in front of the belly: a closing grip
            F = (BONES[f"Forearm_{S}"][1] - BONES[f"Forearm_{S}"][0]).normalized()
            inward = swing.inverted() @ (-out * 0.45 - Z * 0.85 + Y * 0.15).normalized()
            fold = Quaternion((1, 0, 0, 0)).slerp(F.rotation_difference(inward), clamp)
            axis, ang = fold.to_axis_angle()
            if ang > 1e-4:
                add(r, f"Forearm_{S}", axis, ang)
        for i in range(4):
            fan = (i - 1.5) * 0.18 * max(0.0, -curl) * 3
            for k in (1, 2):
                n = f"Finger_{S}_{i}_{k}"
                add(r, n, FINGER_AXIS[n], curl)
                if k == 1:
                    add(r, n, Z, fan * s)
        add(r, f"SmallArm_{S}_1", X, -0.9 * small)
        add(r, f"SmallArm_{S}_1", Z, s * 0.4 * small)
        add(r, f"SmallArm_{S}_2", Z, s * 1.3 * small)
        add(r, f"SmallClaw_{S}", Z, s * 0.5 * small)
        add(r, f"Leg_{S}_1", X, -0.4 * hp)
    return r, V((0, fwd, rise))


actions = []
info = new_action("Swim_Fwd_Loop", 0, 48, True)
for f in range(0, 49, 2):
    key_pose(f, swim(f / 48))
actions.append(finish_action(*info))
info = new_action("Swim_Idle_Loop", 0, 90, True)
for f in range(0, 91, 2):
    key_pose(f, idle(f / 90, f))
actions.append(finish_action(*info))
info = new_action("Zombie_Scratch", 0, LUNGE_FRAMES, False)
for f in range(0, LUNGE_FRAMES + 1):
    rots, loc = lunge(f)
    key_pose(f, rots, loc)
actions.append(finish_action(*info))
for a in actions:
    print("ACTION", a.name, tuple(a.frame_range))


# ---------------------------------------------------------------- skin texture (baked from a procedural shader)
def socket(node, name, kind, out=False):
    return next(s for s in (node.outputs if out else node.inputs) if s.name == name and s.type == kind)


def mix_color(nt, fac, a, b):
    m = nt.nodes.new("ShaderNodeMix")
    m.data_type = "RGBA"
    for sock, val in ((socket(m, "Factor", "VALUE"), fac), (socket(m, "A", "RGBA"), a), (socket(m, "B", "RGBA"), b)):
        if isinstance(val, tuple):
            sock.default_value = (*val, 1.0) if len(val) == 3 else val
        elif isinstance(val, (int, float)):
            sock.default_value = val
        else:
            nt.links.new(val, sock)
    return socket(m, "Result", "RGBA", out=True)


def map_range(nt, value, a, b, c, d):
    m = nt.nodes.new("ShaderNodeMapRange")
    m.clamp = True
    nt.links.new(value, m.inputs["Value"])
    m.inputs["From Min"].default_value, m.inputs["From Max"].default_value = a, b
    m.inputs["To Min"].default_value, m.inputs["To Max"].default_value = c, d
    return m.outputs["Result"]


def fields(nt):
    """Procedural fields shared by every bake pass, in object space (the rest pose)."""
    pos = nt.nodes.new("ShaderNodeTexCoord").outputs["Object"]
    warp = nt.nodes.new("ShaderNodeTexNoise")
    warp.inputs["Scale"].default_value, warp.inputs["Detail"].default_value = 2.5, 4.0
    nt.links.new(pos, warp.inputs["Vector"])
    off = nt.nodes.new("ShaderNodeVectorMath")
    off.operation = "MULTIPLY_ADD"
    nt.links.new(warp.outputs["Color"], off.inputs[0])
    off.inputs[1].default_value = (0.18, 0.18, 0.18)
    nt.links.new(pos, off.inputs[2])

    def maximum(x, y):
        mx = nt.nodes.new("ShaderNodeMath")
        mx.operation = "MAXIMUM"
        nt.links.new(x, mx.inputs[0])
        nt.links.new(y, mx.inputs[1])
        return mx.outputs[0]

    def noise(scale, detail, vec=None):
        n = nt.nodes.new("ShaderNodeTexNoise")
        n.inputs["Scale"].default_value, n.inputs["Detail"].default_value = scale, detail
        nt.links.new(vec or pos, n.inputs["Vector"])
        return n.outputs["Fac"]

    def ridges(scale, width, strength):
        sub = nt.nodes.new("ShaderNodeMath")
        sub.operation = "SUBTRACT"
        nt.links.new(noise(scale, 3.0, off.outputs["Vector"]), sub.inputs[0])
        sub.inputs[1].default_value = 0.5
        ab = nt.nodes.new("ShaderNodeMath")
        ab.operation = "ABSOLUTE"
        nt.links.new(sub.outputs[0], ab.inputs[0])
        return map_range(nt, ab.outputs[0], 0.0, width, strength, 0.0)

    def near(point, inner, outer):
        d = nt.nodes.new("ShaderNodeVectorMath")
        d.operation = "DISTANCE"
        nt.links.new(pos, d.inputs[0])
        d.inputs[1].default_value = point
        return map_range(nt, d.outputs["Value"], inner, outer, 1.0, 0.0)

    def mul(x, y):
        m = nt.nodes.new("ShaderNodeMath")
        m.operation = "MULTIPLY"
        nt.links.new(x, m.inputs[0])
        if isinstance(y, float):
            m.inputs[1].default_value = y
        else:
            nt.links.new(y, m.inputs[1])
        return m.outputs[0]

    main_veins = maximum(ridges(2.2, 0.022, 1.0), ridges(5.5, 0.016, 0.75))
    veins = maximum(main_veins, ridges(13.0, 0.012, 0.45))
    xyz = nt.nodes.new("ShaderNodeSeparateXYZ")
    nt.links.new(pos, xyz.inputs[0])
    absx = nt.nodes.new("ShaderNodeMath")
    absx.operation = "ABSOLUTE"
    nt.links.new(xyz.outputs["X"], absx.inputs[0])
    length = nt.nodes.new("ShaderNodeVectorMath")
    length.operation = "LENGTH"
    nt.links.new(pos, length.inputs[0])
    breakup = map_range(nt, noise(9.0, 6.0), 0.4, 0.62, 0.0, 1.0)
    blood = maximum(mul(near((0.0, 1.02, 0.22), 0.10, 0.36), breakup),                    # around the mouth
                    mul(map_range(nt, absx.outputs[0], 0.95, 1.15, 0.0, 1.0), breakup))  # the hands
    band = map_range(nt, xyz.outputs["Z"], 0.02, 0.10, 0.0, 1.0)
    band_top = map_range(nt, xyz.outputs["Z"], 0.10, 0.17, 1.0, 0.0)
    rot = mul(mul(band, band_top), map_range(nt, noise(5.0, 8.0), 0.35, 0.6, 0.0, 1.0))   # algae and rot at the waterline
    attr = nt.nodes.new("ShaderNodeAttribute")
    attr.attribute_name = "bone"
    flap = nt.nodes.new("ShaderNodeAttribute")
    flap.attribute_name = "flap"
    pores = nt.nodes.new("ShaderNodeTexVoronoi")
    pores.inputs["Scale"].default_value = 240.0
    nt.links.new(pos, pores.inputs["Vector"])
    return {"pos": pos, "veins": veins, "main_veins": main_veins, "mott": noise(4.0, 8.0), "z": xyz.outputs["Z"],
            "len": length.outputs["Value"], "blood": blood, "rot": rot, "bone": attr.outputs["Fac"], "flap": flap.outputs["Fac"],
            "wrinkle": noise(38.0, 8.0), "pores": map_range(nt, pores.outputs["Distance"], 0.0, 0.3, 1.0, 0.0), "mul": mul}


def skin_color(nt, f):
    """Drowned skin: pale mottling, a darker slate back, bruised purple extremities, algae at the waterline, blood
    around the mouth and on the hands, a web of dark veins. Teeth and spines: stained ivory."""
    col = mix_color(nt, map_range(nt, f["mott"], 0.35, 0.7, 0.0, 1.0), (0.82, 0.85, 0.81), (0.55, 0.62, 0.58))
    col = mix_color(nt, map_range(nt, f["z"], 0.04, 0.22, 0.0, 0.75), col, (0.30, 0.36, 0.38))
    col = mix_color(nt, map_range(nt, f["len"], 1.0, 2.6, 0.0, 0.8), col, (0.36, 0.24, 0.32))
    col = mix_color(nt, f["rot"], col, (0.22, 0.27, 0.15))
    col = mix_color(nt, f["mul"](f["veins"], 0.8), col, (0.17, 0.12, 0.20))
    col = mix_color(nt, f["mul"](f["blood"], 0.9), col, (0.33, 0.02, 0.03))
    col = mix_color(nt, f["flap"], col, mix_color(nt, f["wrinkle"], (0.20, 0.21, 0.17), (0.07, 0.06, 0.07)))
    ivory = mix_color(nt, f["wrinkle"], (0.86, 0.82, 0.70), (0.62, 0.55, 0.42))
    return mix_color(nt, f["bone"], col, ivory)


def flesh_color(nt, f):
    """Inside the head and the claws: raw gums with dark folds."""
    return mix_color(nt, map_range(nt, f["wrinkle"], 0.35, 0.65, 0.0, 1.0), (0.42, 0.05, 0.06), (0.16, 0.01, 0.02))


def smoothness(nt, f):
    """Wet everywhere, wetter on the veins and the blood, dull where it rots."""
    s = map_range(nt, f["rot"], 0.0, 1.0, 0.8, 0.35)
    m = nt.nodes.new("ShaderNodeMath")
    m.operation = "MAXIMUM"
    nt.links.new(s, m.inputs[0])
    nt.links.new(map_range(nt, f["blood"], 0.0, 1.0, 0.0, 0.92), m.inputs[1])
    return m.outputs[0]


def height(nt, f):
    """Bump: fine wrinkles, pores pressed in, veins standing out under the skin."""
    h = nt.nodes.new("ShaderNodeMath")
    h.operation = "MULTIPLY_ADD"
    nt.links.new(f["wrinkle"], h.inputs[0])
    h.inputs[1].default_value = 0.5
    nt.links.new(f["veins"], h.inputs[2])
    p = nt.nodes.new("ShaderNodeMath")
    p.operation = "SUBTRACT"
    nt.links.new(h.outputs[0], p.inputs[0])
    nt.links.new(f["mul"](f["pores"], 0.25), p.inputs[1])
    return p.outputs[0]


def bake_pass(kind, size, path, color_space="sRGB"):
    """One Cycles bake over the whole joined mesh into a new image; returns it (saved to path if given)."""
    img = bpy.data.images.new(f"bake_{kind}", size, size, alpha=False)
    img.colorspace_settings.name = color_space
    for mat in MATS:
        nt = mat.node_tree
        nt.nodes.clear()
        out = nt.nodes.new("ShaderNodeOutputMaterial")
        f = fields(nt) if mat in (SKIN, MOUTH) else None
        if kind == "normal":
            bsdf = nt.nodes.new("ShaderNodeBsdfPrincipled")
            nt.links.new(bsdf.outputs[0], out.inputs["Surface"])
            if f is not None:
                bump = nt.nodes.new("ShaderNodeBump")
                bump.inputs["Strength"].default_value = 0.8
                bump.inputs["Distance"].default_value = 0.0025
                nt.links.new(height(nt, f), bump.inputs["Height"])
                nt.links.new(bump.outputs["Normal"], bsdf.inputs["Normal"])
        else:
            em = nt.nodes.new("ShaderNodeEmission")
            nt.links.new(em.outputs[0], out.inputs["Surface"])
            if kind == "color":
                if mat is SKIN:
                    nt.links.new(skin_color(nt, f), em.inputs["Color"])
                elif mat is MOUTH:
                    nt.links.new(flesh_color(nt, f), em.inputs["Color"])
                else:
                    em.inputs["Color"].default_value = (*mat.diffuse_color[:3], 1.0)
            elif kind == "smooth":
                if f is not None:
                    nt.links.new(smoothness(nt, f), em.inputs["Color"])
                else:
                    em.inputs["Color"].default_value = (0.8, 0.8, 0.8, 1.0)
            elif kind == "veins":
                if mat is SKIN:
                    v = f["mul"](f["main_veins"], map_range(nt, f["bone"], 0.0, 1.0, 1.0, 0.0))
                    nt.links.new(v, em.inputs["Color"])
                else:
                    em.inputs["Color"].default_value = (0, 0, 0, 1)
        tex = nt.nodes.new("ShaderNodeTexImage")
        tex.image = img
        nt.nodes.active = tex
    activate(body)
    if kind == "normal":
        bpy.ops.object.bake(type="NORMAL", normal_space="TANGENT", margin=8)
    else:
        bpy.ops.object.bake(type="EMIT", margin=8)
    if path:
        img.filepath_raw = os.path.abspath(path)
        img.file_format = "PNG"
        img.save()
        print("BAKED", path)
    return img


def bake_maps(folder):
    """T_Listener_Skin (RGB colour, A smoothness), T_Listener_Normal (tangent space, OpenGL), T_Listener_Veins (emission)."""
    scene.render.engine = "CYCLES"
    scene.cycles.samples = 1
    scene.cycles.device = "CPU"
    rig.data.pose_position = "REST"  # bake the rest shape: object coordinates = the modelled body
    color = bake_pass("color", 2048, None)
    smooth = bake_pass("smooth", 2048, None)
    px = np.array(color.pixels[:]).reshape(-1, 4)
    px[:, 3] = np.array(smooth.pixels[:]).reshape(-1, 4)[:, 0]
    skin = bpy.data.images.new("T_Listener_Skin", 2048, 2048, alpha=True)
    skin.pixels.foreach_set(px.ravel().astype(np.float32))
    skin.filepath_raw = os.path.join(folder, "T_Listener_Skin.png")
    skin.file_format = "PNG"
    skin.save()
    print("BAKED", skin.filepath_raw)
    bake_pass("normal", 2048, os.path.join(folder, "T_Listener_Normal.png"), "Non-Color")
    bake_pass("veins", 1024, os.path.join(folder, "T_Listener_Veins.png"))
    rig.data.pose_position = "POSE"
    return skin  # the saved PNG, so the .blend shows the texture when opened (an in-memory bake would come back black)


def preview_shaders(img):
    """Lit materials for the dark EEVEE previews (Unity builds its own from the same maps)."""
    normal = bpy.data.images.load(os.path.join(MAP_DIR, "T_Listener_Normal.png"))
    normal.colorspace_settings.name = "Non-Color"
    for mat in MATS:
        nt = mat.node_tree
        nt.nodes.clear()
        bsdf = nt.nodes.new("ShaderNodeBsdfPrincipled")
        nt.links.new(bsdf.outputs[0], nt.nodes.new("ShaderNodeOutputMaterial").inputs["Surface"])
        bsdf.inputs["Roughness"].default_value = mat.roughness
        bsdf.inputs["Base Color"].default_value = (*mat.diffuse_color[:3], 1.0)
        if mat in (SKIN, MOUTH):
            tex = nt.nodes.new("ShaderNodeTexImage")
            tex.image = img
            nt.links.new(tex.outputs["Color"], bsdf.inputs["Base Color"])
            nm = nt.nodes.new("ShaderNodeNormalMap")
            ntex = nt.nodes.new("ShaderNodeTexImage")
            ntex.image = normal
            nt.links.new(ntex.outputs["Color"], nm.inputs["Color"])
            nt.links.new(nm.outputs["Normal"], bsdf.inputs["Normal"])
        if mat in (GLOW, THROAT):
            bsdf.inputs["Emission Color"].default_value = (*mat.diffuse_color[:3], 1.0)
            bsdf.inputs["Emission Strength"].default_value = 6.0 if mat is GLOW else 1.5


MAP_DIR = os.path.dirname(os.path.abspath(OUT_FBX)) if OUT_FBX else os.path.abspath(RENDER_DIR or ".")
skin_img = bake_maps(MAP_DIR)
preview_shaders(skin_img)

# ---------------------------------------------------------------- previews
def render_previews(folder):
    os.makedirs(folder, exist_ok=True)
    scene.render.engine = "BLENDER_WORKBENCH"
    sh = scene.display.shading
    sh.light, sh.color_type = "STUDIO", "TEXTURE"
    sh.show_specular_highlight = True
    sh.show_backface_culling = True  # as Unity draws it
    sh.show_cavity = True
    sh.cavity_type = "BOTH"
    sh.background_type = "VIEWPORT"
    sh.background_color = (0.03, 0.05, 0.06)
    scene.render.resolution_x, scene.render.resolution_y = 960, 640
    scene.render.film_transparent = False
    cam = bpy.data.objects.new("Cam", bpy.data.cameras.new("Cam"))
    scene.collection.objects.link(cam)
    scene.camera = cam
    bm = bmesh.new()
    bmesh.ops.create_grid(bm, x_segments=1, y_segments=1, size=8)
    water_mat = material("Water", (0.02, 0.07, 0.08), 0.05)
    water = new_object("Water", bm, [water_mat])
    water.location.z = 0.05

    def shot(name, action, frame, loc, target, lens=35, show_water=False):
        rig.animation_data.action = action
        scene.frame_set(frame)
        water.hide_render = not show_water
        if loc is None:  # frame the posed head from in front of its face
            hb = rig.pose.bones["Head"]
            mat = rig.matrix_world.to_3x3() @ hb.matrix.to_3x3()
            target = rig.matrix_world @ hb.center
            loc = target + (mat @ V((0, 0, 1))) * 0.5 + (mat @ V((0, 1, 0))) * 1.0
        cam.location = V(loc)
        cam.rotation_euler = (V(target) - V(loc)).to_track_quat("-Z", "Y").to_euler()
        cam.data.lens = lens
        scene.render.filepath = os.path.join(folder, name + ".png")
        bpy.ops.render.render(write_still=True)

    sw, idl, lg = actions
    shot("01_side", sw, 0, (4.6, -0.8, 0.9), (0, -0.9, 0), 32)
    shot("02_top", sw, 12, (0, -1.0, 6.5), (0, -1.0, 0), 32)
    shot("03_front34", sw, 0, (1.6, 2.3, 0.7), (0, 0.4, 0.05), 40)
    shot("04_face", sw, 0, (0.7, 2.1, 0.45), (0, 0.85, 0.27), 50)
    shot("05_glimpse", sw, 24, (1.8, 3.2, 1.6), (0, 0, 0.05), 35, True)
    shot("06_lunge_peak", lg, 28, (1.2, 3.6, 1.0), (0, 0.2, 1.3), 30, True)
    shot("08_lunge_clamp", lg, 52, (0.7, 3.0, 0.8), (0, 0.6, 1.0), 30, True)
    shot("09_mouth", lg, 28, None, None, 50)

    # In the dark, as the pool will show it: only a weak cold lamp, the glow does the rest.
    try:
        scene.render.engine = "BLENDER_EEVEE"
    except TypeError:
        scene.render.engine = "BLENDER_EEVEE_NEXT"
    world = bpy.data.worlds.new("Dark")
    world.use_nodes = True
    world.node_tree.nodes["Background"].inputs["Color"].default_value = (0.004, 0.007, 0.009, 1)
    scene.world = world
    water_mat.use_nodes = True
    wb = water_mat.node_tree.nodes.get("Principled BSDF")
    if wb:
        wb.inputs["Base Color"].default_value = (0.01, 0.03, 0.035, 1)
        wb.inputs["Roughness"].default_value = 0.05
    lamp = bpy.data.objects.new("Lamp", bpy.data.lights.new("Lamp", "POINT"))
    lamp.data.energy, lamp.data.color = 60.0, (0.7, 0.85, 1.0)
    lamp.location = (2.5, 3.5, 3.0)
    scene.collection.objects.link(lamp)
    try:
        scene.eevee.use_bloom = True
    except AttributeError:
        pass
    shot("10_dark_glimpse", sw, 24, (1.8, 3.2, 1.6), (0, -0.4, 0.05), 32, True)
    shot("11_dark_lunge", lg, 30, (0.6, 3.4, 0.6), (0, 0.4, 1.3), 28, True)
    shot("12_dark_mouth", lg, 30, None, None, 45)
    bpy.data.objects.remove(lamp)
    bpy.data.objects.remove(water)
    bpy.data.objects.remove(cam)


if RENDER_DIR:
    render_previews(RENDER_DIR)

rig.animation_data.action = actions[0]
scene.frame_set(0)
if OUT_BLEND:
    bpy.ops.wm.save_as_mainfile(filepath=os.path.abspath(OUT_BLEND))
if OUT_FBX:
    for o in scene.objects:
        o.select_set(o in (rig, body))
    bpy.ops.export_scene.fbx(
        filepath=os.path.abspath(OUT_FBX), use_selection=True, object_types={"ARMATURE", "MESH"},
        # Default axes: the model faces Unity -Z after import; ListenerPrefabBuilder turns it 180 degrees inside the prefab.
        global_scale=SCALE, apply_scale_options="FBX_SCALE_ALL", add_leaf_bones=False, mesh_smooth_type="FACE",
        bake_anim=True, bake_anim_use_all_actions=True, bake_anim_use_nla_strips=False,
        bake_anim_force_startend_keying=True, bake_anim_simplify_factor=0.5)
    print("EXPORTED", OUT_FBX)