"""Rift Titan r9 primary silhouette parameters, independent of Blender.

The r8 head reads as a straight upright spike in side clay. The adopted
left-facing concept has a forward-raking fractured crest over a much fuller
shoulder-to-collar transition. These are shape corrections, not surface detail.
"""
from math import isfinite

CROWN_PARTS = ('SplitCrown0', 'SplitCrown1', 'SplitCrown2',
               'FloatingPrism0', 'FloatingPrism1')
CROWN_BASE_Z = 3.12
CROWN_TIP_Z = 4.43
TIP_FORWARD = 0.42  # authoring units; +Y is forward (screen-left in side view)


def smoothstep(value, start, stop):
    if not all(isfinite(float(x)) for x in (value, start, stop)) or stop <= start:
        raise ValueError('finite, increasing silhouette stations required')
    t = max(0.0, min(1.0, (value - start) / (stop - start)))
    return t * t * (3.0 - 2.0 * t)


def crown_point(name, point):
    """Lean only the upper crest; retain the lower jaw and six neck-rib roots."""
    if name not in CROWN_PARTS:
        raise ValueError('r9 crown transform is restricted to approved crest parts')
    xyz = tuple(float(v) for v in point)
    if len(xyz) != 3 or not all(map(isfinite, xyz)):
        raise ValueError('finite XYZ crest vertex required')
    x, y, z = xyz
    t = smoothstep(z, CROWN_BASE_Z, CROWN_TIP_Z)
    # Slightly splay the two stone halves in front, keeping the central cleft.
    side = -1 if name == 'SplitCrown0' else (1 if name == 'SplitCrown1' else 0)
    return (x + side * 0.045 * t, y + TIP_FORWARD * t, z)


def mantle_spec(side):
    """Repurpose r8's paired floating upper stones into attached-looking thorax masses.

    Broad, diagonal plates visually bridge the inner collar and the existing
    sloped shoulder stones without adding faces or entering the open core.
    """
    if side not in (-1, 1):
        raise ValueError('mantle side must be -1 or +1')
    return dict(center=(side * 0.56, 0.045, 3.00),
                width=0.335, height=0.58, depth=0.235,
                axis=(side * 0.53, 0.06, 1.0),
                variant=2, seed=29 if side == -1 else 30)
