"""Closed heat-fractured dorsal scutes for the established Sulfurous Ashmite.

The five existing dorsal plates retain their authoring anchors, material and
abdomen/thorax bone assignments. Coordinates are mesh-local.
"""
from math import cos, exp, isfinite, pi, sin

RINGS = 16
SIDES = 64
CONTRACT = 'ashmite-five-heat-fractured-scutella-16x64'
PROFILE = ((.09,.94),(.18,.94),(.28,.93),(.38,.90),(.48,.87),
           (.58,.81),(.68,.75),(.78,.64),(.87,.54),(.94,.39),
           (1.,.08),(.98,-.16),(.85,-.35),(.65,-.46),(.40,-.44),(.20,-.34))


def scute_mesh_data(rx, ry, rz, plate):
    """Generate a five-identity, UV-safe, outward-wound solid carapace plate."""
    if not (1 <= plate <= 5):
        raise ValueError('Ashmite scute plate must be 1..5')
    if not all(isfinite(v) and v > 0 for v in (rx,ry,rz)):
        raise ValueError('Ashmite scute radii must be positive finite numbers')
    vertices = [(0.,0.,rz*.94), (0.,0.,-rz*.34)]
    for ring,(rad,level) in enumerate(PROFILE):
        for j in range(SIDES):
            a=2*pi*j/SIDES
            rim=(.040*cos(7*a+plate*.31)+.016*cos(11*a-plate*.43)) * min(1.,rad/.55)
            x=rx*rad*(1+rim)*cos(a)
            y=ry*rad*(1+rim)*sin(a)
            dorsal=max(0.,sin(pi*rad))**2 if ring < 11 else 0.
            crest=.12*exp(-((x/(rx*.32))**2))*dorsal
            crack=max(0.,cos(6*a+plate*.51+rad*.80))**18
            fracture=.095*crack*dorsal
            # Rear overlap and lateral load-bearing ridges are physical geometry,
            # not painted marks. Keep the approved five-scute footprint.
            trailing=max(0.,-sin(a))**8
            flank=abs(cos(a))**10
            rear_lip=.13*trailing*exp(-((rad-.91)/.12)**2)
            flank_buttress=.075*flank*exp(-((rad-.69)/.19)**2)
            z=rz*(level+crest-fracture+rear_lip+flank_buttress)
            vertices.append((x,y,z))
    faces=[]
    for j in range(SIDES):
        faces.append((0,2+j,2+(j+1)%SIDES))
    for ring in range(RINGS-1):
        first=2+ring*SIDES; nxt=first+SIDES
        for j in range(SIDES):
            k=(j+1)%SIDES
            faces.append((first+j,nxt+j,nxt+k,first+k))
    last=2+(RINGS-1)*SIDES
    for j in range(SIDES):
        faces.append((1,last+(j+1)%SIDES,last+j))
    return vertices, faces


# Separate the upper shell, lower shell and exposed overlap edge into UV islands.
# This prevents the original planar projection from stacking dorsal and ventral
# shell texture samples while keeping all three islands within the existing
# 1024px heat-chitin PBR family. Blender must write UVs per face loop: adjacent
# vertices intentionally have different coordinates across the island seams.
UV_CONTRACT = 'ashmite-scute-three-island-uv-v1'
DORSAL_UV_CENTER = (.25, .75)
VENTRAL_UV_CENTER = (.75, .75)
UV_DISK_RADIUS = .205
RIM_U = (.055, .945)
RIM_V = (.085, .395)


def scute_uv(vertex, rx, ry, underside=False):
    """Project the upper or lower carapace into a dedicated disk island."""
    if not all(isfinite(v) and v > 0 for v in (rx, ry)):
        raise ValueError('Ashmite scute UV radii must be positive finite numbers')
    x, y, _ = vertex
    center = VENTRAL_UV_CENTER if underside else DORSAL_UV_CENTER
    return (center[0] + UV_DISK_RADIUS*x/rx,
            center[1] + UV_DISK_RADIUS*y/ry)


def scute_face_uvs(vertices, faces, rx, ry):
    """Author loop UVs with separate dorsal, ventral and perimeter islands.

    The perimeter has a deliberate angular seam at sector zero. Unwrapping
    each edge quad independently avoids the 0->1 texture interpolation that
    otherwise streaks across the entire final sector.
    """
    if len(vertices) != 2 + RINGS*SIDES or len(faces) != (RINGS+1)*SIDES:
        raise ValueError('Ashmite scute UV topology does not match 16x64 shell')
    if not all(isfinite(v) and v > 0 for v in (rx, ry)):
        raise ValueError('Ashmite scute UV radii must be positive finite numbers')
    dorsal_end = (1 + 10)*SIDES
    rim_end = dorsal_end + SIDES
    output = []
    for fi, face in enumerate(faces):
        if fi < dorsal_end:
            uv = tuple(scute_uv(vertices[k], rx, ry) for k in face)
        elif fi < rim_end:
            sector = fi - dorsal_end
            u0 = RIM_U[0] + (RIM_U[1]-RIM_U[0])*sector/SIDES
            u1 = RIM_U[0] + (RIM_U[1]-RIM_U[0])*(sector+1)/SIDES
            uv = ((u0,RIM_V[1]), (u0,RIM_V[0]),
                  (u1,RIM_V[0]), (u1,RIM_V[1]))
        else:
            uv = tuple(scute_uv(vertices[k], rx, ry, underside=True)
                       for k in face)
        output.append(uv)
    return output


def sculpted_scute(name, loc, scale, material, plate):
    """Blender adapter, preserving author-side rigging and material assignment."""
    import bpy
    rx,ry,rz=scale
    vertices,faces=scute_mesh_data(rx,ry,rz,plate)
    mesh=bpy.data.meshes.new(name+'_Mesh')
    mesh.from_pydata(vertices,[],faces)
    mesh.update()
    obj=bpy.data.objects.new(name,mesh)
    bpy.context.collection.objects.link(obj)
    obj.location=loc
    mesh.materials.append(material)
    obj['magenheim_ashmite_scute_contract']=CONTRACT
    obj['magenheim_ashmite_scute_index']=plate
    obj['magenheim_ashmite_scute_uv_contract']=UV_CONTRACT
    layer=mesh.uv_layers.new(name='AshmiteUV')
    loop_uvs=scute_face_uvs(vertices,faces,rx,ry)
    for poly,uvs in zip(mesh.polygons,loop_uvs):
        poly.use_smooth=len(poly.vertices)==4
        for li,uv in zip(poly.loop_indices,uvs):
            layer.data[li].uv=uv
    return obj
