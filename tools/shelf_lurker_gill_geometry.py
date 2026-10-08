"""Shelf Lurker's 24 fungal shelf gills: pure centerline and Blender mesh."""
from math import pi, sin, cos

def shelf_gill_path(start, end, steps=8):
    """Anchor ends, bow sideways and droop below each shelf."""
    if steps < 6: raise ValueError('Shelf gill needs at least six intervals')
    dx, dy = end[0] - start[0], end[1] - start[1]
    reach = (dx * dx + dy * dy) ** .5
    if reach <= .05: raise ValueError('Shelf gill span is too short')
    lateral = (-dy / reach, dx / reach)
    centers = []
    for i in range(steps + 1):
        t = i / steps
        wave = sin(pi * t)
        centers.append((start[0] + dx*t + lateral[0]*.018*wave,
                        start[1] + dy*t + lateral[1]*.018*wave,
                        start[2] + (end[2] - start[2])*t - .032*wave))
    return centers

def shelf_gill(name, start, end, material):
    """Closed 9-ring, 12-sided tapered lamella rather than a straight rod."""
    import bpy
    from mathutils import Vector
    sides, steps = 12, 8
    centers = [Vector(p) for p in shelf_gill_path(start, end, steps)]
    radial = Vector((end[0]-start[0], end[1]-start[1], 0)).normalized()
    lateral = Vector((-radial.y, radial.x, 0))
    verts = []
    for i, center in enumerate(centers):
        t = i/steps
        fullness = sin(pi*t)**.8
        width, thick = .008 + .026*fullness, .003 + .007*fullness
        for j in range(sides):
            a = 2*pi*j/sides
            verts.append(tuple(center + lateral*(width*cos(a)) + Vector((0,0,thick*sin(a)))))
    faces = []
    for i in range(steps):
        for j in range(sides):
            k = (j+1)%sides
            faces.append((i*sides+j, i*sides+k, (i+1)*sides+k, (i+1)*sides+j))
    faces.append(tuple(reversed(range(sides))))
    faces.append(tuple(steps*sides+j for j in range(sides)))
    mesh = bpy.data.meshes.new(name+'_Mesh')
    mesh.from_pydata(verts, [], faces)
    mesh.update()
    obj = bpy.data.objects.new(name, mesh)
    bpy.context.collection.objects.link(obj)
    mesh.materials.append(material)
    obj['magenheim_gill_contract'] = 'swept-lamella-9x12'
    for poly in mesh.polygons: poly.use_smooth = len(poly.vertices)==4
    layer = mesh.uv_layers.new(name='ShelfLurkerUV')
    for poly in mesh.polygons:
        ring_ids = [mesh.loops[li].vertex_index%sides for li in poly.loop_indices]
        seam = 0 in ring_ids and sides-1 in ring_ids
        for li in poly.loop_indices:
            idx = mesh.loops[li].vertex_index
            angular = idx%sides
            if len(poly.vertices)>4:
                angle = 2*pi*angular/sides
                layer.data[li].uv = (.5+.45*cos(angle), .5+.45*sin(angle))
            else:
                layer.data[li].uv = (1.0 if seam and angular==0 else angular/sides, (idx//sides)/steps)
    return obj
