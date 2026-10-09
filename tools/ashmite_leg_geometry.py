"""Sculpted chitin femora and tibiae for the established six-legged Ashmite.

Retains the twelve original segment endpoints, rig bindings and heat-chitin
material. Pure geometry is Blender-independent for deterministic regression.
"""
from math import cos, exp, isfinite, pi, sin, sqrt

RINGS = 13
SIDES = 24
CONTRACT = 'ashmite-sculpted-hexapod-leg-13x24-v1'
KINDS = ('femur', 'tibia')


def _unit(v):
    length = sqrt(sum(x*x for x in v))
    if length <= 1e-8:
        raise ValueError('Degenerate Ashmite leg segment')
    return tuple(x/length for x in v)


def leg_mesh_data(start, end, kind):
    """Return local vertices, outward faces, per-face UVs, and the two cap anchors."""
    if kind not in KINDS or len(start) != 3 or len(end) != 3:
        raise ValueError('Invalid Ashmite leg anatomy')
    if not all(isfinite(x) for x in (*start, *end)):
        raise ValueError('Non-finite Ashmite leg anchor')
    delta = tuple(end[k]-start[k] for k in range(3))
    tangent = _unit(delta)
    # Project world up into the leg cross-section, avoiding camera-relative twist.
    up = (0.0, 1.0, 0.0) if abs(tangent[2]) > 0.95 else (0.0, 0.0, 1.0)
    projection = sum(up[q]*tangent[q] for q in range(3))
    zaxis = _unit(tuple(up[k]-projection*tangent[k] for k in range(3)))
    lateral = (zaxis[1]*tangent[2]-zaxis[2]*tangent[1],
               zaxis[2]*tangent[0]-zaxis[0]*tangent[2],
               zaxis[0]*tangent[1]-zaxis[1]*tangent[0])
    vertices = []
    for i in range(RINGS):
        t = i/(RINGS-1)
        center = tuple(delta[k]*t + (0.0025 if kind == 'femur' else 0.0015)
                       * sin(pi*t)*zaxis[k] for k in range(3))
        # Femora read as proximal armored wedges, tibiae as distal narrow tools.
        if kind == 'femur':
            radius = 0.011 + 0.0045*(1-t) + 0.0012*sin(pi*t)
            height_ratio = 0.72
            keel = 0.34
        else:
            radius = 0.0075 + 0.0045*(1-t)
            height_ratio = 0.70
            keel = 0.27
        # Two recessed mineral fracture seams are real surface relief.
        seam = sum(exp(-((t-p)/0.034)**2) for p in (0.35, 0.70))
        for j in range(SIDES):
            angle = 2*pi*j/SIDES
            co, si = cos(angle), sin(angle)
            dorsal = max(0.0, si)**12
            underside = max(0.0, -si)**8
            # Triangular upper carapace keel, recessed ventral channel, faceted sides.
            width = radius*(1 + 0.065*cos(4*angle))*(1-0.10*seam)
            height = radius*height_ratio*(1 + keel*dorsal - 0.11*underside)*(1-0.085*seam)
            vertices.append(tuple(center[k] + width*co*lateral[k] + height*si*zaxis[k]
                                  for k in range(3)))
    vertices.extend(((0., 0., 0.), delta))
    root, tip = RINGS*SIDES, RINGS*SIDES+1
    faces, uvs = [], []
    for j in range(SIDES):
        k = (j+1)%SIDES
        a, b = 2*pi*j/SIDES, 2*pi*k/SIDES
        faces.append((root, k, j))
        uvs.append(((.5,.5),(.5+.48*cos(b),.5+.48*sin(b)),(.5+.48*cos(a),.5+.48*sin(a))))
    for i in range(RINGS-1):
        for j in range(SIDES):
            k = (j+1)%SIDES
            faces.append((i*SIDES+j, i*SIDES+k, (i+1)*SIDES+k, (i+1)*SIDES+j))
            uvs.append(((j/SIDES,i/(RINGS-1)),((j+1)/SIDES,i/(RINGS-1)),
                        ((j+1)/SIDES,(i+1)/(RINGS-1)),(j/SIDES,(i+1)/(RINGS-1))))
    for j in range(SIDES):
        k = (j+1)%SIDES
        a, b = 2*pi*j/SIDES, 2*pi*k/SIDES
        faces.append((tip,(RINGS-1)*SIDES+j,(RINGS-1)*SIDES+k))
        uvs.append(((.5,.5),(.5+.48*cos(a),.5+.48*sin(a)),(.5+.48*cos(b),.5+.48*sin(b))))
    return vertices, faces, uvs


def sculpted_leg(name, start, end, kind, material):
    """Blender adapter: caller retains original Coxa/Femur rigid bone ownership."""
    import bpy
    vertices, faces, uvs = leg_mesh_data(start, end, kind)
    mesh = bpy.data.meshes.new(name+'_Mesh')
    mesh.from_pydata(vertices, [], faces)
    mesh.update()
    obj = bpy.data.objects.new(name, mesh)
    bpy.context.collection.objects.link(obj)
    obj.location = start
    mesh.materials.append(material)
    obj['magenheim_ashmite_leg_contract'] = CONTRACT
    obj['magenheim_ashmite_leg_kind'] = kind
    layer = mesh.uv_layers.new(name='AshmiteUV')
    for poly, face_uv in zip(mesh.polygons, uvs):
        poly.use_smooth = len(poly.vertices) == 4
        for loop, pair in zip(poly.loop_indices, face_uv):
            layer.data[loop].uv = pair
    return obj
