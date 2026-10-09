"""Sculpted cephalic shield for the established Sulfurous Wastes Ashmite.

Replaces only the generic ellipsoid head. Retains the approved head anchor,
0.16 x 0.144 x 0.096 m nominal envelope, existing eyes and feeding-tool bones.
Geometry is deterministic, closed, UV-authored and Blender-free for QA.
"""
from math import atan2, cos, exp, isfinite, pi, sin

RINGS = 22
SIDES = 64
ORIGIN = (0.0, 0.105, 0.070)
SCALE = (0.080, 0.072, 0.048)
CONTRACT = 'ashmite-cephalic-vent-shield-22x64-v1'


def _wrap_angle(a):
    return (a + pi) % (2*pi) - pi


def head_mesh_data(scale=SCALE):
    """Return local-space vertices and outward-wound polygons.

    Axis: +Y faces the mineral-scraping mouthparts. The cephalic shield is
    low, broader at the thorax, narrowed at the mandibles, with sculpted
    paired brow buttresses and a shallow median heat-suture. No new organs.
    """
    if len(scale) != 3 or not all(isfinite(v) and v > 0 for v in scale):
        raise ValueError('Head radii must be three positive finite values')
    rx, ry, rz = scale
    verts = [(0., -ry, 0.)]
    for i in range(RINGS):
        t = (i + 0.55) / (RINGS + .10)
        a = pi*t
        section = sin(a)**.56
        # Posterior shield stays broad; forward clypeus tapers under the eyes.
        width = rx * section * (1.18 - .55*t)
        height = rz * sin(a)**.65 * (1.07 - .20*t)
        y = ry * (2*t - 1)
        for j in range(SIDES):
            theta = 2*pi*j/SIDES
            xdir, zdir = cos(theta), sin(theta)
            # Twin raised brow roots are continuous shell geometry, not horns.
            brow = 0.
            if .54 < t < .94:
                for brow_angle in (pi*.26, pi*.74):
                    brow += exp(-(_wrap_angle(theta-brow_angle)/.19)**2) * exp(-((t-.76)/.18)**2)
            # Midline heat-suture, shallow enough to preserve the head's dome.
            suture = exp(-(_wrap_angle(theta-pi/2)/.095)**2) * exp(-((t-.46)/.33)**2)
            # Anterior shovel-lip reinforces the existing vent-scraper identity.
            lip = exp(-((t-.86)/.095)**2) * exp(-(_wrap_angle(theta+pi/2)/.30)**2)
            x = width*xdir*(1+.040*brow)
            z = height*zdir*(1.0 if zdir >= 0 else .68)
            z += rz*(.10*brow-.055*suture-.065*lip)
            # Deliberate plate-edge scallop, restrained at the front eye line.
            x *= 1+.009*cos(8*pi*t)*sin(theta)**2
            verts.append((x, y, z))
    verts.append((0., ry, 0.))
    rear = 0
    front = len(verts)-1
    faces = []
    # Outward orientation: rear cap normal -Y, front cap +Y.
    for j in range(SIDES):
        faces.append((rear, 1+j, 1+(j+1)%SIDES))
    for i in range(RINGS-1):
        a = 1+i*SIDES
        b = a+SIDES
        for j in range(SIDES):
            k = (j+1)%SIDES
            faces.append((a+j,b+j,b+k,a+k))
    last = 1+(RINGS-1)*SIDES
    for j in range(SIDES):
        faces.append((front,last+(j+1)%SIDES,last+j))
    return verts, faces


def head_uv(vertex, scale=SCALE):
    """Angular U and longitudinal V; unlike planar UVs, dorsal/ventral differ."""
    x, y, z = vertex
    u = (atan2(z/scale[2], x/scale[0])/(2*pi)) % 1.0
    return (u, (y+scale[1])/(2*scale[1]))


def face_uvs(face, vertices, scale=SCALE):
    """Seam-safe loop UVs with cap-center interpolation; all values in [0,1]."""
    center_ids = (0, len(vertices)-1)
    values = []
    for index in face:
        if index in center_ids:
            values.append(None)
        else:
            values.append(((index-1)%SIDES/SIDES,
                           (vertices[index][1]+scale[1])/(2*scale[1])))
    us = [p[0] for p in values if p is not None]
    crosses_seam = min(us)<.1 and max(us)>.9
    if crosses_seam:
        values = [((1.0 if u<.1 else u),v) if p is not None else None
                  for p in values for u,v in [p or (0,0)]]
    if any(p is None for p in values):
        avg=sum(p[0] for p in values if p is not None)/len(us)
        values = [(avg, 0.0 if face[i]==0 else 1.0) if p is None else p
                  for i,p in enumerate(values)]
    return values


def sculpted_head(name, material):
    """Blender adapter: preserve Head bone and PBR material in author script."""
    import bpy
    vertices, faces = head_mesh_data()
    mesh = bpy.data.meshes.new(name+'_Mesh')
    mesh.from_pydata(vertices, [], faces)
    mesh.update()
    obj = bpy.data.objects.new(name, mesh)
    bpy.context.collection.objects.link(obj)
    obj.location = ORIGIN
    mesh.materials.append(material)
    obj['magenheim_ashmite_head_contract'] = CONTRACT
    layer = mesh.uv_layers.new(name='AshmiteUV')
    for poly in mesh.polygons:
        poly.use_smooth = len(poly.vertices)==4
        uvs = face_uvs(tuple(poly.vertices), vertices)
        for li, pair in zip(poly.loop_indices, uvs):
            layer.data[li].uv = pair
    return obj
