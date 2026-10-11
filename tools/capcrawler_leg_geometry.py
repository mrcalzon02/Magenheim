"""Authored eight-pair Capcrawler leg armor: tapered, ridged and UV-island mapped.

Replaces only the 16 generic upper/lower cylindrical leg meshes. The established
hip, knee and foot coordinates, material identities, rig assignments, tarsal
pads and claws remain untouched. Pure geometry is independently testable.
"""
from math import cos, exp, pi, sin, sqrt

CONTRACT = 'capcrawler-tapered-leg-plates-v1'
UV_CONTRACT = 'capcrawler-leg-sidewall-endcaps-v1'
SECTORS = 20
RINGS = 7
LEG_Y = (-.24, -.08, .10, .26)


def anchors(side, index):
    if side not in (-1, 1) or index not in (1, 2, 3, 4):
        raise ValueError('Expected existing Capcrawler side -1/+1 and leg index 1..4')
    y = LEG_Y[index - 1]
    return ((side*.20, y, .22),
            (side*.34, y+(index-2.5)*.018, .13),
            (side*.43, y+(index-2.5)*.035, .035))


def _unit(a):
    d = sqrt(sum(v*v for v in a))
    if d <= 1e-9:
        raise ValueError('Degenerate limb axis')
    return tuple(v/d for v in a)


def _cross(a, b):
    return (a[1]*b[2]-a[2]*b[1],
            a[2]*b[0]-a[0]*b[2],
            a[0]*b[1]-a[1]*b[0])


def leg_mesh_data(side, index, section):
    """Return local vertices, outward faces, UV loops and original midpoint origin.

    Each segment is a single watertight plate, not intersecting cylinders.
    Section A is a flared upper coxa-to-knee shell; B is the tapered tibia.
    """
    hip, knee, foot = anchors(side, index)
    if section not in ('A', 'B'):
        raise ValueError('Only existing leg segments A/B are valid')
    start, end = (hip, knee) if section == 'A' else (knee, foot)
    origin = tuple((start[k]+end[k])*.5 for k in range(3))
    axis = _unit(tuple(end[k]-start[k] for k in range(3)))
    outward = (float(side), 0., 0.)
    proj = sum(outward[k]*axis[k] for k in range(3))
    normal = _unit(tuple(outward[k]-proj*axis[k] for k in range(3)))
    tangent = tuple(side*v for v in _cross(axis, normal))
    profiles = ((.74, 1.02, 1.26, 1.10, .94, 1.13, .76)
                if section == 'A' else
                (1.12, 1.03, .95, .87, .80, .70, .58))
    radius = .027 if section == 'A' else .020
    vertices = []
    for ring in range(RINGS):
        t = ring/(RINGS-1)
        center = tuple(start[k]+(end[k]-start[k])*t-origin[k] for k in range(3))
        for sector in range(SECTORS):
            theta = 2*pi*sector/SECTORS
            c, s = cos(theta), sin(theta)
            # Raised external ridge and a kneeward plated shoulder, rather
            # than a uniformly round, cylindrical limb.
            crest = max(0., c)**8
            knee_flare = (.23*exp(-((t-.80)/.17)**2)*crest
                          if section == 'A' else .09*exp(-((t-.12)/.20)**2)*crest)
            relief = 1 + .14*crest + knee_flare + .045*cos(6*theta)*(sin(pi*t)**2)
            r = radius*profiles[ring]*relief
            vertices.append(tuple(center[k] + r*(normal[k]*c + .76*tangent[k]*s)
                                  for k in range(3)))
    first_center = len(vertices)
    vertices.append(tuple(start[k]-origin[k] for k in range(3)))
    last_center = len(vertices)
    vertices.append(tuple(end[k]-origin[k] for k in range(3)))
    faces, uvs = [], []
    def add(face, uv):
        faces.append(tuple(reversed(face)) if side < 0 else face)
        uvs.append(tuple(reversed(uv)) if side < 0 else uv)

    for ring in range(RINGS-1):
        for sector in range(SECTORS):
            j, k = sector, (sector+1)%SECTORS
            face = (ring*SECTORS+j, ring*SECTORS+k,
                    (ring+1)*SECTORS+k, (ring+1)*SECTORS+j)
            u0 = .04 + .92*sector/SECTORS
            u1 = .04 + .92*(sector+1)/SECTORS
            v0 = .39 + .57*ring/(RINGS-1)
            v1 = .39 + .57*(ring+1)/(RINGS-1)
            add(face, ((u0,v0),(u1,v0),(u1,v1),(u0,v1)))
    for sector in range(SECTORS):
        j, k = sector, (sector+1)%SECTORS
        a, b = 2*pi*j/SECTORS, 2*pi*k/SECTORS
        add((first_center,k,j), ((.25,.19),(.25+.14*cos(b),.19+.14*sin(b)),
                                 (.25+.14*cos(a),.19+.14*sin(a))))
        add((last_center,(RINGS-1)*SECTORS+j,(RINGS-1)*SECTORS+k),
            ((.75,.19),(.75+.14*cos(a),.19+.14*sin(a)),
             (.75+.14*cos(b),.19+.14*sin(b))))
    return vertices, faces, uvs, origin


def sculpted_leg(name, side, index, section, material):
    """Blender adapter; caller keeps the existing Coxa/Tibia bone binding."""
    import bpy
    vertices, faces, uvs, origin = leg_mesh_data(side, index, section)
    mesh = bpy.data.meshes.new(name+'_Mesh')
    mesh.from_pydata(vertices, [], faces)
    mesh.update()
    obj = bpy.data.objects.new(name, mesh)
    bpy.context.collection.objects.link(obj)
    obj.location = origin
    mesh.materials.append(material)
    obj['magenheim_capcrawler_leg_contract'] = CONTRACT
    obj['magenheim_capcrawler_leg_uv_contract'] = UV_CONTRACT
    layer = mesh.uv_layers.new(name='CapcrawlerUV')
    for polygon, face_uv in zip(mesh.polygons, uvs):
        polygon.use_smooth = len(polygon.vertices) == 4
        for li, uv in zip(polygon.loop_indices, face_uv):
            layer.data[li].uv = uv
    return obj
