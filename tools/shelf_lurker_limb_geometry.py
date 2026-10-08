"""Shelf Lurker's six three-part gripping limbs: authored, curved cuticle anatomy.

The pure geometry path can be tested without Blender. The Blender adapter uses
one existing material and the existing rigid limb-bone assignments.
"""
from math import cos, sin, pi, sqrt, isfinite

RINGS = 13
SIDES = 16
CONTRACT = 'curved-sclerite-13x16'


def _add(a, b):
    return tuple(x + y for x, y in zip(a, b))


def _mul(a, scalar):
    return tuple(x * scalar for x in a)


def _cross(a, b):
    return (a[1]*b[2]-a[2]*b[1], a[2]*b[0]-a[0]*b[2], a[0]*b[1]-a[1]*b[0])


def _unit(a):
    length = sqrt(sum(x*x for x in a))
    if length <= 1e-8:
        raise ValueError('Zero-length anatomical direction')
    return _mul(a, 1/length)


def limb_mesh_data(start, end, radius):
    """Return closed 13-ring cuticle with a bowed centerline and tapered joints.

    Joint endpoints remain on the original armature bone axes. The small lateral
    bow, compressed oval section and two raised dorsal sclerite bands create
    readable fungal-arthropod limb anatomy without changing the six-limbed plan.
    """
    a, b = tuple(float(v) for v in start), tuple(float(v) for v in end)
    r = float(radius)
    if len(a) != 3 or len(b) != 3 or not all(map(isfinite, a+b+(r,))):
        raise ValueError('Nonfinite or malformed limb coordinates')
    delta = tuple(y-x for x, y in zip(a, b))
    length = sqrt(sum(x*x for x in delta))
    if not .12 <= length <= 3.0 or not .025 <= r <= .20:
        raise ValueError('Limb span/radius outside approved Shelf Lurker anatomy')
    axis = _unit(delta)
    reference = (0., 0., 1.) if abs(axis[2]) < .9 else (0., 1., 0.)
    # Keep the armored dorsal surface facing world-up on both mirrored sides.
    # Flip BOTH transverse axes to preserve the right-handed winding contract.
    lateral = _unit(_cross(reference, axis))
    dorsal = _cross(axis, lateral)
    verts = []
    for i in range(RINGS):
        t = i/(RINGS-1)
        center = _add(_add(a, _mul(delta, t)), _mul(lateral, .62*r*sin(pi*t)))
        # Proximal mass and distal taper; the endpoints remain full cross-sections
        # so the articulated joints do not turn into needle-like hinge points.
        taper = (1.04-.38*t)*(1+.13*sin(pi*t))
        for j in range(SIDES):
            angle = 2*pi*j/SIDES
            # Physical dorsal sclerites, not painted stripes: two broad shields
            # separated by a constricted seam. The underside stays pliable and
            # the joint ends remain uninflated for animation clearance.
            dorsal_exposure = max(0., sin(angle))**2
            band = max(0., cos(6*pi*t))
            sclerite = 1. + dorsal_exposure * sin(pi*t)**2 * (.06 + .15*band)
            ridge = 1. + .045*cos(3*angle+1.2*pi*t)*sin(pi*t)
            width = r*taper*ridge*sclerite
            verts.append(_add(center, _add(_mul(lateral, width*cos(angle)),
                                           _mul(dorsal, .86*width*sin(angle)))))
    faces = []
    for i in range(RINGS-1):
        for j in range(SIDES):
            k = (j+1)%SIDES
            faces.append((i*SIDES+j, i*SIDES+k, (i+1)*SIDES+k, (i+1)*SIDES+j))
    faces.append(tuple(reversed(range(SIDES))))
    faces.append(tuple((RINGS-1)*SIDES+j for j in range(SIDES)))
    return verts, faces


def limb_segment(name, start, end, radius, material):
    """Blender-only adapter; explicit UVs and material preserve PBR identity."""
    import bpy
    verts, faces = limb_mesh_data(start, end, radius)
    mesh = bpy.data.meshes.new(name+'_Mesh')
    mesh.from_pydata(verts, [], faces)
    mesh.update()
    obj = bpy.data.objects.new(name, mesh)
    bpy.context.collection.objects.link(obj)
    mesh.materials.append(material)
    obj['magenheim_limb_contract'] = CONTRACT
    for poly in mesh.polygons:
        poly.use_smooth = len(poly.vertices) == 4
    uv = mesh.uv_layers.new(name='ShelfLurkerUV')
    for poly in mesh.polygons:
        ids = [mesh.loops[li].vertex_index%SIDES for li in poly.loop_indices]
        seam = 0 in ids and SIDES-1 in ids
        for li in poly.loop_indices:
            idx = mesh.loops[li].vertex_index
            angular = idx%SIDES
            if len(poly.vertices) > 4:
                angle = 2*pi*angular/SIDES
                uv.data[li].uv = (.5+.45*cos(angle), .5+.45*sin(angle))
            else:
                uv.data[li].uv = (1. if seam and angular == 0 else angular/SIDES,
                                  (idx//SIDES)/(RINGS-1))
    return obj
