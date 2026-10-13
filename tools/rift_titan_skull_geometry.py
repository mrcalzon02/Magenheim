"""Faceted, genuinely recessed Rift Titan eye socket geometry.

The outer stone lip is continuous with an inset cavity and a closed back wall;
the crystal occupies the recess without protruding through the stone lip.
No Blender dependency, so geometry can be inspected before regenerating source.
"""
from math import cos, sin, isfinite


def eye_socket_geometry(center, width, height, depth, tilt=0.0):
    """Return 16 vertices and 28 triangles for a closed four-sided eye recess.

    Outer front lip -> inner front aperture -> inset back wall -> outer rear.
    Front is negative Y, as in the Rift Titan primary study.
    """
    vals = tuple(float(v) for v in (*center, width, height, depth, tilt))
    if len(center) != 3 or not all(map(isfinite, vals)):
        raise ValueError('eye socket needs finite XYZ and dimensions')
    if min(width, height, depth) <= 0:
        raise ValueError('eye socket dimensions must be positive')
    x, y, z = vals[:3]
    ct, st = cos(tilt), sin(tilt)
    # Four closed concentric rectangular loops; the rear cap is an actual face.
    # Each loop has matching clockwise profile indices.
    rings = (
        (1.0, 1.0, -1.35),   # raised, silhouette-readable stone rim
        (.76, .73, -1.18),   # inner lip opening
        (.76, .73, .76),    # recessed back of the well
        (1.0, 1.0, 1.0),    # rear stone surface
    )
    vertices = []
    for sx, sz, sy in rings:
        for ox, oz in ((-1,-1),(1,-1),(1,1),(-1,1)):
            u, v = ox * width * sx, oz * height * sz
            vertices.append((x + u*ct + v*st, y + depth*sy, z + v*ct - u*st))
    faces = []
    # Front stone annulus, cavity walls and exterior sidewalls.
    for a,b in ((0,1),(1,2),(3,0)):
        for i in range(4):
            j=(i+1)%4
            faces.append((4*a+i,4*a+j,4*b+j,4*b+i))
    faces.append((8,9,10,11))  # back of the inset recess
    faces.append((15,14,13,12))  # closed outer rear
    return vertices, faces
