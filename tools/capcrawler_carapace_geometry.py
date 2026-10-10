"""Closed, UV-authored fungal shield for the established Fungal Forest Capcrawler.

Only replaces Capcrawler_Carapace's generic oval ico-sphere. Keeps its original
(0,-.015,.31) origin, low HOST-LOW-CRAWLER envelope and Body bone/material.
Pure geometry can be tested without Blender or game assets.
"""
from math import cos, exp, pi, sin

CONTRACT = 'capcrawler-scalloped-fungal-shield-v1'
UV_CONTRACT = 'capcrawler-shield-dorsal-ventral-rim-v1'
ORIGIN = (0.0, -0.015, 0.31)
SECTORS = 64
RADIAL_RINGS = 11


def _rim(theta):
    """Broad front, scalloped sides, rear notches; never exceeds old shell bounds."""
    x, y = cos(theta), sin(theta)
    # Paired fungal shelf scallops, with shallower front brow and rear lobes.
    scallop = 1.0 - .085 * sin(4 * theta) ** 2
    rear = 1.0 - .026 * max(0.0, -y) ** 4 * (1.0 - cos(4 * theta)) / 2
    front_notch = 1.0 - .080 * exp(-(x / .18) ** 2) * max(0.0, y) ** 4
    return .356 * x * scallop * rear, .425 * y * scallop * rear * front_notch


def shield_mesh_data():
    """Return local vertices, outward faces and explicit per-face UV loops."""
    vertices = [(0.0, 0.0, .101)]
    # Dorsal shell: elevated central shield, raised bilateral folds, shallow
    # median keel, and a pronounced, physically scalloped outer silhouette.
    for i in range(1, RADIAL_RINGS + 1):
        r = i / RADIAL_RINGS
        for j in range(SECTORS):
            theta = 2 * pi * j / SECTORS
            x, y = _rim(theta)
            keel = .012 * (1 - r) * r * (max(0.0, sin(theta)) ** 2)
            paired_folds = .013 * (1-r) * r * cos(4 * theta)
            z = .007 + .094 * (1-r**1.65) + keel + paired_folds
            vertices.append((x*r, y*r, z))
    # The rim is a real downturned shell lip, not an overlapping torus.
    lower_rim = len(vertices)
    for j in range(SECTORS):
        theta = 2*pi*j/SECTORS
        x,y = _rim(theta)
        vertices.append((x*.991,y*.991,-.018))
    # Underside is a shallow inward arch, leaving visible gill clearance.
    underside_start = len(vertices)
    for i in range(RADIAL_RINGS-1,0,-1):
        r=i/RADIAL_RINGS
        for j in range(SECTORS):
            theta=2*pi*j/SECTORS
            x,y=_rim(theta)
            z=-.018-.044*(1-r*r)
            vertices.append((x*r,y*r,z))
    bottom_center = len(vertices)
    vertices.append((0.0,0.0,-.062))

    faces, uvs = [], []
    def dorsal_uv(vertex):
        x,y,_=vertices[vertex]
        return (.26 + .209*x/.356, .26 + .209*y/.425)
    def ventral_uv(vertex):
        x,y,_=vertices[vertex]
        return (.74 + .209*x/.356, .26 + .209*y/.425)
    def add(face,mapper):
        faces.append(face)
        uvs.append(tuple(mapper(k) for k in face))

    for j in range(SECTORS):
        k=(j+1)%SECTORS
        add((0,1+j,1+k),dorsal_uv)
    for i in range(RADIAL_RINGS-1):
        inner=1+i*SECTORS
        outer=inner+SECTORS
        for j in range(SECTORS):
            k=(j+1)%SECTORS
            add((inner+j,outer+j,outer+k,inner+k),dorsal_uv)
    top_outer=1+(RADIAL_RINGS-1)*SECTORS
    for j in range(SECTORS):
        k=(j+1)%SECTORS
        faces.append((top_outer+j,lower_rim+j,lower_rim+k,top_outer+k))
        u0=.05+.90*j/SECTORS
        u1=.05+.90*(j+1)/SECTORS
        uvs.append(((u0,.88),(u0,.60),(u1,.60),(u1,.88)))
    previous=lower_rim
    for i in range(RADIAL_RINGS-1):
        inner=underside_start+i*SECTORS
        for j in range(SECTORS):
            k=(j+1)%SECTORS
            add((previous+j,inner+j,inner+k,previous+k),ventral_uv)
        previous=inner
    for j in range(SECTORS):
        k=(j+1)%SECTORS
        add((bottom_center,previous+k,previous+j),ventral_uv)
    return vertices,faces,uvs


def sculpted_shield(name, material):
    """Blender adapter; caller retains existing Body armature binding."""
    import bpy
    vertices,faces,uvs=shield_mesh_data()
    mesh=bpy.data.meshes.new(name+'_Mesh')
    mesh.from_pydata(vertices,[],faces)
    mesh.update()
    obj=bpy.data.objects.new(name,mesh)
    bpy.context.collection.objects.link(obj)
    obj.location=ORIGIN
    mesh.materials.append(material)
    obj['magenheim_capcrawler_shield_contract']=CONTRACT
    obj['magenheim_capcrawler_shield_uv_contract']=UV_CONTRACT
    layer=mesh.uv_layers.new(name='CapcrawlerUV')
    for polygon,face_uv in zip(mesh.polygons,uvs):
        polygon.use_smooth=len(polygon.vertices)==4
        for li,uv in zip(polygon.loop_indices,face_uv):
            layer.data[li].uv=uv
    return obj
