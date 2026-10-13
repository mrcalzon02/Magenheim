"""Rift Titan concept head: heavy brow, jaw, cheek stone and inset crystal eyes.

Reuses six former floating strata and two former floating prisms, preserving
the existing boss-study triangle count. This is authoring, not art acceptance.
"""
from math import isfinite

# name, center, half-width, half-height, half-depth, Y tilt, seed, material
HEAD_STONES = (
    ('HeadBrow', (0.0, -.16, 3.18), .23, 1.08, .22, 1.53, 40, 'rift-strata'),
    ('HeadJaw', (0.0, -.20, 2.98), .17, .72, .19, 1.51, 43, 'rift-strata'),
    ('HeadCheekL', (-.40, -.19, 3.09), .21, .68, .20, -.28, 45, 'rift-strata'),
    ('HeadCheekR', (.40, -.19, 3.09), .21, .68, .20, .28, 46, 'rift-strata'),
    ('EyeWellL', (-.20, -.405, 3.075), .095, .18, .055, -.06, 49, 'rift-bands'),
    ('EyeWellR', (.20, -.405, 3.075), .095, .18, .055, .06, 50, 'rift-bands'),
)
HEAD_EYES = (
    ('EyeGlowL', (-.20, -.475, 3.075), .065, .15),
    ('EyeGlowR', (.20, -.475, 3.075), .065, .15),
)
RETAINED_FLOATING_PRISMS = (
    ('FloatingPrism2', (-1.33, .10, 2.49), .045, .15),
    ('FloatingPrism3', (1.39, .17, 1.78), .045, .15),
    ('FloatingPrism4', (-.75, .03, .81), .045, .15),
    ('FloatingPrism5', (.70, .08, .66), .045, .15),
)

def head_parts():
    """Validate authored proportions and return stone, eye and mote parts."""
    for name, center, width, height, depth, tilt, seed, material in HEAD_STONES:
        if not all(isfinite(x) for x in (*center, width, height, depth, tilt, seed)):
            raise ValueError('Rift Titan head contains nonfinite geometry')
        if min(width, height, depth) <= 0 or material not in ('rift-strata', 'rift-bands'):
            raise ValueError('Rift Titan head stone is invalid')
    for name, center, radius, length in (*HEAD_EYES, *RETAINED_FLOATING_PRISMS):
        if not all(isfinite(x) for x in (*center, radius, length)) or min(radius, length) <= 0:
            raise ValueError('Rift Titan eye or mote is invalid')
    return HEAD_STONES, HEAD_EYES, RETAINED_FLOATING_PRISMS
