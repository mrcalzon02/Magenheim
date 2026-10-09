"""Closed sulfur-mineral fracture ridges for the existing five Ashmite scutes.

Ten dorsal accretions retain the original segment anchors, SulfurCrust material
and Abdomen/Thorax assignments. Geometry and UVs are Blender-independent.
"""
from math import cos, exp, pi, sin, sqrt

CONTRACT = 'ashmite-scutal-sulfur-ridge-13x20-v1'
RINGS = 13
SIDES = 20


def anchors(side, index):
    if side not in (-1, 1) or index not in range(1, 6):
        raise ValueError('Unknown Ashmite scute ridge')
    y = (-.155, -.095, -.035, .030, .085)[index-1]
    z = .135-abs(y)*.10
    return (side*.020, y, z+.018), (side*.060, y-.010, z+.025)


def _unit(v):
    length = sqrt(sum(c*c for c in v))
    if length <= 1e-9:
        raise ValueError('Degenerate sulfur ridge anchor')
    return tuple(c/length for c in v)


def _cross(a, b):
    return (a[1]*b[2]-a[2]*b[1], a[2]*b[0]-a[0]*b[2], a[0]*b[1]-a[1]*b[0])


def ridge_mesh_data(side, index):
    """Return local vertices, outward closed faces, and authored per-face UVs."""
    start, end = anchors(side, index)
    delta = tuple(end[k]-start[k] for k in range(3))
    tangent = _unit(delta)
    reference = (0., 0., 1.)
    dot = sum(reference[k]*tangent[k] for k in range(3))
    dorsal = _unit(tuple(reference[k]-dot*tangent[k] for k in range(3)))
    lateral = _cross(dorsal, tangent)
    vertices = []
    for i in range(RINGS):
        t = i/(RINGS-1)
        # Accretion narrows toward the outer tip. Two genuine recessed
        # transverse mineral seams interrupt its otherwise raised dorsal crest.
        radius = .0056*(.84+.25*sin(pi*t))*(1-.30*t)
        seams = sum(exp(-((t-pos)/.039)**2) for pos in (.32, .69))
        center = tuple(delta[k]*t for k in range(3))
        for j in range(SIDES):
            a = 2*pi*j/SIDES
            co, si = cos(a), sin(a)
            crest = max(0., si)**8
            mineral = max(0., sin(5*a+2*pi*t+.4*index))**12
            # Physical crest and irregular accretion, not emissive cracks.
            w = radius*(1+.075*mineral-.17*seams)
            h = radius*(.68+.33*crest+.08*mineral-.16*seams)
            vertices.append(tuple(center[k]+w*co*lateral[k]+h*si*dorsal[k]
                                  for k in range(3)))
    vertices.extend(((0., 0., 0.), delta))
    root, tip = RINGS*SIDES, RINGS*SIDES+1
    faces, uvs = [], []
    for j in range(SIDES):
        k = (j+1)%SIDES
        a, b = 2*pi*j/SIDES, 2*pi*k/SIDES
        faces.append((root, k, j))
        uvs.append(((.5,.5),(.5+.48*cos(b),.5+.48*sin(b)),
                    (.5+.48*cos(a),.5+.48*sin(a))))
    for i in range(RINGS-1):
        for j in range(SIDES):
            k = (j+1)%SIDES
            faces.append((i*SIDES+j,i*SIDES+k,(i+1)*SIDES+k,(i+1)*SIDES+j))
            uvs.append(((j/SIDES,i/(RINGS-1)),((j+1)/SIDES,i/(RINGS-1)),
                        ((j+1)/SIDES,(i+1)/(RINGS-1)),(j/SIDES,(i+1)/(RINGS-1))))
    for j in range(SIDES):
        k = (j+1)%SIDES
        a, b = 2*pi*j/SIDES, 2*pi*k/SIDES
        faces.append((tip,(RINGS-1)*SIDES+j,(RINGS-1)*SIDES+k))
        uvs.append(((.5,.5),(.5+.48*cos(a),.5+.48*sin(a)),
                    (.5+.48*cos(b),.5+.48*sin(b))))
    return vertices, faces, uvs


def sculpted_ridge(name, side, index, material):
    """Blender adapter; the author retains the original bone assignment."""
    import bpy
    start, _ = anchors(side, index)
    vertices, faces, uvs = ridge_mesh_data(side, index)
    mesh = bpy.data.meshes.new(name+'_Mesh')
    mesh.from_pydata(vertices, [], faces)
    mesh.update()
    obj = bpy.data.objects.new(name, mesh)
    bpy.context.collection.objects.link(obj)
    obj.location = start
    mesh.materials.append(material)
    obj['magenheim_ashmite_sulfur_ridge_contract'] = CONTRACT
    obj['magenheim_ashmite_sulfur_ridge_side'] = side
    obj['magenheim_ashmite_sulfur_ridge_index'] = index
    layer = mesh.uv_layers.new(name='AshmiteUV')
    for poly, face_uv in zip(mesh.polygons, uvs):
        poly.use_smooth = len(poly.vertices) == 4
        for li, uv in zip(poly.loop_indices, face_uv):
            layer.data[li].uv = uv
    return obj
