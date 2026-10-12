"""Hollow, UV-authored exhaust stacks/cowls for the established Furnace Golem.

The annular ends close the tube walls without sealing the air passage.
Coordinates are local Blender XYZ (+Z up).
"""
from math import cos, sin, pi

SECTORS = 16
PROFILES = {
    'stack': ((-.23,.070,.046),(-.11,.073,.047),(.10,.075,.047),(.23,.077,.047)),
    'cowl': ((-.10,.076,.052),(-.05,.096,.061),(.02,.115,.070),
             (.07,.128,.081),(.10,.121,.088)),
}

def make_vent(kind):
    """Return vertices, quads, per-face UV loops and surface regions."""
    if kind not in PROFILES:
        raise ValueError('Unknown Furnace Golem exhaust profile: '+str(kind))
    profile=PROFILES[kind]
    vertices=[]; faces=[]; uv_faces=[]; regions=[]; rings=[]
    for z,outer,inner in profile:
        pair=[]
        for is_inner,radius in ((False,outer),(True,inner)):
            ring=[]
            for i in range(SECTORS):
                theta=2*pi*i/SECTORS
                relief=(.003 if kind=='cowl' else .0015)*cos(theta*8)
                r=radius+(relief if not is_inner else 0)
                ring.append(len(vertices))
                vertices.append((r*cos(theta),r*sin(theta),z))
            pair.append(ring)
        rings.append(pair)

    def emit(indices,coords,region):
        faces.append(tuple(indices)); uv_faces.append(tuple(coords)); regions.append(region)

    for j in range(len(profile)-1):
        v0=j/(len(profile)-1); v1=(j+1)/(len(profile)-1)
        for i in range(SECTORS):
            k=(i+1)%SECTORS
            u0=i/SECTORS; u1=(i+1)/SECTORS
            outer0,inner0=rings[j]; outer1,inner1=rings[j+1]
            emit((outer0[i],outer0[k],outer1[k],outer1[i]),
                 ((.02+.46*u0,.03+.42*v0),(.02+.46*u1,.03+.42*v0),
                  (.02+.46*u1,.03+.42*v1),(.02+.46*u0,.03+.42*v1)),'outer')
            emit((inner0[i],inner1[i],inner1[k],inner0[k]),
                 ((.52+.46*u0,.03+.42*v0),(.52+.46*u0,.03+.42*v1),
                  (.52+.46*u1,.03+.42*v1),(.52+.46*u1,.03+.42*v0)),'bore')

    def disk_uv(index,center):
        x,y,_=vertices[index]
        return (center[0]+x*.70,center[1]+y*.70)

    bottom_outer,bottom_inner=rings[0]
    top_outer,top_inner=rings[-1]
    for i in range(SECTORS):
        k=(i+1)%SECTORS
        low=(bottom_outer[i],bottom_inner[i],bottom_inner[k],bottom_outer[k])
        high=(top_outer[i],top_outer[k],top_inner[k],top_inner[i])
        emit(low,tuple(disk_uv(idx,(.25,.73)) for idx in low),'base_rim')
        emit(high,tuple(disk_uv(idx,(.75,.73)) for idx in high),'mouth_rim')
    return vertices,faces,uv_faces,regions
