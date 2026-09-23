"""Shared flora authoring for the Underworld biome tools (author-underworld-<biome>.py).

A `Model` collects one model's parts and materials against a biome's surface table and bakes them
into one atlas; the forms below are the stalks, caps, gills, buttress roots and mushroom clusters
every fungal or waterline biome reuses. Tools importing this list it under `inputs` in
assets/generated.manifest.json.
"""
import bmesh
import bpy
import math
import random

from mathutils import Matrix, Vector

from magenheim_blender_kit import BAKE_SIZE, blob, curve, lathe, merge, plate, sweep  # noqa: F401

Z = Vector((0.0, 0.0, 1.0))


class Model:
    def __init__(self, model_id, atlas_size, surfaces):
        self.model_id = model_id
        self.surfaces = surfaces
        self.atlas_size = atlas_size
        self.atlas = bpy.data.images.new(model_id + '-albedo', BAKE_SIZE, BAKE_SIZE, alpha=True)
        self.atlas.generated_color = (0, 0, 0, 0)
        self.materials = {}
        self.parts = []

    def material(self, surface):
        if surface in self.materials:
            return self.materials[surface]
        spec = self.surfaces[surface]
        mat = bpy.data.materials.new(f'magenheim.underworld.{self.model_id}.{surface}')
        mat.use_nodes = True
        mat.use_backface_culling = True
        mat['surface'] = surface
        nodes = mat.node_tree.nodes
        bsdf = nodes.get('Principled BSDF')
        bsdf.inputs['Base Color'].default_value = (1, 1, 1, 1)
        bsdf.inputs['Metallic'].default_value = spec['metallic']
        bsdf.inputs['Roughness'].default_value = spec['rough']
        bsdf.inputs['Emission Color'].default_value = (*spec.get('emission', (0, 0, 0)), 1)
        bsdf.inputs['Emission Strength'].default_value = 1.0
        image = nodes.new('ShaderNodeTexImage')
        image.image = self.atlas
        image.name = 'Atlas'
        mat.node_tree.links.new(image.outputs['Color'], bsdf.inputs['Base Color'])
        nodes.active = image
        self.materials[surface] = mat
        return mat

    def part(self, name, mesh, surface, smooth=True, collider=False, sharp=55.0):
        if bpy.data.objects.get(name):
            raise ValueError(f'{self.model_id}: duplicate part {name}')
        data = bpy.data.meshes.new(name)
        verts, faces = mesh
        data.from_pydata([tuple(v) for v in verts], [], faces)
        data.update()
        bm = bmesh.new()
        bm.from_mesh(data)
        bmesh.ops.remove_doubles(bm, verts=bm.verts, dist=1e-6)
        bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
        bm.to_mesh(data)
        bm.free()
        if smooth:
            data.shade_smooth()
            data.set_sharp_from_angle(angle=math.radians(sharp))
        else:
            data.shade_flat()
        data.materials.append(self.material(surface))
        obj = bpy.data.objects.new(name, data)
        bpy.context.scene.collection.objects.link(obj)
        obj['game_node_path'] = f'magenheim.underworld.{self.model_id}/{name}'
        obj['game_collision'] = collider
        obj['game_crystal'] = 'null'
        self.parts.append(obj)
        return obj


def tube(points, radii, sides=12, samples=12):
    """A tapering tube along a curve. radii: (t, r) stations, interpolated."""
    path = curve(points, samples)

    def radius(t):
        for (t0, r0), (t1, r1) in zip(radii, radii[1:]):
            if t0 <= t <= t1:
                return r0 + (r1 - r0) * (t - t0) / max(1e-6, t1 - t0)
        return radii[-1][1]

    def profile(k, t):
        r = radius(t)
        if r <= 0:
            return [(0.0, 0.0)]
        return [(r * math.cos(math.tau * j / sides), r * math.sin(math.tau * j / sides)) for j in range(sides)]
    # The reference axis must never lie along the tube or its frame collapses: X for a rising
    # stalk, Z for a branch or root that runs mostly sideways.
    direction = (Vector(points[-1]) - Vector(points[0])).normalized()
    return sweep(path, profile, Vector((1.0, 0.0, 0.0)) if abs(direction.z) > 0.7 else Z)


def cap(centre, radius, height, underside=0.18, sides=24, bell=0.0):
    """A mushroom cap: a domed top meeting a shallow concave underside at a rolled rim.
    bell > 0 draws the rim down into a bell."""
    cx, cy, cz = centre
    r, h = radius, height
    profile = [(cz + h, 0.0), (cz + h * 0.97, r * 0.32), (cz + h * 0.86, r * 0.62), (cz + h * 0.62, r * 0.86),
               (cz + h * 0.30 - bell * h, r * 0.98), (cz + h * 0.10 - bell * h * 1.2, r * 1.0),
               (cz - bell * h * 1.1, r * 0.94), (cz + underside * h * 0.3, r * 0.70), (cz + underside * h, r * 0.30),
               (cz + underside * h * 1.2, 0.0)]
    return lathe(profile, sides, centre=(cx, cy))


def gills(centre, inner, outer, depth, count=28, thickness=0.016):
    """Radial gill blades hanging under a cap."""
    blades = []
    for i in range(count):
        a = math.tau * i / count
        outline = [(inner, 0.0), (outer, -depth * 0.2), (outer * 0.98, -depth * 0.45), (inner * 1.1, -depth)]
        m = Matrix.Translation(Vector(centre)) @ Matrix.Rotation(a, 4, 'Z')
        blades.append(plate(outline, thickness, m))
    return merge(*blades)


def buttresses(base, count, reach, height, width, rng):
    roots = []
    for i in range(count):
        a = math.tau * i / count + rng.uniform(-0.3, 0.3)
        d = Vector((math.cos(a), math.sin(a), 0.0))
        start = Vector(base) + Z * height
        roots.append(tube([start, start + d * reach * 0.45 - Z * height * 0.45, start + d * reach - Z * (height + 0.12)],
                          [(0.0, width), (0.6, width * 0.55), (1.0, 0.0)], 8, 8))
    return merge(*roots)


def small_mushrooms(m, count, surface, stalk_surface, size_range, bell, seed, spread, height=(1.6, 2.6)):
    rng = random.Random(seed)
    stalks, caps = [], []
    for i in range(count):
        a = rng.uniform(0, math.tau)
        d = rng.uniform(0.0, spread)
        x, y = d * math.cos(a), d * math.sin(a)
        size = rng.uniform(*size_range)
        h = size * rng.uniform(*height)
        stalks.append(lathe([(0.0, size * 0.28), (h * 0.5, size * 0.22), (h, size * 0.20)], 8, centre=(x, y)))
        caps.append(cap((x, y, h - size * 0.1), size, size * 0.62, bell=bell, sides=14))
    m.part('stalks', merge(*stalks), stalk_surface)
    m.part('caps', merge(*caps), surface)
