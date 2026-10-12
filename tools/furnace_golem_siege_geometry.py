"""Authored impact silhouette for the established Furnace Golem siege gauntlets.

Local Blender XYZ; +Y is the strike direction. The broad hammer fists and
three hooked impact tines per hand replace generic boxes/cones. This is editable
source geometry, not a runtime-LOD or approved-concept acceptance claim.
"""
from math import cos, sin, pi, copysign

FIST_SECTORS = 18
TOOTH_SECTORS = 12
FIST_RINGS = ((-.15,.69,.78),(-.105,.88,.88),(-.045,1.00,.98),
              (.045,1.13,1.04),(.135,1.16,1.13),(.205,1.08,1.10),
              (.225,.92,.94))
TOOTH_RINGS = ((0.,1.),(.035,1.08),(.085,.85),(.13,.60),(.175,.29),(.21,.035))


def _tube(sectors, rings, coords, cap_radius):
    vertices=[]; faces=[]; uv_faces=[]; regions=[]; indexed=[]
    for j in range(len(rings)):
        ring=[]
        for i in range(sectors):
            ring.append(len(vertices)); vertices.append(coords(j,i,2*pi*i/sectors))
        indexed.append(ring)
    def emit(indices,uv,region):
        faces.append(tuple(indices)); uv_faces.append(tuple(uv)); regions.append(region)
    for j in range(len(rings)-1):
        for i in range(sectors):
            k=(i+1)%sectors; u0=i/sectors; u1=(i+1)/sectors
            v0=j/(len(rings)-1); v1=(j+1)/(len(rings)-1)
            emit((indexed[j][i],indexed[j+1][i],indexed[j+1][k],indexed[j][k]),
                 ((.03+.94*u0,.03+.60*v0),(.03+.94*u0,.03+.60*v1),
                  (.03+.94*u1,.03+.60*v1),(.03+.94*u1,.03+.60*v0)),'shell')
    for j,region,center in ((0,'wrist',(.27,.83)),(len(rings)-1,'strike',(.73,.83))):
        cy=rings[j][0]; cidx=len(vertices); vertices.append((0,cy,0))
        # For teeth the central z coordinate can be curved; use average ring center.
        vertices[cidx]=(sum(vertices[v][0] for v in indexed[j])/sectors,cy,
                        sum(vertices[v][2] for v in indexed[j])/sectors)
        for i in range(sectors):
            k=(i+1)%sectors
            face=(cidx,indexed[j][i],indexed[j][k]) if j==0 else (cidx,indexed[j][k],indexed[j][i])
            uvs=[]
            for vi in face:
                x,_,z=vertices[vi]; cx,_,cz=vertices[cidx]
                uvs.append((center[0]+cap_radius*(x-cx),center[1]+cap_radius*(z-cz)))
            emit(face,uvs,region)
    return vertices,faces,uv_faces,regions


def make_siege_fist(width=.29,height=.27):
    """Massive beveled hammer-fist with projecting impact brow and closed wrist."""
    if width<=0 or height<=0: raise ValueError('Siege fist dimensions must be positive')
    def coords(j,i,theta):
        y,sx,sz=FIST_RINGS[j]
        x=.5*width*sx*copysign(abs(cos(theta))**.58,cos(theta))
        z=.5*height*sz*copysign(abs(sin(theta))**.58,sin(theta))
        # A physical upper strike brow; not merely painted onto a box.
        if j>=3 and sin(theta)>.55:
            z+=.018*(1 if j in (4,5) else .5)
        return x,y,z
    return _tube(FIST_SECTORS,FIST_RINGS,coords,.75)


def make_siege_tooth(radius=.031,length=.21):
    """Hooked forward-facing impact tooth with a thick root and a downturned tip."""
    if radius<=0 or length<=0: raise ValueError('Siege tooth dimensions must be positive')
    def coords(j,i,theta):
        y,scale=TOOTH_RINGS[j]
        y=y*length/.21
        r=radius*scale
        # The tip turns downward as it projects beyond the hammer face.
        drop=.037*(y/length)**2
        return r*cos(theta),y,r*sin(theta)-drop
    return _tube(TOOTH_SECTORS,TOOTH_RINGS,coords,2.4)
