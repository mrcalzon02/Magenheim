"""Sculpted suction-grip pads for the existing Shelf Lurker's six wall-clinging feet.

Closed elliptical pads retain the donor rig's six grip pivots. Two concentric
underside contact ridges and a recessed center provide real geometry; the
planar UVs align the existing grip-pad PBR concentric rings with those ridges.
"""
from math import cos, sin, pi, isfinite

SIDES = 48
PROFILE = (
    (.14, .72), (.30, .82), (.48, .78), (.66, .64),
    (.82, .42), (.94, .12), (1.00, -.12),
    (.96, -.40), (.86, -.72), (.74, -.95),
    (.64, -.76), (.52, -1.00), (.40, -.75),
    (.30, -.90), (.20, -.58), (.10, -.35),
)
CONTRACT = 'sculpted-suction-pad-16x48'


def grip_uv_coord(point, radius_x, radius_y):
    """Local XY radial projection with an atlas gutter for the pad PBR maps."""
    x, y, _ = point
    if not all(isfinite(v) for v in (x, y, radius_x, radius_y)) or radius_x <= 0 or radius_y <= 0:
        raise ValueError('Invalid grip UV scale or coordinates')
    return (.5 + x / (2.12 * radius_x), .5 + y / (2.12 * radius_y))


def grip_mesh_data(radius_x=.18, radius_y=.24, radius_z=.055):
    """Return closed, outward-wound pad mesh in local grip-bone coordinates."""
    if not all(isfinite(v) and v > 0 for v in (radius_x, radius_y, radius_z)):
        raise ValueError('Grip dimensions must be finite and positive')
    if not (.14 <= radius_x <= .23 and .19 <= radius_y <= .29 and .04 <= radius_z <= .075):
        raise ValueError('Grip dimensions exceed established Shelf Lurker silhouette')
    verts = [(0., 0., .70 * radius_z), (0., 0., -.25 * radius_z)]
    for ring, (radius, height) in enumerate(PROFILE):
        for j in range(SIDES):
            angle = 2 * pi * j / SIDES
            edge = 1 + (.018 * cos(10 * angle + .3) if ring in (5, 6, 7, 8) else 0)
            verts.append((radius_x * radius * edge * cos(angle),
                          radius_y * radius * edge * sin(angle),
                          radius_z * height))
    faces = [(0, 2 + j, 2 + (j + 1) % SIDES) for j in range(SIDES)]
    for ring in range(len(PROFILE) - 1):
        inner = 2 + ring * SIDES
        outer = inner + SIDES
        for j in range(SIDES):
            nxt = (j + 1) % SIDES
            faces.append((inner + j, outer + j, outer + nxt, inner + nxt))
    last = 2 + (len(PROFILE) - 1) * SIDES
    faces.extend((1, last + (j + 1) % SIDES, last + j) for j in range(SIDES))
    return verts, faces


def grip_pad(name, location, dimensions, material):
    """Blender adapter; preserves the existing Grip bone, material, and pivot."""
    import bpy
    verts, faces = grip_mesh_data(*dimensions)
    mesh = bpy.data.meshes.new(name + '_Mesh')
    mesh.from_pydata(verts, [], faces)
    mesh.update()
    obj = bpy.data.objects.new(name, mesh)
    bpy.context.collection.objects.link(obj)
    obj.location = location
    mesh.materials.append(material)
    obj['magenheim_grip_contract'] = CONTRACT
    obj['magenheim_grip_uv_radii'] = dimensions[:2]
    for polygon in mesh.polygons:
        polygon.use_smooth = len(polygon.vertices) == 4
    uv = mesh.uv_layers.new(name='ShelfLurkerUV')
    for polygon in mesh.polygons:
        for li in polygon.loop_indices:
            vertex = mesh.vertices[mesh.loops[li].vertex_index]
            uv.data[li].uv = grip_uv_coord(vertex.co, dimensions[0], dimensions[1])
    return obj
