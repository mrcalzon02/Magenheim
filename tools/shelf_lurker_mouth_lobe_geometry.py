"""Sculpted four-petal feeding collar for the established Shelf Lurker.

Each formerly spherical mouth lobe becomes a closed, downturned, fluted
lamella. Its root, reach, Head binding and MouthGill PBR identity are retained;
only the four existing lobes are replaced, not the eight hooked feeding barbs.
"""
from math import cos, hypot, isfinite, pi, sin

RINGS = 25
SIDES = 32
CONTRACT = 'downturned-feeding-lamella-25x32'
MOUTH_CENTER = (0., .38, 1.29)


def lobe_mesh_data(target, center=MOUTH_CENTER):
    """Return a watertight, outward-wound feeding petal in creature coordinates."""
    if len(target) != 3 or len(center) != 3:
        raise ValueError('Mouth lobe points must be 3D')
    values = tuple(float(v) for v in (*target, *center))
    if not all(isfinite(v) for v in values):
        raise ValueError('Nonfinite mouth lobe coordinates')
    dx, dy = target[0]-center[0], target[1]-center[1]
    reach = hypot(dx, dy)
    if not .19 <= reach <= .25 or abs(target[2]-center[2]) > .015:
        raise ValueError('Mouth lobe anchor outside established fourfold collar')
    ux, uy = dx/reach, dy/reach
    # The local cross-section is tangential to the feeding aperture; its
    # right-handed orientation keeps cap and side winding consistent.
    tx, ty = -uy, ux
    start = (center[0]+ux*.10, center[1]+uy*.10, center[2]+.01)
    end = (center[0]+ux*.36, center[1]+uy*.36, center[2]-.05)
    vertices = []
    for ring in range(RINGS):
        t = ring/(RINGS-1)
        bow = sin(pi*t)
        x = start[0]+(end[0]-start[0])*t
        y = start[1]+(end[1]-start[1])*t
        z = start[2]+(end[2]-start[2])*t-.035*bow
        width = (.055+.080*bow**1.3)*(1-.12*t)
        thickness = .027*(.85+.15*bow)
        for j in range(SIDES):
            angle = 2*pi*j/SIDES
            # Six continuous longitudinal gill flutes, strongest at midspan;
            # the tips retain a full cross-section for a closed, readable rim.
            flute = 1+.075*cos(6*angle)*bow**2
            transverse = width*cos(angle)*flute
            vertical = thickness*sin(angle)*(1+.20*cos(4*angle)*bow**2)
            vertices.append((x+tx*transverse, y+ty*transverse, z+vertical))
    faces = []
    for ring in range(RINGS-1):
        for j in range(SIDES):
            nxt = (j+1)%SIDES
            faces.append((ring*SIDES+j, ring*SIDES+nxt,
                          (ring+1)*SIDES+nxt, (ring+1)*SIDES+j))
    faces.append(tuple(reversed(range(SIDES))))
    faces.append(tuple((RINGS-1)*SIDES+j for j in range(SIDES)))
    return vertices, faces


def lobe_uv(index, cap, seam):
    """Continuous longitudinal side UVs and noncollapsed planar end caps."""
    ring, angular = divmod(index, SIDES)
    if cap:
        angle = 2*pi*angular/SIDES
        return (.5+.45*cos(angle), .5+.45*sin(angle))
    return (1. if seam and angular == 0 else angular/SIDES, ring/(RINGS-1))


def mouth_lobe(name, target, material):
    """Blender adapter: retain existing Head bone and mouth-gill material."""
    import bpy
    vertices, faces = lobe_mesh_data(target)
    mesh = bpy.data.meshes.new(name+'_Mesh')
    mesh.from_pydata(vertices, [], faces)
    mesh.update()
    obj = bpy.data.objects.new(name, mesh)
    bpy.context.collection.objects.link(obj)
    mesh.materials.append(material)
    obj['magenheim_mouth_lobe_contract'] = CONTRACT
    for poly in mesh.polygons:
        poly.use_smooth = len(poly.vertices) == 4
    uv = mesh.uv_layers.new(name='ShelfLurkerUV')
    for poly in mesh.polygons:
        ids = [mesh.loops[li].vertex_index%SIDES for li in poly.loop_indices]
        seam = 0 in ids and SIDES-1 in ids
        for li in poly.loop_indices:
            index = mesh.loops[li].vertex_index
            uv.data[li].uv = lobe_uv(index, len(poly.vertices)>4, seam)
    return obj
