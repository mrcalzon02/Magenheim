"""Sculpted paired compound-eye lenses for the established Sulfurous Wastes Ashmite.

The original left/right eye origins, envelope, Eye PBR material and Head rig are
unchanged. The surface is closed, explicitly UV-unwrapped, and Blender-independent
so geometry can be regression-tested without pretending to regenerate a .blend.
"""
from math import cos, exp, isfinite, pi, sin

RINGS = 18
SIDES = 48
CONTRACT = 'ashmite-compound-eye-socket-lens-18x48-v1'
EYE_RADIUS = 0.013
EYE_DEPTH = 0.009


def anchor(side):
    if side not in (-1, 1):
        raise ValueError('Eye side must be -1 (L) or +1 (R)')
    return (side * 0.047, 0.151, 0.085)


def eye_mesh_data(side):
    """Return local-space vertices, outward faces and seam-safe per-loop UVs.

    +Y is forward. A narrow peripheral socket rim, physical annular lens
    groove and restrained twelve-sector corneal facets replace an ico-sphere.
    No extra eyes, horns, or emitted light are introduced.
    """
    anchor(side)  # Reject unknown anatomy rather than generating a new variant.
    vertices = [(0., -EYE_DEPTH, 0.)]
    for i in range(RINGS):
        t = (i + .5) / RINGS
        theta_y = pi * t
        y = EYE_DEPTH * (2*t - 1)
        shell = sin(theta_y)**.59 * (1.06 - .10*t)
        rim = .12 * exp(-((t-.23)/.075)**2)
        groove = -.115 * exp(-((t-.34)/.048)**2)
        for j in range(SIDES):
            a = 2*pi*j/SIDES
            facet = .022 * cos(12*a) * sin(theta_y)**2
            radial = EYE_RADIUS * shell * (1 + rim + groove + facet)
            # Eye remains bilaterally symmetric: cos(12a) mirrors across X.
            vertices.append((radial*cos(a), y, .92*radial*sin(a)))
    vertices.append((0., EYE_DEPTH, 0.))
    rear, front = 0, len(vertices)-1
    faces, uvs = [], []
    for j in range(SIDES):
        k=(j+1)%SIDES
        faces.append((rear,1+j,1+k))
        a,b=2*pi*j/SIDES,2*pi*k/SIDES
        uvs.append(((.5,.5),(.5+.47*cos(a),.5+.47*sin(a)),
                    (.5+.47*cos(b),.5+.47*sin(b))))
    for i in range(RINGS-1):
        for j in range(SIDES):
            k=(j+1)%SIDES
            faces.append((1+i*SIDES+j,1+(i+1)*SIDES+j,
                          1+(i+1)*SIDES+k,1+i*SIDES+k))
            uvs.append(((j/SIDES,(i+.5)/RINGS),
                        (j/SIDES,(i+1.5)/RINGS),
                        ((j+1)/SIDES,(i+1.5)/RINGS),
                        ((j+1)/SIDES,(i+.5)/RINGS)))
    last=1+(RINGS-1)*SIDES
    for j in range(SIDES):
        k=(j+1)%SIDES
        faces.append((front,last+k,last+j))
        a,b=2*pi*j/SIDES,2*pi*k/SIDES
        uvs.append(((.5,.5),(.5+.47*cos(b),.5+.47*sin(b)),
                    (.5+.47*cos(a),.5+.47*sin(a))))
    # Angular seam is explicit per loop: j=47 -> k=0 uses U=1, not U=0.
    return vertices, faces, uvs


def sculpted_eye(name, side, material):
    """Blender adapter; caller retains original Head bone and eye material."""
    import bpy
    verts, faces, uvs = eye_mesh_data(side)
    mesh = bpy.data.meshes.new(name+'_Mesh')
    mesh.from_pydata(verts, [], faces)
    mesh.update()
    obj = bpy.data.objects.new(name, mesh)
    bpy.context.collection.objects.link(obj)
    obj.location = anchor(side)
    mesh.materials.append(material)
    obj['magenheim_ashmite_eye_contract'] = CONTRACT
    obj['magenheim_ashmite_eye_side'] = side
    layer = mesh.uv_layers.new(name='AshmiteUV')
    for poly, loop_uv in zip(mesh.polygons, uvs):
        poly.use_smooth = len(poly.vertices)==4
        for li, pair in zip(poly.loop_indices, loop_uv):
            layer.data[li].uv = pair
    return obj
