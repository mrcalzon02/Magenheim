"""Rift Titan concept-rock plate geometry, independent of Blender.

Keep the original six-point outer outline for rig and silhouette continuity.
The shallow, offset front/back ridge replaces planar caps with actual stone
facets without increasing the number of independent floating rock pieces.
"""
from math import cos, isfinite, sin

OUTLINE = ((-.90, -.33), (-.66, .35), (-.12, .50), (.80, .32),
           (1.0, -.10), (.22, -.50))


def faceted_plate_geometry(center, width, height, depth, tilt=0.0, seed=0):
    """Return (XYZ vertices, faces) for a 24-triangle closed faceted rock plate.

    Width/height are authored half-profile scales, depth is half-thickness.
    Ridge depth is deliberately capped at 15% so reduced shoulder thickness
    remains reduced even after the new face facets are added.
    """
    c = tuple(float(v) for v in center)
    if len(c) != 3:
        raise ValueError('plate center must be XYZ')
    width, height, depth, tilt, seed = map(float, (width, height, depth, tilt, seed))
    if not all(map(isfinite, c + (width, height, depth, tilt, seed))):
        raise ValueError('plate coordinates must be finite')
    if min(width, height, depth) <= 0:
        raise ValueError('plate dimensions must be positive')
    ct, st = cos(tilt), sin(tilt)
    vertices = []
    for side in (-1, 1):
        for k, (ox, oz) in enumerate(OUTLINE):
            x = ox * width * (1 + .07 * sin(seed + k * 2.1))
            z = oz * height
            vertices.append((c[0] + x * ct + z * st,
                             c[1] + side * depth * (1 + .12 * cos(k * 1.9 + seed)),
                             c[2] + z * ct - x * st))
    # A shallow, irregular rock face ridge. Both points are INSIDE the
    # six-point profile, and sit beyond their corresponding perimeter plane.
    for side in (-1, 1):
        x = width * (.035 * sin(seed + 1.3))
        z = height * (.065 * cos(seed + .7))
        vertices.append((c[0] + x * ct + z * st,
                         c[1] + side * depth * 1.15,
                         c[2] + z * ct - x * st))
    faces = ([(12, (k + 1) % 6, k) for k in range(6)]
             + [(13, 6 + k, 6 + (k + 1) % 6) for k in range(6)]
             + [(k, (k + 1) % 6, (k + 1) % 6 + 6, k + 6)
                for k in range(6)])
    return vertices, faces
