"""Sculpted tarsi and backward-facing vent rasp claws of the approved Ashmite.

Preserves all six existing Tarsus bone anchors, toe positions, mouthpart/joint
materials and the old 30-mm scraper envelope. Pure geometry is Blender-free.
"""
from math import cos, exp, pi, sin, sqrt

CONTRACT = 'ashmite-tarsal-rasp-six-pairs-v1'
SIDES = 20
RINGS = {'tarsus': 11, 'scraper': 17}


def _unit(v):
    length = sqrt(sum(c*c for c in v))
    if length < 1e-9:
        raise ValueError('Degenerate Ashmite tarsal anchor')
    return tuple(c/length for c in v)


def _cross(a, b):
    return (a[1]*b[2]-a[2]*b[1], a[2]*b[0]-a[0]*b[2], a[0]*b[1]-a[1]*b[0])


def anchors(side, pair, kind):
    if side not in (-1, 1) or pair not in (1, 2, 3) or kind not in RINGS:
        raise ValueError('Unknown Ashmite tarsal anatomy')
    y = (.065, 0., -.070)[pair-1]
    ankle = (side*.165, y-.012, .018)
    toe = (side*.188, y+.020, .008)
    if kind == 'tarsus':
        return ankle, toe
    # Original 14-sided cone rotated +90 degrees about X: broad end faces
    # forward (+Y), narrow end returns backward (-Y) along the vent surface.
    loc = (side*.198, toe[1]+.010, .007)
    return (loc[0],loc[1]+.015,loc[2]), (loc[0],loc[1]-.015,loc[2])


def tarsal_mesh_data(side, pair, kind):
    """Closed local vertices, outward faces, and per-loop PBR UVs."""
    start, end = anchors(side, pair, kind)
    delta = tuple(end[k]-start[k] for k in range(3))
    tangent = _unit(delta)
    reference = (0., 1., 0.) if abs(tangent[2]) > .95 else (0., 0., 1.)
    dot = sum(reference[k]*tangent[k] for k in range(3))
    dorsal = _unit(tuple(reference[k]-dot*tangent[k] for k in range(3)))
    lateral = _cross(dorsal, tangent)
    n = RINGS[kind]
    vertices = []
    for i in range(n):
        t = i/(n-1)
        if kind == 'tarsus':
            radius = .0075*(1-.48*t)
            height = radius*.72
            # Protective ridged tarsal shell, taper and two discrete sutures.
            suture = sum(exp(-((t-pos)/.037)**2) for pos in (.34,.68))
            cx, cy, cz = (delta[k]*t for k in range(3))
        else:
            radius = .0085*(1-.87*t)
            height = radius*.77
            suture = sum(exp(-((t-pos)/.041)**2) for pos in (.29,.51,.73))
            # Back-curved, inward-biased scraping hook, inside original envelope.
            cx = delta[0]*t - side*.0114*exp(-((t-.80)/.22)**2)*sin(pi*t)
            cy = delta[1]*t
            cz = delta[2]*t - .0013*sin(pi*t)
        center = (cx, cy, cz)
        for j in range(SIDES):
            a = 2*pi*j/SIDES
            co, si = cos(a), sin(a)
            crest = max(0.,si)**10
            ventral = max(0.,-si)**10
            if kind == 'tarsus':
                w = radius*(1+.075*cos(4*a))*(1-.12*suture)
                h = height*(1+.22*crest-.08*ventral)*(1-.08*suture)
            else:
                w = radius*(1+.07*cos(4*a))
                h = height*(1+.17*crest+.22*ventral*suture)
            vertices.append(tuple(center[k]+w*co*lateral[k]+h*si*dorsal[k] for k in range(3)))
    vertices.extend(((0.,0.,0.),delta))
    root, tip = n*SIDES,n*SIDES+1
    faces,uvs = [],[]
    for j in range(SIDES):
        k=(j+1)%SIDES
        a,b=2*pi*j/SIDES,2*pi*k/SIDES
        faces.append((root,k,j))
        uvs.append(((.5,.5),(.5+.48*cos(b),.5+.48*sin(b)),(.5+.48*cos(a),.5+.48*sin(a))))
    for i in range(n-1):
        for j in range(SIDES):
            k=(j+1)%SIDES
            faces.append((i*SIDES+j,i*SIDES+k,(i+1)*SIDES+k,(i+1)*SIDES+j))
            uvs.append(((j/SIDES,i/(n-1)),((j+1)/SIDES,i/(n-1)),
                        ((j+1)/SIDES,(i+1)/(n-1)),(j/SIDES,(i+1)/(n-1))))
    for j in range(SIDES):
        k=(j+1)%SIDES
        a,b=2*pi*j/SIDES,2*pi*k/SIDES
        faces.append((tip,(n-1)*SIDES+j,(n-1)*SIDES+k))
        uvs.append(((.5,.5),(.5+.48*cos(a),.5+.48*sin(a)),(.5+.48*cos(b),.5+.48*sin(b))))
    return vertices,faces,uvs


def sculpted_tarsal(name, side, pair, kind, material):
    """Blender adapter; caller retains the existing Tarsus bone assignment."""
    import bpy
    start,_ = anchors(side,pair,kind)
    vertices,faces,uvs = tarsal_mesh_data(side,pair,kind)
    mesh = bpy.data.meshes.new(name+'_Mesh')
    mesh.from_pydata(vertices,[],faces)
    mesh.update()
    obj = bpy.data.objects.new(name,mesh)
    bpy.context.collection.objects.link(obj)
    obj.location = start
    mesh.materials.append(material)
    obj['magenheim_ashmite_tarsal_contract'] = CONTRACT
    obj['magenheim_ashmite_tarsal_kind'] = kind
    obj['magenheim_ashmite_tarsal_pair'] = pair
    obj['magenheim_ashmite_tarsal_side'] = side
    layer = mesh.uv_layers.new(name='AshmiteUV')
    for poly, face_uv in zip(mesh.polygons,uvs):
        poly.use_smooth = len(poly.vertices)==4
        for li,uv in zip(poly.loop_indices,face_uv):
            layer.data[li].uv=uv
    return obj
