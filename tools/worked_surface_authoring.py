"""Packed, irregular workshop surfaces with a common 1.5 metre texture tile."""
import hashlib
import math
import random


def bind_worked_surface(bpy, material, wood=False, weathered=False):
    tree = material.node_tree
    bsdf = tree.nodes.get('Principled BSDF')
    old = next((n.image for n in tree.nodes if n.type == 'TEX_IMAGE' and
                'Albedo' in n.name and n.image), None)
    if old is None:
        return
    pixels = list(old.pixels)
    base = tuple(sum(pixels[i::4]) / (len(pixels)//4) for i in range(3))
    seed = int(hashlib.sha256(material.name.encode()).hexdigest()[:8], 16)
    rng = random.Random(seed)
    grids = {n: [[rng.uniform(-1, 1) for _ in range(n)] for _ in range(n)] for n in (4, 9, 19, 43)}

    def noise(u, v, n):
        x, y = u*n, v*n
        ix, iy = math.floor(x), math.floor(y)
        fx, fy = x-ix, y-iy
        fx, fy = fx*fx*(3-2*fx), fy*fy*(3-2*fy)
        g = grids[n]
        a = g[iy % n][ix % n]*(1-fx)+g[iy % n][(ix+1) % n]*fx
        b = g[(iy+1) % n][ix % n]*(1-fx)+g[(iy+1) % n][(ix+1) % n]*fx
        return a*(1-fy)+b*fy

    size = 128
    field = []
    for y in range(size):
        row = []
        for x in range(size):
            u, v = x/size, y/size
            broad = noise(u, v, 4)
            value = .55*broad + .28*noise(u, v, 9)+.12*noise(u, v, 19)+.05*noise(u, v, 43)
            if wood:
                # Wandering grain, interrupted by knots; no diagonal interference grid.
                grain = math.sin(math.tau*(v*11+.38*noise(u, v, 4)))
                value = .48*value+.32*grain+.20*noise(u, v*.15, 19)
            if weathered:
                # Interrupted pores and grain scuffs break the clean, uniform polish.
                value -= max(0, -noise(u, v, 43)-.35)*.5
            row.append(value)
        field.append(row)
    surfaces = [('Albedo', False), ('Normal', True)]
    if weathered:
        surfaces += [('MetallicSmoothness', False), ('Roughness', False)]
    for suffix, normal in surfaces:
        image = bpy.data.images.new(material.name+'.worked.'+suffix, width=512, height=512)
        if normal or suffix in ('MetallicSmoothness', 'Roughness'):
            image.colorspace_settings.name = 'Non-Color'
        values = []
        for y in range(512):
            for x in range(512):
                ix, iy = x//4, y//4
                value = field[iy][ix]
                if normal:
                    strength = .48 if weathered else .22
                    dx = (field[iy][(ix+1)%size]-field[iy][(ix-1)%size])*strength
                    dy = (field[(iy+1)%size][ix]-field[(iy-1)%size][ix])*strength
                    values.extend((.5-dx, .5-dy, 1, 1))
                elif suffix in ('MetallicSmoothness', 'Roughness'):
                    rough = max(.25, min(.95, bsdf.inputs['Roughness'].default_value + value*.20))
                    metal = bsdf.inputs['Metallic'].default_value
                    values.extend((metal, metal, metal, 1-rough) if suffix == 'MetallicSmoothness' else (rough, rough, rough, 1))
                else:
                    shade = 1 + (.55 if weathered else .30)*value
                    values.extend((*[max(0, min(1, c*shade)) for c in base], 1))
        image.pixels.foreach_set(values)
        image.pack()
        node = next(n for n in tree.nodes if n.type == 'TEX_IMAGE' and suffix in n.name)
        node.image = image
