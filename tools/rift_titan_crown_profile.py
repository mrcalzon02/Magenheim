"""Rift Titan r9 primary silhouette parameters, independent of Blender.

The r8 head reads as a straight upright spike in side clay. The adopted
left-facing concept has a forward-raking fractured crest over a much fuller
shoulder-to-collar transition. These are shape corrections, not surface detail.
"""
from math import isfinite

CROWN_PARTS = ('SplitCrown0', 'SplitCrown1', 'SplitCrown2',
               'FloatingPrism0', 'FloatingPrism1')
CROWN_BASE_Z = 3.12
CROWN_TIP_Z = 4.43
TIP_FORWARD = 0.42  # authoring units; +Y is forward (screen-left in side view)


def smoothstep(value, start, stop):
    if not all(isfinite(float(x)) for x in (value, start, stop)) or stop <= start:
        raise ValueError('finite, increasing silhouette stations required')
    t = max(0.0, min(1.0, (value - start) / (stop - start)))
    return t * t * (3.0 - 2.0 * t)


def crown_point(name, point):
    """Lean only the upper crest; retain the lower jaw and six neck-rib roots."""
    if name not in CROWN_PARTS:
        raise ValueError('r9 crown transform is restricted to approved crest parts')
    xyz = tuple(float(v) for v in point)
    if len(xyz) != 3 or not all(map(isfinite, xyz)):
        raise ValueError('finite XYZ crest vertex required')
    x, y, z = xyz
    t = smoothstep(z, CROWN_BASE_Z, CROWN_TIP_Z)
    # Slightly splay the two stone halves in front, keeping the central cleft.
    side = -1 if name == 'SplitCrown0' else (1 if name == 'SplitCrown1' else 0)
    return (x + side * 0.045 * t, y + TIP_FORWARD * t, z)


def mantle_spec(side):
    """Repurpose r8's paired floating upper stones into attached-looking thorax masses.

    Broad, diagonal plates visually bridge the inner collar and the existing
    sloped shoulder stones without adding faces or entering the open core.
    """
    if side not in (-1, 1):
        raise ValueError('mantle side must be -1 or +1')
    return dict(center=(side * 0.56, 0.045, 3.00),
                width=0.335, height=0.58, depth=0.235,
                axis=(side * 0.53, 0.06, 1.0),
                variant=2, seed=29 if side == -1 else 30)

def build(B, arm, record, profile, tube, cap):
    """Reuse r8 meshes and bones; change only the crown and upper mantle."""
    import bpy, bmesh, json
    from math import sin, cos
    from mathutils import Vector
    import rift_titan_joint_silhouette as r8
    from pathlib import Path
    out = Path(__file__).resolve().parents[1] / 'docs/art/creature-models/2026-10-13/rift-titan-crown-r9'
    r8.OUT = out
    trunk = r8.build(B, arm, record, profile, tube, cap)
    meshes = [o for o in bpy.context.scene.objects if o.type == 'MESH']
    count = sum(len(o.data.polygons) for o in meshes)
    for name in CROWN_PARTS:
        for v in bpy.data.objects[name].data.vertices:
            v.co = crown_point(name, v.co)
    for name, side in (('FloatingStratum0', -1), ('FloatingStratum1', 1)):
        o = bpy.data.objects[name]
        spec = mantle_spec(side)
        center = Vector(spec['center'])
        basis = r8.frame(spec['axis'])
        w, h, d, seed = (spec[k] for k in ('width', 'height', 'depth', 'seed'))
        pts = []
        for face_side in (-1, 1):
            for k, (u, z) in enumerate(r8.OUTLINES[spec['variant']]):
                taper = .42 + .58 * max(0, 1 - abs(z) / .55)
                pts.append(center + basis @ Vector((
                    u * w * (1 + .045 * sin(seed + k * 2.1)) * (1 if face_side > 0 else .88),
                    face_side * d * taper * (1 + .12 * cos(k * 1.9 + seed)),
                    z * h + face_side * .025 * h * sin(k * 1.5 + seed))))
        for face_side in (-1, 1):
            pts.append(center + basis @ Vector((
                w * .025 * sin(seed + 1.3), face_side * d * 1.04, h * .04 * cos(seed + .7))))
        assert len(pts) == len(o.data.vertices) == 14
        for v, p in zip(o.data.vertices, pts):
            v.co = p
        bm = bmesh.new()
        bm.from_mesh(o.data)
        bmesh.ops.recalc_face_normals(bm, faces=list(bm.faces))
        bm.to_mesh(o.data)
        bm.free()
    assert sum(len(o.data.polygons) for o in meshes) == count
    contacts = json.loads(bpy.context.scene['tissue_attachment_contract'])
    assert len(contacts) == 58
    assert all(c['support'] not in CROWN_PARTS and c['tissue'] not in CROWN_PARTS for c in contacts)
    out.mkdir(parents=True, exist_ok=True)
    (out / 'crown-authoring.json').write_text(json.dumps({
        'change': 'forward crest and diagonal thorax mantle; r8 rig, UVs, roots and faces retained',
        'status': 'unaccepted silhouette candidate; Blender and native verification pending'}, indent=2) + '\n')
    return trunk


if __name__ == '__main__':
    import sys
    from pathlib import Path
    import json
    sys.modules['rift_titan_crown_profile'] = sys.modules[__name__]
    root = Path(__file__).resolve().parents[1]
    runner = root / 'tools/refine-rift-titan-joints-r8.py'
    source = runner.read_text(encoding='utf-8').replace('rift-titan-joints-r8', 'rift-titan-crown-r9').replace('rift_titan_joint_silhouette', 'rift_titan_crown_profile')
    exec(compile(source, str(__file__), 'exec'))
    output = root / 'docs/art/creature-models/2026-10-13/rift-titan-crown-r9'
    metrics = output / 'refinement-metrics.json'
    row = json.loads(metrics.read_text(encoding='utf-8'))
    row.update(previous_id='underworld-boss-rift-titan-joints-r8',
               status='unaccepted silhouette candidate; Blender/native acceptance pending',
               changes='forward-raking crown and paired collar-to-shoulder mantle stones')
    metrics.write_text(json.dumps(row, indent=2) + '\n', encoding='utf-8')
