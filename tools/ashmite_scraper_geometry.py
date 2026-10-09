"""Closed sculpted vent scraper for the existing Sulfurous Wastes Ashmite.

The small central mouth tool replaces only the prior 14-sided cone. It retains
its world-space origin, 50-mm front/back reach, sulfur material and Jaw binding.
"""
from math import cos, exp, pi, sin

RINGS = 17
SIDES = 32
CONTRACT = 'ashmite-sulfurized-vent-scraper-17x32'
ORIGIN = (0.0, 0.185, 0.048)


def scraper_mesh_data():
    """Return local vertices, outward faces and continuous per-loop UVs."""
    vertices = []
    for i in range(RINGS):
        t = i / (RINGS - 1)
        y = 0.025 - 0.050 * t
        rx = 0.017 * (1 - 0.86 * t)
        rz = 0.011 * (1 - 0.80 * t)
        for j in range(SIDES):
            angle = 2 * pi * j / SIDES
            co, si = cos(angle), sin(angle)
            rasp = sum(exp(-((t - k) / 0.041) ** 2) for k in (0.25, 0.50, 0.75))
            underside = max(0.0, -si) ** 8
            dorsal = max(0.0, si) ** 12
            z = 0.0025 * sin(pi * t) + rz * si * (1 + 0.19 * rasp * underside + 0.17 * dorsal)
            x = rx * co * (1 + 0.035 * cos(6 * angle) * sin(pi * t))
            vertices.append((x, y, z))
    vertices += [(0.0, 0.025, 0.0), (0.0, -0.025, 0.0)]
    base, tip = RINGS * SIDES, RINGS * SIDES + 1
    faces, uvs = [], []
    for j in range(SIDES):
        k = (j + 1) % SIDES
        angle_a, angle_b = 2 * pi * j / SIDES, 2 * pi * k / SIDES
        faces.append((base, k, j))
        uvs.append(((0.5, 0.5), (0.5 + 0.48 * cos(angle_b), 0.5 + 0.48 * sin(angle_b)),
                    (0.5 + 0.48 * cos(angle_a), 0.5 + 0.48 * sin(angle_a))))
    for i in range(RINGS - 1):
        for j in range(SIDES):
            k = (j + 1) % SIDES
            faces.append((i * SIDES + j, i * SIDES + k,
                          (i + 1) * SIDES + k, (i + 1) * SIDES + j))
            uvs.append(((j / SIDES, i / (RINGS - 1)), ((j + 1) / SIDES, i / (RINGS - 1)),
                        ((j + 1) / SIDES, (i + 1) / (RINGS - 1)),
                        (j / SIDES, (i + 1) / (RINGS - 1))))
    for j in range(SIDES):
        k = (j + 1) % SIDES
        angle_a, angle_b = 2 * pi * j / SIDES, 2 * pi * k / SIDES
        faces.append((tip, (RINGS - 1) * SIDES + j, (RINGS - 1) * SIDES + k))
        uvs.append(((0.5, 0.5), (0.5 + 0.48 * cos(angle_a), 0.5 + 0.48 * sin(angle_a)),
                    (0.5 + 0.48 * cos(angle_b), 0.5 + 0.48 * sin(angle_b))))
    return vertices, faces, uvs


def sculpted_scraper(name, material):
    """Blender adapter; the existing Jaw bone remains the rigid parent."""
    import bpy
    vertices, faces, uvs = scraper_mesh_data()
    mesh = bpy.data.meshes.new(name + '_Mesh')
    mesh.from_pydata(vertices, [], faces)
    mesh.update()
    obj = bpy.data.objects.new(name, mesh)
    bpy.context.collection.objects.link(obj)
    obj.location = ORIGIN
    mesh.materials.append(material)
    obj['magenheim_ashmite_scraper_contract'] = CONTRACT
    layer = mesh.uv_layers.new(name='AshmiteUV')
    for poly, face_uv in zip(mesh.polygons, uvs):
        poly.use_smooth = len(poly.vertices) == 4
        for loop, uv in zip(poly.loop_indices, face_uv):
            layer.data[loop].uv = uv
    return obj
