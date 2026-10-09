"""Closed, swept talons for the existing Shelf Lurker's 18 three-digit claws.

The 36 segments keep original endpoints, grip bones and hard-claw material.
A shared vertical knuckle tangent prevents the former two-cone elbow kink.
"""
from math import cos, sin, pi, sqrt, isfinite

RINGS = 9
SIDES = 12
CONTRACT = 'swept-gripping-talon-9x12'

def _add(a,b):
    return tuple(x+y for x,y in zip(a,b))

def _mul(a,s):
    return tuple(x*s for x in a)

def _cross(a,b):
    return (a[1]*b[2]-a[2]*b[1],a[2]*b[0]-a[0]*b[2],a[0]*b[1]-a[1]*b[0])

def _unit(a):
    length=sqrt(sum(x*x for x in a))
    if length<=1e-9: raise ValueError('Degenerate talon tangent/frame')
    return _mul(a,1/length)

def talon_mesh_data(start,end,radius_start,radius_end,bow):
    """Outward-facing closed nine-ring tube with exact endpoint centers."""
    a,b,c=(tuple(float(v) for v in point) for point in (start,end,bow))
    r0,r1=float(radius_start),float(radius_end)
    if any(len(p)!=3 for p in (a,b,c)) or not all(isfinite(v) for v in a+b+c+(r0,r1)):
        raise ValueError('Invalid/nonfinite talon geometry')
    delta=tuple(y-x for x,y in zip(a,b))
    length=sqrt(sum(x*x for x in delta))
    if not .10<=length<=.50 or not .004<=r1<=r0<=.06 or b[2]>=a[2]:
        raise ValueError('Talon dimensions outside approved gripping anatomy')
    if sqrt(sum(x*x for x in c))>.15 or abs(c[2])>1e-8:
        raise ValueError('Talon curvature exceeds safe silhouette/knuckle clearance')
    verts=[]
    for ring in range(RINGS):
        t=ring/(RINGS-1)
        center=_add(_add(a,_mul(delta,t)),_mul(c,sin(pi*t)))
        tangent=_unit(_add(delta,_mul(c,pi*cos(pi*t))))
        u=_unit(_cross((0.,1.,0.),tangent))
        v=_cross(tangent,u)
        radius=(1-t)*r0+t*r1
        for j in range(SIDES):
            theta=2*pi*j/SIDES
            flutes=1+.042*cos(3*theta)*sin(pi*t)**2
            verts.append(_add(center,_mul(_add(_mul(u,cos(theta)),_mul(v,sin(theta))),radius*flutes)))
    faces=[]
    for ring in range(RINGS-1):
        for j in range(SIDES):
            k=(j+1)%SIDES
            faces.append((ring*SIDES+j,ring*SIDES+k,(ring+1)*SIDES+k,(ring+1)*SIDES+j))
    faces.append(tuple(reversed(range(SIDES))))
    faces.append(tuple((RINGS-1)*SIDES+j for j in range(SIDES)))
    return verts,faces

def talon_segment(name,start,end,radius_start,radius_end,bow,material):
    """Blender adapter: existing hard-claw PBR material and grip rig."""
    import bpy
    verts,faces=talon_mesh_data(start,end,radius_start,radius_end,bow)
    mesh=bpy.data.meshes.new(name+'_Mesh')
    mesh.from_pydata(verts,[],faces)
    mesh.update()
    obj=bpy.data.objects.new(name,mesh)
    bpy.context.collection.objects.link(obj)
    mesh.materials.append(material)
    obj['magenheim_talon_contract']=CONTRACT
    for polygon in mesh.polygons:
        polygon.use_smooth=len(polygon.vertices)==4
    uv=mesh.uv_layers.new(name='ShelfLurkerUV')
    for polygon in mesh.polygons:
        ids=[mesh.loops[li].vertex_index%SIDES for li in polygon.loop_indices]
        seam=0 in ids and SIDES-1 in ids
        for li in polygon.loop_indices:
            index=mesh.loops[li].vertex_index
            angular=index%SIDES
            if len(polygon.vertices)>4:
                theta=2*pi*angular/SIDES
                uv.data[li].uv=(.5+.45*cos(theta),.5+.45*sin(theta))
            else:
                uv.data[li].uv=(1. if seam and angular==0 else angular/SIDES,
                                (index//SIDES)/(RINGS-1))
    return obj
