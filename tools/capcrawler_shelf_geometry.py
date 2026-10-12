"""Closed, rig-preserving bracket-fungus shelf plates for the existing Capcrawler.

The four existing Capcrawler_ShelfPlate_{L,R}{1,2} identities retain their
Body binding, CapcrawlerCarapace material and original anchor positions. This
module only replaces the generic flattened ico-spheres with physical fluted
fans, downturned margins and separate dorsal/ventral/rim UV islands.
"""
from math import cos, pi, sin

CONTRACT = 'capcrawler-fluted-bracket-shelves-v1'
UV_CONTRACT = 'capcrawler-shelf-three-island-uv-v1'
SECTORS = 64
RINGS = 6
POSITIONS = ((-.16, .130, .075), (.04, .145, .085))


def shelf_mesh_data(side, index):
    """Return (local vertices, outward triangle faces, face-loop UVs).

    The origin is the unchanged shelf anchor; mirrored X coordinates keep
    right and left anatomical silhouettes and original bone pivots.
    """
    if side not in (-1, 1) or index not in (1, 2):
        raise ValueError('Expected side -1/+1 and existing shelf index 1/2')
    _, sx, sy = POSITIONS[index - 1]
    vertices = []

    def position(r, theta, dorsal):
        c, s = cos(theta), sin(theta)
        # The root is narrow; the outer lip spreads into a broad shelf.
        fan = .36 + .64 * ((1 + c) / 2) ** .75
        scallop = 1 - .14 * (sin(6 * theta) ** 2) * r**5
        x = .045 + sx * r * c * scallop
        y = sy * r * s * fan * scallop + .010 * r*r * c
        if dorsal:
            # Convex fungal cuticle, radiating physical ribs, and a
            # deliberately lowered rim rather than a paper-thin disc.
            ribs = .0045 * (max(0., cos(12 * theta)) ** 2) * r*r
            z = .028 * (1 - r*r) - .008 * r**6 + ribs
        else:
            z = -.016 - .003 * (1 - r*r)
        return (side * x, y, z)

    # Center fans avoid degenerate coincident vertices at r=0.
    vertices.append(position(0, 0, True))
    for ring in range(1, RINGS + 1):
        r = ring / RINGS
        for sector in range(SECTORS):
            vertices.append(position(r, 2*pi*sector/SECTORS, True))
    bottom_start = len(vertices)
    vertices.append(position(0, 0, False))
    for ring in range(1, RINGS + 1):
        r = ring / RINGS
        for sector in range(SECTORS):
            vertices.append(position(r, 2*pi*sector/SECTORS, False))

    faces, uvs = [], []
    def atlas(v, upper):
        x, y, _ = vertices[v]
        return ((.25 if upper else .75) + .20 * ((side*x-.045) / sx),
                .25 + .20 * (y / sy))
    def add(face, upper):
        faces.append(face)
        uvs.append(tuple(atlas(v, upper) for v in face))

    # Side mirroring changes geometric orientation: reverse winding on left.
    def orient(face):
        return tuple(reversed(face)) if side < 0 else face

    for j in range(SECTORS):
        k = (j + 1) % SECTORS
        add(orient((0, 1+j, 1+k)), True)
    for ring in range(RINGS - 1):
        inner = 1 + ring*SECTORS
        outer = inner + SECTORS
        for j in range(SECTORS):
            k = (j+1) % SECTORS
            add(orient((inner+j, outer+j, outer+k, inner+k)), True)
    for j in range(SECTORS):
        k = (j+1) % SECTORS
        add(orient((bottom_start, bottom_start+1+k, bottom_start+1+j)), False)
    for ring in range(RINGS - 1):
        inner = bottom_start+1+ring*SECTORS
        outer = inner+SECTORS
        for j in range(SECTORS):
            k = (j+1) % SECTORS
            add(orient((inner+j, inner+k, outer+k, outer+j)), False)
    top = 1+(RINGS-1)*SECTORS
    bottom = bottom_start+1+(RINGS-1)*SECTORS
    for j in range(SECTORS):
        k=(j+1)%SECTORS
        face=orient((top+j,bottom+j,bottom+k,top+k))
        faces.append(face)
        rim_uv={top+j:(.05+.90*j/SECTORS,.87),
                bottom+j:(.05+.90*j/SECTORS,.63),
                bottom+k:(.05+.90*(j+1)/SECTORS,.63),
                top+k:(.05+.90*(j+1)/SECTORS,.87)}
        uvs.append(tuple(rim_uv[v] for v in face))
    return vertices,faces,uvs


def sculpted_shelf(name, side, index, material):
    """Blender adapter; caller binds the returned object to Body."""
    import bpy
    vertices, faces, uvs = shelf_mesh_data(side, index)
    mesh=bpy.data.meshes.new(name+'_Mesh')
    mesh.from_pydata(vertices,[],faces)
    mesh.update()
    obj=bpy.data.objects.new(name,mesh)
    bpy.context.collection.objects.link(obj)
    y, _, _ = POSITIONS[index-1]
    obj.location=(side*.245,y,.382)
    mesh.materials.append(material)
    obj['magenheim_capcrawler_shelf_contract']=CONTRACT
    obj['magenheim_capcrawler_shelf_uv_contract']=UV_CONTRACT
    layer=mesh.uv_layers.new(name='CapcrawlerUV')
    for poly,face_uv in zip(mesh.polygons,uvs):
        poly.use_smooth=True
        for li,uv in zip(poly.loop_indices,face_uv):
            layer.data[li].uv=uv
    return obj
