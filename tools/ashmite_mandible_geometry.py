"""Authored paired Ashmite feeding pincers, preserving existing mandible anchors.

The inward-returning tips, dorsal keratin keel and three inner denticles are
physical topology, not texture-only markings. No Blender dependency for tests.
"""
from math import cos, exp, isfinite, pi, sin, sqrt

RINGS = 17
SIDES = 24
CONTRACT = 'ashmite-paired-hooked-serrated-mandibles-17x24'


def anchors(side):
    if side not in (-1, 1):
        raise ValueError('Mandible side must be -1 (left) or +1 (right)')
    return ((side*.034, .142, .065), (side*.060, .195, .045))


def centerline(side, t):
    if side not in (-1, 1) or not isfinite(t) or not 0 <= t <= 1:
        raise ValueError('Invalid Ashmite mandible side or parameter')
    a, b = anchors(side)
    # Bulge outward before returning inward at the original tip anchor.
    bulge = sin(pi*t)**2
    return (a[0] + (b[0]-a[0])*t + side*.023*bulge,
            a[1] + (b[1]-a[1])*t,
            a[2] + (b[2]-a[2])*t + .007*bulge)


def _frame(side, t):
    lo = max(0, t-1e-4)
    hi = min(1, t+1e-4)
    a, b = centerline(side, lo), centerline(side, hi)
    d = [b[k]-a[k] for k in range(3)]
    norm = sqrt(sum(v*v for v in d))
    tangent = [v/norm for v in d]
    # Project world X onto the normal plane. The Y advance guarantees stability.
    x = [(1. if k == 0 else 0.)-tangent[0]*tangent[k] for k in range(3)]
    xn = sqrt(sum(v*v for v in x))
    x = [v/xn for v in x]
    z = [tangent[1]*x[2]-tangent[2]*x[1],
         tangent[2]*x[0]-tangent[0]*x[2],
         tangent[0]*x[1]-tangent[1]*x[0]]
    return x, z


def mandible_mesh_data(side):
    """Return mesh-local vertices, closed outward faces and authored loop UVs."""
    start, end = anchors(side)
    vertices = []
    for i in range(RINGS):
        t = i/(RINGS-1)
        c = centerline(side, t)
        xaxis, zaxis = _frame(side, t)
        radius = .012*(1-t)**1.10 + .0015
        for j in range(SIDES):
            angle = 2*pi*j/SIDES
            if side == -1: angle = pi-angle
            co, si = cos(angle), sin(angle)
            inner = max(0., -side*co)**10
            dorsal = max(0., -si)**10
            teeth = sum(exp(-((t-pos)/.050)**2) for pos in (.25, .50, .75))
            width = radius*(1 + .20*inner*teeth + .14*dorsal)
            height = radius*.76*(1 + .28*dorsal)
            vertices.append(tuple(c[k]-start[k]+width*co*xaxis[k]+height*si*zaxis[k] for k in range(3)))
    # Distinct cap centers retain the existing root/tip anchors exactly.
    vertices += [(0.,0.,0.), tuple(end[k]-start[k] for k in range(3))]
    root, tip = RINGS*SIDES, RINGS*SIDES+1
    faces, uv_faces = [], []
    for j in range(SIDES):
        k = (j+1)%SIDES
        faces.append((root,k,j))
        a, b = 2*pi*j/SIDES, 2*pi*k/SIDES
        uv_faces.append(((.5,.5),(.5+.48*cos(b),.5+.48*sin(b)),(.5+.48*cos(a),.5+.48*sin(a))))
    for i in range(RINGS-1):
        for j in range(SIDES):
            k=(j+1)%SIDES
            faces.append((i*SIDES+j,i*SIDES+k,(i+1)*SIDES+k,(i+1)*SIDES+j))
            uv_faces.append(((j/SIDES,i/(RINGS-1)),((j+1)/SIDES,i/(RINGS-1)),
                             ((j+1)/SIDES,(i+1)/(RINGS-1)),(j/SIDES,(i+1)/(RINGS-1))))
    for j in range(SIDES):
        k=(j+1)%SIDES
        faces.append((tip,(RINGS-1)*SIDES+j,(RINGS-1)*SIDES+k))
        a,b=2*pi*j/SIDES,2*pi*k/SIDES
        uv_faces.append(((.5,.5),(.5+.48*cos(a),.5+.48*sin(a)),(.5+.48*cos(b),.5+.48*sin(b))))
    if side == -1:
        faces = [tuple(reversed(face)) for face in faces]
        uv_faces = [tuple(reversed(uv)) for uv in uv_faces]
    return vertices,faces,uv_faces


def sculpted_mandible(name, side, material):
    """Blender adapter; rigid bind remains with the existing Mandible_L/R bone."""
    import bpy
    start, _ = anchors(side)
    vertices, faces, uvs = mandible_mesh_data(side)
    mesh = bpy.data.meshes.new(name+'_Mesh')
    mesh.from_pydata(vertices, [], faces)
    mesh.update()
    obj = bpy.data.objects.new(name, mesh)
    bpy.context.collection.objects.link(obj)
    obj.location = start
    mesh.materials.append(material)
    obj['magenheim_ashmite_mandible_contract'] = CONTRACT
    obj['magenheim_ashmite_mandible_side'] = side
    layer = mesh.uv_layers.new(name='AshmiteUV')
    for poly, face_uv in zip(mesh.polygons, uvs):
        poly.use_smooth = len(poly.vertices) == 4
        for loop, pair in zip(poly.loop_indices, face_uv):
            layer.data[loop].uv = pair
    return obj
