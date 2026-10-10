"""Sculpted protected coxal membranes for the six established Ashmite hip sockets."""
from math import sin, cos, exp, pi
RINGS=20
SIDES=48
CONTRACT='ashmite-six-folded-coxal-membranes-v1'
SCALE=(.020,.022,.017)
def anchor(side,pair):
    if side not in (-1,1) or pair not in (1,2,3):
        raise ValueError('Invalid Ashmite coxal side/pair')
    return (side*.068,(.065,0.,-.070)[pair-1],.070)
def joint_mesh_data():
    sx,sy,sz=SCALE
    vertices=[(sx,0.,0.),(-sx,0.,0.)]
    for i in range(1,RINGS):
        theta=pi*i/RINGS
        ring=sin(theta)
        fold=exp(-((theta-.92)/.27)**2)+exp(-((theta-2.22)/.27)**2)
        for j in range(SIDES):
            angle=2*pi*j/SIDES
            relief=1-.105*fold+.040*fold*cos(8*theta)
            vertices.append((sx*cos(theta),
                             sy*ring*relief*(1+.023*cos(3*angle+.35))*cos(angle),
                             sz*ring*relief*(1+.015*sin(5*angle))*sin(angle)))
    faces=[]
    uvs=[]
    for j in range(SIDES):
        k=(j+1)%SIDES
        a,b=2*pi*j/SIDES,2*pi*(j+1)/SIDES
        faces.append((0,2+j,2+k))
        uvs.append(((.83,.25),(.83+.12*cos(a),.25+.12*sin(a)),
                    (.83+.12*cos(b),.25+.12*sin(b))))
    for i in range(RINGS-2):
        first=2+i*SIDES
        nxt=first+SIDES
        u0=.05+.63*(i+1)/RINGS
        u1=.05+.63*(i+2)/RINGS
        for j in range(SIDES):
            k=(j+1)%SIDES
            v0=.05+.90*j/SIDES
            v1=.05+.90*(j+1)/SIDES
            faces.append((first+j,nxt+j,nxt+k,first+k))
            uvs.append(((u0,v0),(u1,v0),(u1,v1),(u0,v1)))
    last=2+(RINGS-2)*SIDES
    for j in range(SIDES):
        k=(j+1)%SIDES
        a,b=2*pi*j/SIDES,2*pi*(j+1)/SIDES
        faces.append((1,last+k,last+j))
        uvs.append(((.83,.75),(.83+.12*cos(b),.75+.12*sin(b)),
                    (.83+.12*cos(a),.75+.12*sin(a))))
    return vertices,faces,uvs
def sculpted_joint(name,side,pair,material):
    import bpy
    verts,faces,uvs=joint_mesh_data()
    mesh=bpy.data.meshes.new(name+'_Mesh')
    mesh.from_pydata(verts,[],faces)
    mesh.update()
    obj=bpy.data.objects.new(name,mesh)
    bpy.context.collection.objects.link(obj)
    obj.location=anchor(side,pair)
    mesh.materials.append(material)
    obj['magenheim_ashmite_joint_contract']=CONTRACT
    obj['magenheim_ashmite_joint_side']=side
    obj['magenheim_ashmite_joint_pair']=pair
    layer=mesh.uv_layers.new(name='AshmiteUV')
    for poly,face_uvs in zip(mesh.polygons,uvs):
        poly.use_smooth=len(poly.vertices)==4
        for li,uv in zip(poly.loop_indices,face_uvs):
            layer.data[li].uv=uv
    return obj
