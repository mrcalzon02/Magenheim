"""Rift Titan primary-body masses, authored from the adopted 2026-10-03 concept.

The approved silhouette has load-bearing shoulder/pectoral rock and paired lower
thorax buttresses around an EXPOSED core, not isolated arm towers and a loose
floating pelvis. This is source-art layout only, not a runtime mesh or approval.
"""
from math import isfinite

# (name, side-local X, Y, Z, half-width, half-height, half-depth, Y tilt, seed)
# The chest remains deliberately open between the upper mantle and lower
# buttresses. Four faceted plates cost 4 x 24 = 96 source triangles.
PRIMARY_BODY_MASSES = (
    ('ThoraxMantle', .49, -.055, 2.77, .44, .64, .19, -.20, 29),
    ('LowerCoreButtress', .40, .015, 1.74, .29, .58, .16, .16, 37),
)


def primary_mass_strata():
    """Return four paired plates with side-specific pivots and identifiers.

    Layout is symmetric in load-bearing outline, while the existing per-plate
    rock-facet seeds provide surface irregularity. Each plate binds to Body.
    """
    parts = []
    for side, suffix in ((-1, 'L'), (1, 'R')):
        for name, x, y, z, width, height, depth, tilt, seed in PRIMARY_BODY_MASSES:
            values = (x, y, z, width, height, depth, tilt, seed)
            if not all(isfinite(v) for v in values) or min(width, height, depth) <= 0:
                raise ValueError('invalid Rift Titan primary-body mass')
            parts.append((name + suffix, (side * x, y, z), width,
                          height, depth, side * tilt, seed + (0 if side < 0 else 1)))
    return tuple(parts)
