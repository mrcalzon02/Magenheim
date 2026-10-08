"""Pure, testable swept feeding-barb anatomy for the established Shelf Lurker."""
from math import cos, sin, pi, sqrt, isfinite

RINGS = 9
SIDES = 12
CONTRACT = 'swept-mouth-barb-9x12'


def _add(a, b):
    return tuple(x+y for x,y in zip(a,b))


def _mul(a, s):
    return tuple(x*s for x in a)


def _cross(a, b):
    return (a[1]*b[2]-a[2]*b[1], a[2]*b[0]-a[0]*b[2], a[0]*b[1]-a[1]*b[0])


def _unit(a):
    length = sqrt(sum(x*x for x in a))
    if length < 1e-9:
        raise ValueError('Degenerate barb tangent')
    return _mul(a, 1.0/length)


def mouth_barb_mesh_data(start, end, radius_start, radius_end, bow):
    """Closed curved tapered tube, with exact endpoint centerlines."""
    a,b,c = (tuple(float(v) for v in xyz) for xyz in (start,end,bow))
    r0,r1=float(radius_start),float(radius_end)
    if any(len(v)!=3 for v in (a,b,c)) or not all(isfinite(v) for v in a+b+c+(r0,r1)):
        raise ValueError('Invalid or nonfinite mouth geometry')
    delta=tuple(y-x for x,y in zip(a,b))
    length=sqrt(sum(v*v for v in delta))
    if not .08<=length<=.40 or not .002<=r1<=r0<=.045:
        raise ValueError('Mouth barb outside established anatomy')
    if sqrt(sum(v*v for v in c))>.12:
        raise ValueError('Mouth barb curvature exceeds safe clearance')
    vertices=[]
    for i in range(RINGS):
        t=i/(RINGS-1)
        center=_add(_add(a,_mul(delta,t)),_mul(c,sin(pi*t)))
        tangent=_unit(_add(delta,_mul(c,pi*cos(pi*t))))
        reference=(0.,1.,0.) if abs(tangent[1])<.90 else (1.,0.,0.)
        u=_unit(_cross(reference,tangent))
        v=_cross(tangent,u)
        radius=(1-t)*r0+t*r1
        for j in range(SIDES):
            angle=2*pi*j/SIDES
            relief=1+.035*cos(3*angle+2*pi*t)*sin(pi*t)
            point=_add(center,_mul(_add(_mul(u,cos(angle)),_mul(v,sin(angle))),radius*relief))
            vertices.append(point)
    faces=[]
    for i in range(RINGS-1):
        for j in range(SIDES):
            k=(j+1)%SIDES
            faces.append((i*SIDES+j,i*SIDES+k,(i+1)*SIDES+k,(i+1)*SIDES+j))
    faces.append(tuple(reversed(range(SIDES))))
    faces.append(tuple((RINGS-1)*SIDES+j for j in range(SIDES)))
    return vertices,faces


def mouth_barb_segment(name,start,end,radius_start,radius_end,bow,material):
    """Blender adapter retaining mouth material, UVs and Head rig binding."""
    import bpy
    vertices,faces=mouth_barb_mesh_data(start,end,radius_start,radius_end,bow)
    mesh=bpy.data.meshes.new(name+'_Mesh')
    mesh.from_pydata(vertices,[],faces)
    mesh.update()
    obj=bpy.data.objects.new(name,mesh)
    bpy.context.collection.objects.link(obj)
    mesh.materials.append(material)
    obj['magenheim_mouth_contract']=CONTRACT
    for polygon in mesh.polygons:
        polygon.use_smooth=len(polygon.vertices)==4
    uv=mesh.uv_layers.new(name='ShelfLurkerUV')
    for polygon in mesh.polygons:
        angular_ids=[mesh.loops[li].vertex_index%SIDES for li in polygon.loop_indices]
        seam=0 in angular_ids and SIDES-1 in angular_ids
        for li in polygon.loop_indices:
            index=mesh.loops[li].vertex_index
            angle=2*pi*(index%SIDES)/SIDES
            if len(polygon.vertices)>4:
                uv.data[li].uv=(.5+.45*cos(angle),.5+.45*sin(angle))
            else:
                uv.data[li].uv=(1. if seam and index%SIDES==0 else (index%SIDES)/SIDES,
                                (index//SIDES)/(RINGS-1))
    return obj
