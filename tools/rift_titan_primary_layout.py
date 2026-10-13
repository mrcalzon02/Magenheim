"""Concept-led proportions for the Rift Titan's major shoulder and limb strata.

Pure geometry layout; the Blender author owns mesh construction and materials.
Keep the boss's existing Rift Colossus rig, open core cage, and 1.5x StoneGolem
soft cost ceiling. These are authored source proportions, not art acceptance.
"""
from math import atan2, hypot, isfinite

# Side-local X, height Z, half-thickness Y. Preserve the outward shoulder reach
# while reducing the stacked depth that made the r3 clay study look blocky.
SHOULDER_STRATA = (
    (0.65, 2.85, 0.47, 0.34, 0.18, 0.10),
    (1.02, 2.76, 0.35, 0.41, 0.15, 0.22),
    (0.84, 3.00, 0.38, 0.19, 0.12, -0.14),
)


def limb_strata(head, tail, side, leg=False):
    """Return three slim, bone-aligned plates (center, width, height, depth, tilt).

    The plate's local long axis is +Z. Its author rotates that axis about Y,
    so the X/Z component of the real rig bone—not world vertical—sets tilt.
    The shallow front/back staggering preserves separate fault-line surfaces.
    """
    a = tuple(float(x) for x in head)
    b = tuple(float(x) for x in tail)
    if len(a) != 3 or len(b) != 3 or not all(map(isfinite, a + b)):
        raise ValueError('Rift Titan limb endpoints must be finite XYZ points')
    if side not in (-1, 1):
        raise ValueError('Rift Titan limb side must be -1 or +1')
    delta = tuple(y - x for x, y in zip(a, b))
    length = hypot(*delta)
    if length <= 1e-5 or hypot(delta[0], delta[2]) <= 1e-5:
        raise ValueError('Rift Titan limb bone must have a usable XZ axis')
    axis = atan2(delta[0], delta[2])
    width = min(0.155 if leg else 0.135, length * 0.14)
    depth = min(0.125 if leg else 0.110, length * 0.115)
    return tuple(
        (
            (a[0] + delta[0] * t,
             a[1] + delta[1] * t + (0.025 if j == 1 else -0.018),
             a[2] + delta[2] * t),
            width * (1 - 0.055 * j),
            length * (0.60 if j == 1 else 0.53),
            depth,
            axis + side * (0.025 if j == 1 else -0.018),
        )
        for j, t in enumerate((0.18, 0.50, 0.82))
    )
