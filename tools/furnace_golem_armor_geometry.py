"""Editable, UV-authored stone/iron armor volumes for the established Furnace Golem.

Coordinates are local Blender XYZ (+Y faces forward); this module deliberately has
no bpy dependency so topology and the furnace-throat cavity can be regression tested.
"""

PROFILES = {
    # Narrow waist, sloping overhanging shoulders, recessed neck saddle.
    'torso': ((-.34,-.50),(.34,-.50),(.45,-.35),(.52,.28),(.39,.50),(.15,.46),
              (-.15,.46),(-.39,.50),(-.52,.28),(-.45,-.35)),
    'pelvis': ((-.38,-.50),(.38,-.50),(.50,-.31),(.48,.32),(.30,.50),(-.30,.50),
               (-.48,.32),(-.50,-.31)),
    'helm': ((-.37,-.50),(.37,-.50),(.50,-.31),(.50,.30),(.32,.50),(-.32,.50),
             (-.50,.30),(-.50,-.31)),
    'plate': ((-.34,-.50),(.34,-.50),(.50,-.32),(.50,.27),(.26,.50),(-.26,.50),
              (-.50,.27),(-.50,-.32)),
    'pauldron': ((-.46,-.50),(.46,-.50),(.60,-.31),(.63,.11),(.37,.50),(.14,.41),
                 (-.14,.41),(-.37,.50),(-.63,.11),(-.60,-.31)),
}


def make_armor(profile, width, height, depth, furnace_throat=False):
    """Return (vertices, faces, per-face UV loops, surface regions).

    Width/height/depth are full physical extents. The throat is a real blind
    aperture in the front face, not a painted or overlaid dark rectangle.
    """
    if profile not in PROFILES or min(width,height,depth) <= 0:
        raise ValueError('Invalid Furnace Golem armor profile or dimensions')
    outline = PROFILES[profile]
    if furnace_throat and profile != 'torso':
        raise ValueError('Only the torso can carry the furnace throat')
    n = len(outline)
    vertices, faces, uv_faces, regions = [], [], [], []

    def ring(points, y):
        start = len(vertices)
        vertices.extend((x, y, z) for x,z in points)
        return list(range(start, start+len(points)))

    def face(indices, uvs, region):
        faces.append(tuple(indices)); uv_faces.append(tuple(uvs)); regions.append(region)

    # The 4-ring bevel changes the profile in all three dimensions without
    # subdivision for its own sake. Front/back face interiors stay broad.
    ys = (-depth*.50, -depth*.35, depth*.34, depth*.50)
    scales = (.83, 1., 1., .83)
    rings = [ring([(x*width*s, z*height*s) for x,z in outline], y)
             for y,s in zip(ys,scales)]
    for j in range(3):
        for i in range(n):
            k = (i+1)%n
            face((rings[j][i],rings[j+1][i],rings[j+1][k],rings[j][k]),
                 ((.02+.44*i/n,.03+.40*j/3),(.02+.44*i/n,.03+.40*(j+1)/3),
                  (.02+.44*(i+1)/n,.03+.40*(j+1)/3),(.02+.44*(i+1)/n,.03+.40*j/3)), 'edge')

    # A fan on the rear; front is either a fan or an annulus around a cavity.
    rear_center = len(vertices); vertices.append((0,ys[0],0))
    for i in range(n):
        k=(i+1)%n
        def cap_uv(v, ox, oy):
            x,_,z=vertices[v]
            return (ox+.10+.095*x/width, oy+.10+.095*z/height)
        face((rear_center,rings[0][i],rings[0][k]),
             (cap_uv(rear_center,.50,.02),cap_uv(rings[0][i],.50,.02),cap_uv(rings[0][k],.50,.02)), 'rear')

    if furnace_throat:
        # Ten perimeter points align with the shoulder-outline topology.
        # These coordinates are in the same physical width/height space.
        inner = ((-.16,-.18),(.16,-.18),(.19,-.12),(.19,.12),(.13,.18),(.05,.18),
                 (-.05,.18),(-.13,.18),(-.19,.12),(-.19,-.12))
        front_inner = ring([(x,z) for x,z in inner], ys[-1])
        floor_inner = ring([(x,z) for x,z in inner], depth*.13)
        for i in range(n):
            k=(i+1)%n
            face((rings[-1][i],front_inner[i],front_inner[k],rings[-1][k]),
                 (cap_uv(rings[-1][i],.76,.02),cap_uv(front_inner[i],.76,.02),
                  cap_uv(front_inner[k],.76,.02),cap_uv(rings[-1][k],.76,.02)), 'front')
            face((front_inner[i],floor_inner[i],floor_inner[k],front_inner[k]),
                 ((.02+.44*i/n,.54),(.02+.44*i/n,.76),
                  (.02+.44*(i+1)/n,.76),(.02+.44*(i+1)/n,.54)), 'throat_wall')
        floor_center=len(vertices); vertices.append((0,depth*.13,0))
        for i in range(n):
            k=(i+1)%n
            face((floor_center,floor_inner[k],floor_inner[i]),
                 (cap_uv(floor_center,.76,.52),cap_uv(floor_inner[k],.76,.52),
                  cap_uv(floor_inner[i],.76,.52)), 'throat_floor')
    else:
        front_center=len(vertices); vertices.append((0,ys[-1],0))
        for i in range(n):
            k=(i+1)%n
            face((front_center,rings[-1][k],rings[-1][i]),
                 (cap_uv(front_center,.76,.02),cap_uv(rings[-1][k],.76,.02),
                  cap_uv(rings[-1][i],.76,.02)), 'front')
    return vertices, faces, uv_faces, regions
